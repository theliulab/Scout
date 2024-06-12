using SpectrumWizard.Predictors.CleavableXL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoutCore.IonPairLogicOmega
{
    public class IonPairGroup
    {
        public double LightMH { get; set; }
        public double HeavyMH { get; set; }

        public Dictionary<int, List<(double mz, double intensity)>> LightIons { get; set; }
        public Dictionary<int, List<(double mz, double intensity)>> HeavyIons { get; set; }

        public CleaveReagent Reagent { get; set; }

        public double SearchMass { get => LightMH - Reagent.LightFragment; }

        public override string ToString()
        {
            return $"SearchMass: {SearchMass}, " +
                $"LightMH: {LightMH:N4}, Ions: {LightIons.Sum(a => a.Value.Count)}, " +
                $"HeavyMH: {HeavyMH:N4}, Ions: {HeavyIons.Sum(a => a.Value.Count)}";
        }
    }
}
