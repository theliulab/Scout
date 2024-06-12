using System;
using System.Collections.Generic;
using System.Text;

namespace ScoutCore.IonPairLogicOmega
{
    public class CleaveDoublet
    {
        public int ScanNumber { get; set; }
        public IonPairGroup LightPairGroup { get; set; }
        public IonPairGroup HeavyPairGroup { get; set; }

        public bool IsPrecursor { get; set; }


        public override string ToString()
        {
            return
                $"Light Doublet PepMH: {LightPairGroup.SearchMass}." +
                $"Heavy Doublet PepMH: {HeavyPairGroup.SearchMass}.";
        }
    }
}
