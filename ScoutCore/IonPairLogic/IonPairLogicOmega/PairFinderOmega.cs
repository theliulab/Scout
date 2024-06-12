using PatternTools.MSParserLight;
using SpectrumWizard.Predictors.CleavableXL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ScoutCore.SpectraOperations.ChargeOperations;

namespace ScoutCore.IonPairLogicOmega
{
    public class PairFinderOmega
    {
        private float ppm;
        private float intensityThreshold;
        private float relativeIntensityThreshold;
        private CleaveReagent reagent;

        private static float proton = (float) PatternTools.pTools.PROTONMASS;

        public PairFinderOmega(
            CleaveReagent reagent, 
            float ppm, 
            float intensityThreshold = 0.02f, 
            float? protonMass = null)
        {
            this.reagent = reagent;
            this.ppm = ppm;

            this.intensityThreshold = intensityThreshold;

            if (protonMass == null)
                proton = (float)PatternTools.pTools.PROTONMASS;
            else
                proton = (float)protonMass;
        }

        public List<CleaveDoublet> FindCandidatesDeconv(MSUltraLight ms)
        {
            var yada = new PatternTools.YADA.DeconvolutionSimple(
                ppm, 
                ms.Precursors.Max(a => a.Z));

            var envs = yada.DeconvoluteMS(0.5, ms.Ions, true);
            

            var pairs = GetSimplePairs(ms.Ions, 1, ppm, reagent);
            var doublets = GetDoublets(ms.Ions, pairs, ms.Precursors[0], 1, ppm, reagent);

            return doublets;
        }

        public List<CleaveDoublet> FindCandidates(MSUltraLight ms)
        {
            double absIntensityThreshold = ms.Ions
                .Max(a => a.Intensity) * relativeIntensityThreshold;

            var ions = ms.Ions
                .Where(a => a.Intensity > absIntensityThreshold)
                .ToList();

            double precursorMH = ToMH(ms.Precursors[0].MZ, ms.Precursors[0].Z, proton);

            var simplePairs = GetSimplePairs(ions, 3, ppm, reagent);
            List<CleaveDoublet> doublets = GetDoublets(ions, simplePairs, ms.Precursors[0], 3, ppm, reagent);

            return doublets;
        }

        private static List<CleaveDoublet> GetDoublets(
            List<(double MZ, double Intensity)> ions,
            List<IonPairSimple> simplePairs,
            (double MZ, short Z) precursor, 
            int maxCharge,
            float ppm,
            CleaveReagent reagent)
        {
            List<CleaveDoublet> r = new();

            simplePairs = simplePairs.OrderBy(a => a.LightMH).ToList();
            //simplePairs.Sort((a, b) => a.LightMH.CompareTo(b.HeavyMH));

            short precCharge = precursor.Z;

            if (precursor.Z <= 1) precCharge = 2;
            if (precursor.Z > maxCharge) precCharge = (short) maxCharge;

            List<int> chargeArray = Enumerable.Range(1, precursor.Z).ToList();

            double precursorMH = ToMH(precursor.MZ, precursor.Z, proton);

            List<double> addedMHs = new List<double>();

            foreach (IonPairSimple pair in simplePairs)
            {
                if (addedMHs.Any(a => PatternTools.pTools.PPM(pair.SearchMH, a) < 3*ppm))
                    continue;

                double complementLight = precursorMH - pair.HeavyMH + proton;
                double complementHeavy = precursorMH - pair.LightMH + proton;

                float minPepMH = 100;

                List<(int c, List<int> indexes)> matchesCompLight = 
                    chargeArray.Select(c =>
                        (c, BinarySearchForMZSpread(
                            ions,
                            ToMZ(complementLight, c, proton),
                            ppm, minPepMH, 0, ions.Count))).ToList();

                List<(int c, List<int> indexes)> matchesCompHeavy = 
                    chargeArray.Select(c =>
                        (c,BinarySearchForMZSpread(
                            ions,
                            ToMZ(complementHeavy, c, proton),
                            ppm, minPepMH, 0, ions.Count))).ToList();

                if (!matchesCompLight.Any(a => a.indexes != null) 
                    && !matchesCompHeavy.Any(a => a.indexes != null))
                    continue;

                var compGroup = new IonPairGroup()
                {
                    LightMH = complementLight,
                    HeavyMH = complementHeavy,
                    LightIons = matchesCompLight
                        .Where(a => a.indexes != null)
                        .ToDictionary(a => a.c, a => a.indexes.Select(i => ions[i]).ToList() ),
                    HeavyIons = matchesCompHeavy
                        .Where(a => a.indexes != null)
                        .ToDictionary(a => a.c, a => a.indexes.Select(i => ions[i]).ToList()),
                    Reagent = reagent
                };

                addedMHs.Add(pair.SearchMH);
                addedMHs.Add(complementLight - reagent.LightFragment);

                List<(int c, List<int> indexes)> matchesLight = 
                    chargeArray.Select(c =>
                        (c, BinarySearchForMZSpread(
                            ions,
                            ToMZ(pair.LightMH, c, proton),
                            ppm, minPepMH, 0, ions.Count))).ToList();

                List<(int c, List<int> indexes)> matchesHeavy = 
                    chargeArray.Select(c =>
                        (c, BinarySearchForMZSpread(
                            ions,
                            ToMZ(pair.HeavyMH, c, proton),
                            ppm, minPepMH, 0, ions.Count))).ToList();

                var regularGroup = new IonPairGroup()
                {
                    LightMH = pair.LightMH,
                    HeavyMH = pair.HeavyMH,
                    LightIons = matchesLight
                        .Where(a => a.indexes != null)
                        .ToDictionary(a => a.c, a => a.indexes.Select(i => ions[i]).ToList()),
                    HeavyIons = matchesHeavy
                        .Where(a => a.indexes != null)
                        .ToDictionary(a => a.c, a => a.indexes.Select(i => ions[i]).ToList()),
                    Reagent = reagent
                };

                matchesLight = 
                    chargeArray.Select(c =>
                        (c, BinarySearchForMZSpread(
                            ions,
                            ToMZ(pair.LightMH, c, proton),
                            ppm, minPepMH, 0, ions.Count))).ToList();

                if(compGroup.LightMH < regularGroup.LightMH)
                {
                    var aux = regularGroup;
                    regularGroup = compGroup;
                    compGroup = aux;
                }

                r.Add(
                    new CleaveDoublet()
                    {
                        LightPairGroup = regularGroup,
                        HeavyPairGroup = compGroup,
                        IsPrecursor = false
                    });

            }

            return r;
        }

