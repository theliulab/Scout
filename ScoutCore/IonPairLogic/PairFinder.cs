//using MoreLinq;
using ScoutCore.QueryLogic;
using ScoutCore.SpectraOperations;
using PatternTools.MSParserLight;
using SpectrumWizard.Predictors.CleavableXL;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YCore;
using static ScoutCore.SpectraOperations.ChargeOperations;
using SpectrumWizard.Utils;

namespace ScoutCore.IonPairLogic
{
    public class PairFinder
    {
        private CleaveReagent reagent;

        private double proton;
        private double ppm;

        private int maxCharge;
        private int maxIsotopes;
        private double isotopeMass;

        private double intensityThreshold;
        private readonly bool applyDeconvForPairSearching;
        private bool fullPairsOnly;
        private bool addPairIntensity;
        private int maxQuery;

        private List<(int c1, int c2)> _chargeTestOrder;

        public PairFinder(
            CleaveReagent reagent,
            float ppm,
            int maxCharge,
            int maxIsotopes,
            int maxQuery,
            bool fullPairsOnly,
            bool addPairIntensity,
            float intensityThreshold,
            bool applyDeconvForPairSearching,
            float? isotopeShift,
            float? protonMass
            )
        {



            this.reagent = reagent;
            this.ppm = ppm;
            this.maxCharge = maxCharge;

            this._chargeTestOrder = GetChargeTestOrder(maxCharge);

            this.maxIsotopes = maxIsotopes;
            this.maxQuery = maxQuery;

            this.intensityThreshold = intensityThreshold;
            this.applyDeconvForPairSearching = applyDeconvForPairSearching;
            this.fullPairsOnly = fullPairsOnly;
            this.addPairIntensity = addPairIntensity;

            if (protonMass == null)
                proton = (float)1.00782503214;
            else
                proton = (float)protonMass;

            if (isotopeShift == null)
                this.isotopeMass = 1.00335483778f;
            else
                this.isotopeMass = (float)isotopeShift;

        }

        private List<(int c1, int c2)> GetChargeTestOrder(int maxCharge)
        {
            var array = new List<(int c1, int c2)>(maxCharge * maxCharge);

            for (int c1 = 1; c1 <= maxCharge; c1++)
            {
                for (int c2 = 1; c2 <= maxCharge; c2++)
                {
                    array.Add((c1, c2));
                }
            }
            array = array.OrderByDescending(a => a.c1 == a.c2).ThenByDescending(a => a.c1 + a.c2).ToList();

            return array;
        }


        public List<IonPair> GetPairs(MSUltraLight ms)
        {
            return GetPairs(ms.Ions, ms.Precursors[0].MZ, ms.Precursors[0].Z);
        }

        public List<IonPair> GetPairs(List<(double MZ, double Intensity)> allIons, double precMZ, int precZ)
        {
            var allCandidates = new List<CleaveCandidate>();

            double absIntensityThreshold = allIons.Max(a => a.Item2) * intensityThreshold;

            var ions = allIons
                .Where(a => a.Item2 > absIntensityThreshold).ToList();

            double reagentShift = reagent.DeltaShift;
            double precursorMH = ToMH(precMZ, precZ, proton);
            List<IonPair> pairs = new();

            for (int i = 0; i < ions.Count; i++)
            {
                IonPair pair = SearchForPairFromLightIon(
                    ions[i],
                    ions,
                    reagentShift,
                    out double mh_light);

                if (pair == null) continue;
                pairs.Add(pair);
            }

            return pairs;
        }

        public List<CleaveCandidate> FindCleaveCandidates(MSUltraLight ms)
        {
            return FindCleaveCandidates(ms.ScanNumber, ms.Ions, ms.Precursors[0].MZ, ms.Precursors[0].Z);
        }

        public List<CleaveCandidate> FindCleaveCandidates(int scanNumber, List<(double MZ, double Intensity)> allIons, double precMZ, int precZ)
        {
            if (allIons.Count == 0) return new List<CleaveCandidate>();
            var allCandidates = new List<CleaveCandidate>();

            double absIntensityThreshold = allIons.Max(a => a.Intensity) * intensityThreshold;

            var ions = allIons
                .Where(a => a.Item2 > absIntensityThreshold).ToList();

            ions = ions.OrderBy(a => a.MZ).ToList();

            double reagentShift = reagent.DeltaShift;
            double precursorMH = ToMH(precMZ, precZ, proton);

            for (int i = 0; i < ions.Count; i++)
            {
                IonPair pair = SearchForPairFromLightIon(ions[i], ions,
                    reagentShift, out double mh_light);

                if (pair == null) continue;
                for (int precIsotope = 0; precIsotope <= maxIsotopes; precIsotope++)
                {
                    double adjustedPrecursorMH = precursorMH - precIsotope * isotopeMass;

                    IonPair complementPair = SearchForComplementPair(
                        pair.SearchMass, adjustedPrecursorMH, ions, fullPairsOnly);

                    if (complementPair != null)
                    {
                        if (addPairIntensity == true)
                        {
                            double pairIntensity = PairUtils.GetPairIntensity(
                                pair.SearchMass, ions, reagent, ppm, out _, maxCharge, maxIsotopes);
                            double compPairIntensity = PairUtils.GetPairIntensity(
                                complementPair.SearchMass, ions, reagent, ppm, out _, maxCharge, maxIsotopes);
                            pair.Intensity = pairIntensity;
                            complementPair.Intensity = compPairIntensity;
                        }

                        allCandidates.Add(
                            new CleaveCandidate(
                                scanNumber,
                                pair,
                                complementPair,
                                false,
                                maxQuery,
                                precIsotope,
                                false));
                    }
                }
            }

            var distincted = allCandidates.Distinct(
                new CleaveCandidateComparer() { Ppm = ppm }).ToList();

            distincted = distincted
                .OrderByDescending(a => a.LightPairIntensity + a.HeavyPairIntensity)
                .ToList();

            return distincted;
        }

