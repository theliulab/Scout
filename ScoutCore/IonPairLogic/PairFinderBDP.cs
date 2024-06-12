using CSMSL.Chemistry;
using PatternTools.MSParserLight;
using ScoutCore.QueryLogic;
using ScoutCore.SpectraOperations;
using SpectrumWizard.Predictors.CleavableXL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static ScoutCore.SpectraOperations.ChargeOperations;

namespace ScoutCore.IonPairLogic
{
    internal class PairFinderBDP
    {
        private CleaveReagent cXLReagent;
        private float pairFinderPPM;
        private int maxCharge;
        private int maxIsotopesPrecursor;
        private int maxQueryResults;

        private const double proton = 1.00727647;
        private const double isotope_shift = 1.00335483778;

        public PairFinderBDP(CleaveReagent cXLReagent, float pairFinderPPM, int v1, int v2, int maxQueryResults)
        {
            this.cXLReagent = cXLReagent;
            this.pairFinderPPM = pairFinderPPM;
            this.maxCharge = v1;
            this.maxIsotopesPrecursor = v2;
            this.maxQueryResults = maxQueryResults;


        }

        internal List<CleaveCandidate> FindCleaveCandidates(MSUltraLight ms)
        {
            //if(ms.ScanNumber == 27798)
            //{

            //}

            var allCandidates = new List<CleaveCandidate>();

            double stumpMass = cXLReagent.LightFragment;
            double reporterMass = cXLReagent.HeavyFragment;
            double precursorMH = ToMH(ms.Precursors[0].Item1, ms.Precursors[0].Item2, proton);


            for (int i = 0; i < ms.Ions.Count(); i++)
            {
                if(i == 51)
                {

                }
                var mass1 = ms.Ions[i].MZ;
                if (mass1 < 200)
                    continue;

                for (int isotope = 0; isotope <= maxIsotopesPrecursor; isotope++)
                {
                    double target = precursorMH - reporterMass - mass1 + proton - isotope * isotope_shift;

                    int? complement_index = PeakSearching.BinarySearchForMZ(ms.Ions, target, pairFinderPPM, 0, ms.Ions.Count - 1);

                    if (complement_index == null)
                        continue;

                    if (complement_index <= i)
                        continue;

                    var pair1 = new IonPair(
                        ms.Ions[i],
                        1,
                        ms.Ions[i].MZ - stumpMass
                        );

                    
                    var pair2 = new IonPair(
                        ms.Ions[(int)complement_index],
                        1,
                        ms.Ions[(int)complement_index].MZ - stumpMass
                        );

                    allCandidates.Add(
                         new CleaveCandidate(
                                ms.ScanNumber,
                                pair1,
                                pair2,
                                false,
                                maxQueryResults,
                                isotope,
                                false));
                }

               
            }
            var distincted = allCandidates.Distinct(
               new CleaveCandidateComparer() { Ppm = pairFinderPPM }).ToList();

            distincted = distincted
                .OrderByDescending(a => a.LightPair.LightIon.Item2 + a.HeavyPair.LightIon.Item2)
                .ToList();

            return distincted;
        }
    }
}