        private static List<IonPairSimple> GetSimplePairs(List<(double MZ, double Intensity)> ions, 
            int maxCharge,
            float ppm, 
            CleaveReagent reagent)
        {
            var r = new List<IonPairSimple>();

            if (maxCharge <= 1) maxCharge = 2;
            List<int> chargeArray = Enumerable.Range(1, maxCharge).ToList();

            double reagentShift = reagent.DeltaShift;

            for (int i = 0; i < ions.Count; i++)
            {
                if (ions[i].MZ < 100) continue;

                for (int c1 = 1; c1 <= chargeArray.Count; c1++)
                {
                    double target_mh = ToMH(ions[i].MZ, c1, proton);

                    for (int c2 = 1; c2 <= chargeArray.Count; c2++)
                    {
                        double target = ToMZ(target_mh + reagentShift, c2, proton);
                        if (target > ions.Last().MZ) continue;
                        int? foundIndex = BinarySearchForMZ(
                            ions, 
                            target, 
                            ppm, 0,
                            ions.Count);

                        if (foundIndex != null)
                        {
                            r.Add(new IonPairSimple
                            (
                                lightIon : ions[i],
                                lightIonCharge : c1,
                                heavyIon : ions[(int)foundIndex],
                                heavyIonCharge : c2,
                                reagent : reagent
                            ) );

                            c1 = 100;
                            c2 = 100;
                        }
                    }
                }
            }

            return r;
        }

        private static int? BinarySearchForMZ(
            List<(double MZ, double Intensity)> allIons, 
            double target, 
            float ppm,
            int low, 
            int high)
        {
            double ppm_step = target * ppm / 1000000;
            double max_mz = target + ppm_step;
            double min_mz = target - ppm_step;

            while (low <= high)
            {
                int middle = ((low + high) / 2);
                double mz = allIons[middle].MZ;
                if (mz < max_mz && mz > min_mz)
                    return middle;
                else if (mz < target)
                    low = middle + 1;
                else
                    high = middle - 1;
            }

            return null;
        }

        private static List<int> BinarySearchForMZSpread(
            List<(double MZ, double Intensity)> allIons,
            double target,
            float ppm,
            float minPepMH,
            int low,
            int high)
        {
            if(target > allIons.Last().MZ 
                || target < allIons.First().MZ
                || target < minPepMH)
            {
                return null;
            }

            double ppm_step = target * ppm / 1000000;
            double max_mz = target + ppm_step;
            double min_mz = target - ppm_step;

            int? found = null;
            while (low <= high)
            {
                int middle = ((low + high) / 2);
                double mz = allIons[middle].MZ;

                if (mz < max_mz && mz > min_mz)
                {
                    found = middle;
                    break;
                }
                else if (mz < target)
                    low = middle + 1;
                else
                    high = middle - 1;
            }

            if (found == null) return null;

            List<int> r = new List<int>();
            int i = (int)found-1;
            while(i > 0 && allIons[i].MZ > min_mz)
            {
                i--;
            }
            i++;
            while(i < allIons.Count && allIons[i].MZ < max_mz)
            {
                r.Add(i);
                i++;
            }

            return r;
        }
    }
}
