using MemoryPack;
using ScoutCore.PSMEngines;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScoutCore.QueryLogic
{
    [MemoryPackable]
    public partial class Query
    {
        public double SearchMH { get; set; }
        public int IsotopeNumber { get; set; }
        public (int index, double intensity)[] SparseBinnedMS { get; set; }
        public int ScanNumber { get; set; }
        public bool IsCleavable { get; set; } = false;
        public bool IsLoopLink { get; set; } = false;
        [MemoryPackAllowSerialize]
        public List<IPSM> PSMs { get; private set; }
        [MemoryPackConstructor]
        public Query(double searchMH, int isotopeNumber, (int index, double intensity)[] sparseBinnedMS, int scanNumber, bool isCleavable, bool isLoopLink, List<IPSM> pSMs)
        {
            SearchMH = searchMH;
            IsotopeNumber = isotopeNumber;
            SparseBinnedMS = sparseBinnedMS;
            ScanNumber = scanNumber;
            IsCleavable = isCleavable;
            IsLoopLink = isLoopLink;
            PSMs = pSMs;
        }

        internal Query(
            int scanNumber,
            double searchMH,
            int isotopeNumber,
            (int index, double intensity)[] sparseBinnedMS,
            bool isCleavable,
            bool isLoopLink,
            List<IPSM> psms)
        {
            ScanNumber = scanNumber;
            SearchMH = searchMH;
            IsotopeNumber = isotopeNumber;
            SparseBinnedMS = sparseBinnedMS;
            IsCleavable = isCleavable;
            IsLoopLink = isLoopLink;
            PSMs = psms;
        }

        public override string ToString()
        {
            return $"Scan {ScanNumber}, MH {SearchMH}, Iso: {IsotopeNumber}, PSMs: {PSMs.Count}, Cleave: {IsCleavable}";
        }
    }
}
