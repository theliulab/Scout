using SpectrumWizard.Predictors.CleavableXL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoutCore.IonPairLogicOmega
{
    internal class IonPairSimple
    {
        internal double LightMH { get; private set; }
        internal (double mz, double intensity) LightIon { get; set; }
        internal int LightIonCharge { get; set; } = 0;
        internal double HeavyMH { get; private set; }
        internal (double mz, double intensity) HeavyIon { get; set; }
        internal int HeavyIonCharge { get; set; } = 0;
        
        internal CleaveReagent Reagent { get; set; }
        

        internal double SearchMH { get =>
                SpectraOperations.ChargeOperations.ToMH(LightIon.mz, LightIonCharge) - Reagent.LightFragment; }


        public IonPairSimple(
            (double mz, double intensity) lightIon, int lightIonCharge, 
            (double mz, double intensity) heavyIon, int heavyIonCharge,
            CleaveReagent reagent)
        {
            LightIon = lightIon;
            LightIonCharge = lightIonCharge;
            HeavyIon = heavyIon;
            HeavyIonCharge = heavyIonCharge;
            Reagent = reagent;

            LightMH = SpectraOperations.ChargeOperations.ToMH(lightIon.mz, LightIonCharge);
            HeavyMH = SpectraOperations.ChargeOperations.ToMH(heavyIon.mz, HeavyIonCharge);
        }

        public override string ToString()
        {
            return
                $"SearchMass: {SearchMH:N3}" +
                $"Light MZ: {LightIon.mz:N3}, Z: {LightIonCharge}, " +
                $"Heavy MZ: {HeavyIon.mz:N3}, Z: {HeavyIonCharge}, "
                ;
        }
    }
}
