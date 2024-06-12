using ScoutCore.SpectraOperations;
using SpectrumWizard;
using SpectrumWizard.Predictors.CleavableXL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ScoutCore.SpectraOperations.ChargeOperations;

namespace ScoutCore.IonPairLogic
{
    public static class PairUtils
    {
        
        public static 
            (List<(double mz, int charge, int iso)> lightIons, 
            List<(double mz, int charge, int iso)> heavyIons) 
            GetPairIons(
                double mh,
                CleaveReagent reagent,
                int maxCharge,
                int maxIsotope,
                double isotopeShift = 1.00335483778)
        {
            List<(double mz, int charge, int iso)> lightIons = new(maxCharge * (maxIsotope + 1));
            List<(double mz, int charge, int iso)> heavyIons = new(maxCharge * (maxIsotope + 1));
            
            double lightMH = mh + reagent.LightFragment;
            double heavyMH = mh + reagent.HeavyFragment;

            for (int charge = 1; charge <= maxCharge; charge++)
            {
                for (int iso = 0; iso <= maxIsotope; iso++)
                {
                    double targetLight = ToMZ(lightMH + iso * isotopeShift, charge);
                    double targetHeavy = ToMZ(heavyMH + iso * isotopeShift, charge);

                    lightIons.Add((targetLight, charge, iso));
                    heavyIons.Add((targetHeavy, charge, iso));
                }
            }

            return (lightIons, heavyIons);
        }

        public static
           List<(double mz, int charge, int iso, double intensity)> GetPairIonsMatches(
            List<(double mz, double intensity)> eIons,   
            List<(double mz, int charge, int iso)> tIons,
            double ppm)
        {
            tIons = tIons.OrderBy(a => a.mz).ToList();
            eIons = eIons.OrderBy(a => a.mz).ToList();

            var dict = MatchingHelper.GetMatchesIndex(
                tIons.Select(a => a.mz).ToList(),
                eIons.Select(a => a.mz).ToList(),
                ppm,
                false);

            List<(double mz, int charge, int iso, double intensity)> r = new();

            for (int i = 0; i < tIons.Count; i++)
            {
                var currentTIon = tIons[i];
                if (!dict.TryGetValue(i, out List<int> matches))
                {
                    r.Add(
                        (currentTIon.mz, 
                        currentTIon.charge, 
                        currentTIon.iso, 
                        0));
                    continue;
                }

                //continue;
                var mIons = matches.Select(a => eIons[a]);
                r.Add(
                    (currentTIon.mz, 
                    currentTIon.charge, 
                    currentTIon.iso, 
                    mIons.Sum(a => a.intensity)));
            }

            return r;
        }

        internal static double GetPairIntensity(
            double searchMass,
            List<(double mz, double intensity)> ions,
            CleaveReagent reagent,
            double ppm,
            out int matchCount,
            int maxCharge = 2,
            int maxIsotope = 1)
        {

            var (lightIons, heavyIons) = GetPairIons(searchMass, reagent, maxCharge, maxIsotope);

            double intensity = 0;
            matchCount = 0;
            foreach (var ion in lightIons.Concat(heavyIons))
            {
                int? match = PeakSearching.BinarySearchForMZ(ions, ion.mz, ppm);
                if (match != null)
                {
                    matchCount++;
                    intensity += ions[(int)match].intensity;
                }
            }

            return intensity;
        }
    }
}
