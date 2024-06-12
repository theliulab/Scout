using MemoryPack;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScoutCore.IonPairLogic
{
    [MemoryPackable]
    [ProtoContract]
    public partial class IonPair
    {
        [ProtoMember(1)]
        public Tuple<float, float> LightIon { get; set; }
        [ProtoMember(2)]
        public int LightIonCharge { get; set; } = 0;

        [ProtoMember(3)]
        public Tuple<float, float> HeavyIon { get; set; }
        [ProtoMember(4)]
        public int HeavyIonCharge { get; set; } = 0;

        [ProtoMember(5)]
        public double SearchMass { get; set; } = 0;

        [ProtoMember(6)]
        public double Intensity { get; set; } = 0;

        [MemoryPackConstructor]
        public IonPair(Tuple<float, float> lightIon, int lightIonCharge, Tuple<float, float> heavyIon, int heavyIonCharge, double searchMass, double intensity)
        {
            LightIon = lightIon;
            LightIonCharge = lightIonCharge;
            HeavyIon = heavyIon;
            HeavyIonCharge = heavyIonCharge;
            SearchMass = searchMass;
            Intensity = intensity;
        }

        public IonPair(
            (double, double)? lightIon,
            int lightIonCharge,
            (double, double)? heavyIon,
            int heavyIonCharge,
            double lightFragmentMass,
            double heavyFragmentMass)
        {
            if(lightIon != null)
                LightIon = new Tuple<float,float>(
                    (float) (((double, double))lightIon).Item1, 
                    (float) (((double, double))lightIon).Item2);
            if(heavyIon != null)
                HeavyIon = new Tuple<float, float>(
                    (float) (((double, double))heavyIon).Item1, 
                    (float) (((double, double))heavyIon).Item2);

            LightIonCharge = lightIonCharge;
            HeavyIonCharge = heavyIonCharge;

            //TODO: This is a mess. Rework.
            SearchMass =
                LightIon != null ?
                    SpectraOperations.ChargeOperations.ToMH(
                        LightIon.Item1, LightIonCharge,
                        SpectrumWizard.Utils.Chemistry.Proton)
                    - lightFragmentMass
                    :
                    SpectraOperations.ChargeOperations.ToMH(
                        HeavyIon.Item1, HeavyIonCharge,
                        SpectrumWizard.Utils.Chemistry.Proton)
                    - heavyFragmentMass;
        }

        /// <summary>
        /// For BDP only
        /// </summary>
        /// <param name="lightIon"></param>
        /// <param name="lightIonCharge"></param>
        /// <param name="lightFragmentMass">StumpMass</param>
        /// <param name="heavyFragmentMass">ReporterMass</param>
        public IonPair(
            (double, double)? lightIon,
            int lightIonCharge,
            double searchMass)
        {
            if (lightIon != null)
                LightIon = new Tuple<float, float>(
                    (float)(((double, double))lightIon).Item1,
                    (float)(((double, double))lightIon).Item2);
            

            LightIonCharge = lightIonCharge;
            SearchMass = searchMass;
        }

        public IonPair() { }

        public override string ToString()
        {
            return $"SearchMass: {SearchMass}. Light: {LightIon},{LightIonCharge}. Heavy: {HeavyIon},{HeavyIonCharge}";
        }


    }
}