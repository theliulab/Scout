using ScoutCore.SpectraOperations;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MemoryPack;

namespace ScoutCore.QueryLogic
{
    [MemoryPackable]
    [ProtoContract]
    public partial class ScanResults
    {
        [ProtoMember(1)]
        public int ScanNumber { get; set; }
        [ProtoMember(2)]
        public double RetentionTime { get; set; }
        [ProtoMember(3)]
        public short PrecursorCharge { get; set; }
        [ProtoMember(4)]
        public double PrecursorMZ { get; set; }
        [ProtoMember(6)]
        [MemoryPackAllowSerialize]
        public ShotgunCandidate ShotgunCandidate { get; set; }
        [ProtoMember(7)]
        [MemoryPackAllowSerialize]
        public List<ICleaveCandidate> CleaveCandidates { get; set; }

        [ProtoMember(8)]
        public Dictionary<string, double?> Scores { get; set; }

        [ProtoMember(9)]
        public string Spectrum { get; set; }

        [MemoryPackConstructor]
        public ScanResults(int scanNumber, double retentionTime, short precursorCharge, double precursorMZ, ShotgunCandidate shotgunCandidate, List<ICleaveCandidate> cleaveCandidates, Dictionary<string, double?> scores, string spectrum)
        {
            ScanNumber = scanNumber;
            RetentionTime = retentionTime;
            PrecursorCharge = precursorCharge;
            PrecursorMZ = precursorMZ;
            ShotgunCandidate = shotgunCandidate;
            CleaveCandidates = cleaveCandidates;
            Scores = scores;
            Spectrum = spectrum;
        }

        public ScanResults() { }

        public ScanResults(int scanNumber, double cromatographyRetentionTime,
            double precMZ, short charge)
        {
            ScanNumber = scanNumber;
            RetentionTime = cromatographyRetentionTime;
            PrecursorCharge = charge;
            PrecursorMZ = precMZ;
            CleaveCandidates = new();
            Scores = new();
        }

        //public ICleaveCandidate GetBestCleaveCandidate()
        //{
        //    if (CleaveCandidates == null || CleaveCandidates.Count == 0) return null;

        //    var ok = CleaveCandidates
        //        .Where(a => a.LightPSMs != null && a.LightPSMs.Count != 0)
        //        .Where(a => a.HeavyPSMs != null && a.HeavyPSMs.Count != 0);

        //    if (!ok.Any())
        //        return null;

        //    var ordered = ok.OrderByDescending(a => 
        //        //ScoringFunctions.XLScoreLengthWeighted(a.LightPSMs.First(), a.HeavyPSMs.First()) 
        //        a.XLScore
        //        ).ToList();

        //    return ordered.First();
        //}

        public List<ICleaveCandidate> GetBestCleaveCandidates(int howMany)
        {
            //Return null if candidates does not have at least 1 PSM in each psm list
            var ok = CleaveCandidates
                .Where(a => (!a.IsLoopLink &&
                            (a.AllLightPSMs != null && a.AllLightPSMs.Count != 0) &&
                             a.AllHeavyPSMs != null && a.AllHeavyPSMs.Count != 0) ||
                             a.IsLoopLink &&
                             a.AllLightPSMs != null && a.AllLightPSMs.Count != 0)
                .ToList();
            if (ok.Count() == 0)
                return null;

            //var ordered = ok.OrderByDescending(a => a.XLScore).ToList();
            var ordered = ok.OrderByDescending(a => a.XLScore).ToList();

            if (howMany > ordered.Count)
                howMany = ordered.Count;

            return ordered.Take(howMany)
                .ToList();
        }
    }
}
