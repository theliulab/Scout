using Digestor;
using MoreLinq;
using ScoutCore.IonPairLogic;
using ScoutCore.PSMEngines;
using ScoutCore.QueryLogic;
using ScoutCore.Results;
using ScoutCore.SpectraOperations;
using Newtonsoft.Json;
using PatternTools.FastaTools;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Ionic.Zip;
using MemoryPack;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace ScoutCore.Results
{
    [MemoryPackable]
    [ProtoContract]
    public partial class ScoutRawResults
    {
        [ProtoMember(1)]
        public string RawFile { get; set; }
        [ProtoMember(2)]
        public string FastaFile { get; set; }

        [ProtoMember(3)]
        public Dictionary<int, ScanResults> ScanResults { get; set; }

        [MemoryPackIgnore]
        public ScoutParameters _Params { get; set; }

        [ProtoMember(4)]
        public string ParamsSaveString { get; set; }

        [ProtoMember(5)]
        public double ProcessingTime { get; set; }

        [MemoryPackConstructor]
        public ScoutRawResults(string rawFile,
            string fastaFile,
            Dictionary<int, ScanResults> scanResults,
            string paramsSaveString,
            double processingTime)
        {
            this.RawFile = rawFile;
            this.FastaFile = fastaFile;
            this.ScanResults = scanResults;
            this.ParamsSaveString = paramsSaveString;
            this.ProcessingTime = processingTime;
        }

        public ScoutRawResults()
        {
            ScanResults = new Dictionary<int, ScanResults>();
        }

        public ScoutRawResults(string file, string fastaFile, List<ScanResults> scanResults, ScoutParameters parameters)
        {
            RawFile = file;
            FastaFile = fastaFile;
            _Params = parameters;

            ScanResults = new Dictionary<int, ScanResults>();
            foreach (var sr in scanResults)
            {
                ScanResults.Add(sr.ScanNumber, sr);
            }

            if (!_Params.MergeDatabase)
                MarkDecoys();
        }

        public ScoutRawResults(string file, string fastaFile, Dictionary<int, ScanResults> scanResults, ScoutParameters parameters)
        {
            RawFile = file;
            FastaFile = fastaFile;
            _Params = parameters;
            ScanResults = scanResults;

            if (!_Params.MergeDatabase)
                MarkDecoys();
        }

        private void MarkDecoys()
        {
            var fp = GetSearchFasta();

            var psms = GetAllPSMs();

            foreach (var psm in psms)
            {
                bool isDecoy = true;
                foreach (var mapping in psm.Peptide.Mappings)
                {
                    var prot = fp.MyItems[mapping.ProteinFastaIndex];

                    if (!prot.SequenceIdentifier.StartsWith(_Params.DecoyTag))
                    {
                        isDecoy = false;
                        break;
                    }
                }
                psm.Peptide.IsDecoy = isDecoy;
            }
        }

        private List<IPSM> GetAllPSMs()
        {
            var any1null = ScanResults.Values.SelectMany(a => a.CleaveCandidates).Where(a => a.AllLightPSMs == null).ToList();

            var any1null2not = ScanResults.Values.SelectMany(a => a.CleaveCandidates).Where(a => a.AllLightPSMs == null && a.AllHeavyPSMs != null).ToList();

            var any2null1not = ScanResults.Values.SelectMany(a => a.CleaveCandidates).Where(a => a.AllLightPSMs != null && a.AllHeavyPSMs == null).ToList();

            var cleavePSMs = ScanResults.Values
                .SelectMany(a => a.CleaveCandidates)
                .SelectMany(a =>
                {
                    if (a.AllLightPSMs != null && a.AllHeavyPSMs != null)
                        return a.AllLightPSMs.Concat(a.AllHeavyPSMs);
                    else if (a.AllLightPSMs != null && a.AllHeavyPSMs == null)
                        return a.AllLightPSMs;
                    else if (a.AllLightPSMs == null && a.AllHeavyPSMs != null)
                        return a.AllHeavyPSMs;
                    else
                        return new List<CleavePSM>();
                })
                .ToList();

            var shotgunPSMs = ScanResults.Values
                .Where(a => a.ShotgunCandidate != null && a.ShotgunCandidate.PSMs != null)
                .SelectMany(a => a.ShotgunCandidate.PSMs)
                .ToList();

            return cleavePSMs.Concat(shotgunPSMs).ToList();
        }

        public List<int> GetSearchedScanNumbers(
            bool wantLinears = true,
            bool wantCleaves = true)
        {
            List<int> scans = new List<int>();
            if (ScanResults == null)
                return scans;

            if (wantLinears)
            {
                List<int> linear = ScanResults
                    .Where(a => a.Value != null && a.Value.ShotgunCandidate != null && a.Value.ShotgunCandidate.PSMs.Any())
                    .Select(a => a.Key)
                    .Distinct()
                    .ToList();
                scans.AddRange(scans);
            }

            if (wantCleaves)
            {
                List<int> cleave = ScanResults
                    .Where(a => a.Value != null && a.Value.CleaveCandidates != null && a.Value.CleaveCandidates.Any())
                    .Where(a => a.Value != null && a.Value.CleaveCandidates != null && a.Value.CleaveCandidates.Any(b =>
                                        (b.AllHeavyPSMs != null && b.AllHeavyPSMs.Any()) ||
                                        (b.AllLightPSMs != null && b.AllLightPSMs.Any())
                                        ))
                    .Select(a => a.Key)
                    .Distinct()
                    .ToList();
                scans.AddRange(cleave);
            }

            return scans.Distinct().ToList();
        }
        public FastaFileParser GetSearchFasta(string fastaPath = null)
        {
            if (fastaPath == null)
                fastaPath = FastaFile;

            var digestionParams = _Params.AssembleDigestionParemeters();

            var fp = Digestor.Digestor.AssembleFasta(fastaPath, digestionParams);

            return fp;
        }
        public void SaveToProtoBufFile(string path)
        {
            ZipFile zipFile = ScoutCore.FileManagement.Serializer.CreateZipFile();
            int fileIndex = 1;
            SplitResultsToSendToProtBuf(zipFile, fileIndex);
            ScoutCore.FileManagement.Serializer.SaveZipFile(zipFile, path, fileIndex);
        }

        private void SplitResultsToSendToProtBuf(ZipFile zipFile, int fileIndex)
        {
            SaveParams(zipFile);
            int old_progress = 0;
            int total_file = ScanResults.Count;

            Console.Write("INFO: Saving file: 0%");
            for (int count = 0; count < total_file; count += ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED, fileIndex++)
            {
                var partial_results = new Results.ScoutRawResults(RawFile, FastaFile, ScanResults.Skip(count).Take(ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED).ToDictionary(x => x.Key, x => x.Value), _Params);
                ScoutCore.FileManagement.Serializer.AddProtoBufFileToZip(partial_results, zipFile, fileIndex);

                #region progress bar
                int new_progress = (int)((double)count / (total_file) * 100);
                if (new_progress > old_progress)
                {
                    old_progress = new_progress;
                    Console.Write("INFO: Saving file: " + old_progress + "%");
                }
                #endregion
            }
            fileIndex--;
        }

        private void SaveParams(ZipFile zipFile)
        {
            ParamsSaveString = FileManagement.Serializer.ToJSON(_Params, false, false);
            ScoutCore.FileManagement.Serializer.AddParamsZip(ParamsSaveString, zipFile, 1);
            ScoutCore.FileManagement.Serializer.AddParamsZip(ProcessingTime, zipFile, 2);
            ScoutCore.FileManagement.Serializer.AddParamsZip(RawFile, zipFile, 3);
            ScoutCore.FileManagement.Serializer.AddParamsZip(FastaFile, zipFile, 4);
        }

        public static ScoutRawResults Load(CancellationToken token, string pathScoutPack)
        {
            ScoutRawResults r = null;
            try
            {
                r = LoadFile(token, pathScoutPack);
            }
            catch (MemoryPack.MemoryPackSerializationException)
            {
                r = LoadFile(token, pathScoutPack, true);//It uses Protobuf lib to deserializer
            }
            catch (Exception)//Old version (it's not a zip file)
            {
                r = ScoutCore.FileManagement.Serializer.FromProtoBufBinary<ScoutRawResults>(pathScoutPack);

                if (r == null)
                    throw new Exception($"Unable to load the following buf file: {pathScoutPack}");
            }
            r._Params = FileManagement.Serializer.FromJson<ScoutParameters>(r.ParamsSaveString, true);

            return r;
        }

        private static ScoutRawResults LoadFile(
            CancellationToken token,
            string pathScoutPack, 
            bool isProtobuf = false)
        {
            int total_split_files = -1;
            int set_processed = 0;
            int old_progress = 0;
            ZipFile zipFile = ScoutCore.FileManagement.Serializer.OpenZipFile(pathScoutPack, out total_split_files);

            var total_results = new ScoutRawResults();

            Console.Write("INFO: Loading file: 0%");
            for (int count = 1; count <= total_split_files; count++)
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                var toDeserialize = ScoutCore.FileManagement.Serializer.GetPieceOfFile<ScoutRawResults>(count, zipFile, isProtobuf);
                if (toDeserialize != null && toDeserialize.ScanResults != null)
                {
                    foreach (KeyValuePair<int, QueryLogic.ScanResults> scan in toDeserialize.ScanResults)
                    {
                        total_results.ScanResults.Add(scan.Key, scan.Value);
                    }
                }

                #region progress bar
                set_processed++;
                int new_progress = (int)((double)set_processed / (total_split_files) * 100);
                if (new_progress > old_progress)
                {
                    old_progress = new_progress;
                    Console.Write("INFO: Loading file: " + old_progress + "%");
                }
                #endregion
            }

            LoadParams(total_results, zipFile, isProtobuf);
            return total_results;
        }

        private static void LoadParams(ScoutRawResults total_results, ZipFile zipFile, bool isProtobuf)
        {
            total_results.ParamsSaveString = ScoutCore.FileManagement.Serializer.GetParams<string>(1, zipFile, isProtobuf);
            total_results.ProcessingTime = ScoutCore.FileManagement.Serializer.GetParams<double>(2, zipFile, isProtobuf);
            total_results.RawFile = ScoutCore.FileManagement.Serializer.GetParams<string>(3, zipFile, isProtobuf);
            total_results.FastaFile = ScoutCore.FileManagement.Serializer.GetParams<string>(4, zipFile, isProtobuf);
        }
    }
}