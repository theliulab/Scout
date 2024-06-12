using ScoutCore.PSMEngines;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using MemoryPack;

namespace ScoutCore.QueryLogic
{
    [MemoryPackable]
    [ProtoContract]
    public partial class ShotgunCandidate 
    {
        [ProtoMember(1)]
        public int ScanNumber { get; set; }
        [ProtoMember(2)]
        public double MH { get; set; }
        [ProtoMember(3)]
        [MemoryPackAllowSerialize]
        public List<IPSM> PSMs { get; set; }

        [MemoryPackConstructor]
        public ShotgunCandidate(int scanNumber, double mH, List<IPSM> pSMs)
        {
            ScanNumber = scanNumber;
            MH = mH;
            PSMs = pSMs;
        }

        private ShotgunCandidate() { }

        public ShotgunCandidate(int scan, float mhPrec, int maxQueryResults)
        {
            ScanNumber = scan;
            MH = mhPrec;
            PSMs = new List<PSMEngines.IPSM>(maxQueryResults);
        }
    }
}
