using ScoutCore.FileManagement;
using ScoutCore.IonPairLogic;
using ScoutCore.PSMEngines;
using PatternTools.MSParserLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScoutCore.QueryLogic
{
    public class SearchPack
    {
        public List<ScanResults> ScanResults { get; set; }

        public List<Query> ShotgunQueries { get; set; }
        public List<Query> CleaveQueries { get; set; }


        public SearchPack(List<ScanResults> scanResults, List<Query> shotgunQueries, List<Query> cleaveQueries)
        {
            ScanResults = scanResults;
            ShotgunQueries = shotgunQueries;
            CleaveQueries = cleaveQueries;
        }

        public List<Query> GetAllQueries()
        {
            List<Query> queries = new List<Query>();

            if (ShotgunQueries != null)
                queries.AddRange(ShotgunQueries);

            if (CleaveQueries != null)
                queries.AddRange(CleaveQueries);

            return queries.OrderBy(a => a.SearchMH)
                .ToList();
        }
    }
}