        private IonPair SearchForComplementPair(
            double sourcePepMH,
            double precursorMH,
            List<(double, double)> ions,
            bool fullPairsOnly)
        {
            double complementPepMH = precursorMH - sourcePepMH - reagent.WholeMass +
                SpectrumWizard.Utils.Chemistry.Hydrogen;
            double complementMH_light = complementPepMH + reagent.LightFragment;
            double complementMH_heavy = complementPepMH + reagent.HeavyFragment;

            (double, double)? lightComplement = null;
            int lightCharge = 0;
            (double, double)? heavyComplement = null;
            int heavyCharge = 0;

            for (int c = 1; c <= maxCharge; c++)
            {
                if (lightComplement == null)
                {
                    double targetLight = ToMZ(complementMH_light, c, proton);
                    int? lightComplementIndex = PeakSearching.BinarySearchForMZ(ions, targetLight, 50, 0, ions.Count - 1);
                    if (lightComplementIndex != null)
                        lightComplement = ions[(int)lightComplementIndex];
                    lightCharge = c;
                    if (lightComplement != null)
                        break;
                }
            }

            if (fullPairsOnly && lightComplement == null)
                return null;

            for (int c = 1; c <= maxCharge; c++)
            {
                if (heavyComplement == null)
                {
                    double targetHeavy = ToMZ(complementMH_heavy, c, proton);
                    int? heavyComplementIndex = PeakSearching.BinarySearchForMZ(ions, targetHeavy, ppm, 0, ions.Count - 1);
                    if (heavyComplementIndex != null)
                        heavyComplement = ions[(int)heavyComplementIndex];
                    heavyCharge = c;
                    if (heavyComplement != null)
                        break;
                }
            }

            if (lightComplement == null && heavyComplement == null)
                return null;
            else
            {
                if (lightComplement == null) lightCharge = 0;
                if (heavyComplement == null) heavyCharge = 0;

                return
                    new IonPair(
                        lightComplement, lightCharge,
                        heavyComplement, heavyCharge,
                        (float)reagent.LightFragment, (float)reagent.HeavyFragment);
            }
        }

        private IonPair SearchForPairFromLightIon(
            (double, double) lightIon,
            List<(double, double)> allIons,
            double re, out double mh_light)
        {
            mh_light = 0;

            for (int i = 0; i < _chargeTestOrder.Count; i++)
            {
                int c_light = _chargeTestOrder[i].c1;
                int c_heavy = _chargeTestOrder[i].c2;

                mh_light = ToMH(lightIon.Item1, c_light, proton);

                double target = HeavyMassCalc(mh_light, c_heavy, re);

                int? heavyIonIndex = PeakSearching.BinarySearchForMZ(allIons, target, ppm, 0, allIons.Count - 1);

                if (heavyIonIndex != null)
                {
                    return new IonPair(
                        lightIon,
                        c_light,
                        allIons[(int)heavyIonIndex],
                        c_heavy,
                        reagent.LightFragment,
                        reagent.HeavyFragment);
                }
            }

            return null;
        }

        public double HeavyMassCalc(double mh1, int c2, double reagentMass)
        {
            return (reagentMass + mh1 + proton * c2 - proton) / c2;
        }


    }

    internal class CleaveCandidateComparer : EqualityComparer<CleaveCandidate>
    {
        public double Ppm { get; set; }


        public override bool Equals(CleaveCandidate x, CleaveCandidate y)
        {
            if (Object.ReferenceEquals(x, y))
                return true;

            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            var ppm1 = Math.Abs(SpectraOperations.ScoringFunctions.GetPPM(x.LightPair.SearchMass, y.LightPair.SearchMass));

            if (x.IsLoopLink == false && y.IsLoopLink == false)
            {
                var ppm2 = Math.Abs(SpectraOperations.ScoringFunctions.GetPPM(x.HeavyPair.SearchMass, y.HeavyPair.SearchMass));

                return ppm1 < Ppm && ppm2 < Ppm;
            }
            else
                return ppm1 < Ppm;
        }


        public override int GetHashCode([DisallowNull] CleaveCandidate obj)
        {
            return 0;
        }
    }
}
