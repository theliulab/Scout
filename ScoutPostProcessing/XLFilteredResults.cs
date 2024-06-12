using MoreLinq;
using ScoutCore;
using ScoutCore.PSMEngines;
using ScoutCore.QueryLogic;
using ScoutCore.Results;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.Models;
using ScoutPostProcessing.PPILogic;
using ScoutPostProcessing.ProteinLogic;
using ScoutPostProcessing.ResiduePairLogic;
using ScoutPostProcessing.Scoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;
using Ionic.Zip;
using MemoryPack;
using System.Threading.Tasks;
using Accord.Math.Geometry;

namespace ScoutPostProcessing
{
    [MemoryPackable]
    public partial class XLFilteredResults
    {
        public static XLFilteredResults Results { get; private set; }
        public static List<Task<XLFilteredResults>> AllTasksReadFile { get; set; }
        [MemoryPackIgnore]
        private const string CONTAMINAT = "contaminant";
        [MemoryPackIgnore]
        public ScoutParameters SearchParams { get; set; }
        public string ParamsSearchSaveString { get; set; }
        [MemoryPackIgnore]
        public PostProcessingParameters PostParams { get; set; }
        public string ParamsPostSaveString { get; set; }

        [MemoryPackAllowSerialize]
        public CSMPackage PackageCSMs { get; set; }
        [MemoryPackAllowSerialize]
        public CSMPackage PackageCSMsLoopLinks { get; set; }

        [MemoryPackAllowSerialize]
        public ResiduePairPackage PackageResPairs { get; set; }

        [MemoryPackAllowSerialize]
        public PPIPackage PackagePPIs { get; set; }
        public string FastaPath { get; set; }
        public string[] BufFiles { get; set; }
        public double TotalProcessingTime { get; set; }
        public string RawsFolder { get; set; }
        public Dictionary<string, int> ProteinScores { get; set; }
        public string FileVersion { get; set; }

        private XLFilteredResults() { }



        public XLFilteredResults(
            string[] msFiles,
            string fastaPath,
            CSMPackage cSMPackage,
            CSMPackage packageCSMsLoop,
            ResiduePairPackage respairPack,
            PPIPackage ppiPack,
            Dictionary<string, int> proteinScores,
            ScoutParameters searchParams,
            PostProcessingParameters postParams,
            string fileVersion)
        {
            BufFiles = msFiles;
            FastaPath = fastaPath;

            PackageCSMs = cSMPackage;
            PackageCSMsLoopLinks = packageCSMsLoop;
            PackageResPairs = respairPack;
            PackagePPIs = ppiPack;
            ProteinScores = proteinScores;
            SearchParams = searchParams;
            PostParams = postParams;
            FileVersion = fileVersion;
        }

        [MemoryPackConstructor]
        public XLFilteredResults(
            string paramsSearchSaveString,
            string paramsPostSaveString,
            CSMPackage packageCSMs,
            CSMPackage packageCSMsLoopLinks,
            ResiduePairPackage packageResPairs,
            PPIPackage packagePPIs,
            string fastaPath,
            string[] bufFiles,
            double totalProcessingTime,
            string rawsFolder,
            Dictionary<string, int> proteinScores,
            string fileVersion)
        {
            ParamsSearchSaveString = paramsSearchSaveString;
            ParamsPostSaveString = paramsPostSaveString;
            PackageCSMs = packageCSMs;
            PackageCSMsLoopLinks = packageCSMsLoopLinks;
            PackageResPairs = packageResPairs;
            PackagePPIs = packagePPIs;
            FastaPath = fastaPath;
            BufFiles = bufFiles;
            TotalProcessingTime = totalProcessingTime;
            RawsFolder = rawsFolder;
            ProteinScores = proteinScores;
            FileVersion = fileVersion;
        }

        public bool SaveResultsToProtoBuf(
            string savePath)
        {
            Console.WriteLine("INFO: Creating ZIP file...");
            ZipFile zipFile = ScoutCore.FileManagement.Serializer.CreateZipFile();
            Console.WriteLine("INFO: Done...");

            int fileIndex = 1;
            SplitResultsToSendToProtBuf(zipFile, ref fileIndex);
            ScoutCore.FileManagement.Serializer.SaveZipFile(zipFile, savePath, fileIndex);

            return true;
        }

        private void SplitResultsToSendToProtBuf(ZipFile zipFile, ref int fileIndex)
        {
            Console.WriteLine("INFO: Saving parameters...");
            SaveParams(zipFile);
            Console.WriteLine("INFO: Done...");

            int old_progress = 0;
            int total_file = PackageCSMs.AllCSMs.Count;

            Console.Write("INFO: Saving file: 0%");
            int count = 0;
            try
            {
                //small pieces. These pieces are saved in the same zip file, but in different FileCompressed subfiles.
                for (count = 0; count < total_file; count += ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED, fileIndex++)
                {
                    CSMPackage partial_csmPackage = new CSMPackage(
                        PackageCSMs.AllCSMs.Skip(count).Take(ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED).ToList(),
                        PostParams.FDRMode,
                        PostParams.CSM_FDR);
                    CSMPackage partial_csm_loopPackage = new CSMPackage(
                        PackageCSMsLoopLinks.AllCSMs.Skip(count).Take(ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED).ToList(),
                        PostParams.FDRMode,
                        PostParams.CSM_FDR);
                    ResiduePairPackage partial_ResPairPackage = new ResiduePairPackage(
                        PackageResPairs.AllResPairs.Skip(count).Take(ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED).ToList(),
                        PostParams.FDRMode,
                        PostParams.ResPair_FDR);
                    PPIPackage partial_PPIPackage = new PPIPackage(
                        PackagePPIs.AllPPIs.Skip(count).Take(ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED).ToList(),
                        PostParams.FDRMode,
                        PostParams.PPI_FDR);
                    var partial_results = new XLFilteredResults(
                        null,
                        null,
                        partial_csmPackage,
                        partial_csm_loopPackage,
                        partial_ResPairPackage,
                        partial_PPIPackage,
                        null,
                        null,
                        null,
                        null);
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
            }
            catch (Exception e2)
            {
                Console.WriteLine("ERROR: SplitResultsToSendToProtBuf method. ITEMS_TO_BE_SAVED = " + count + "\n" + e2.StackTrace + "\n" + e2.Message);
                throw;
            }
            fileIndex--;
        }

        private void SaveParams(ZipFile zipFile)
        {
            ParamsSearchSaveString = ScoutCore.FileManagement.Serializer.ToJSON(SearchParams, false, false);
            ParamsPostSaveString = ScoutCore.FileManagement.Serializer.ToJSON(PostParams, false, false);

            ScoutCore.FileManagement.Serializer.AddParamsZip(ParamsSearchSaveString, zipFile, 1);
            ScoutCore.FileManagement.Serializer.AddParamsZip(ParamsPostSaveString, zipFile, 2);
            ScoutCore.FileManagement.Serializer.AddParamsZip(FastaPath, zipFile, 3);
            ScoutCore.FileManagement.Serializer.AddParamsZip(BufFiles, zipFile, 4);
            ScoutCore.FileManagement.Serializer.AddParamsZip(TotalProcessingTime, zipFile, 5);
            ScoutCore.FileManagement.Serializer.AddParamsZip(RawsFolder, zipFile, 6);
            ScoutCore.FileManagement.Serializer.AddParamsZip(ProteinScores, zipFile, 7);
            ScoutCore.FileManagement.Serializer.AddParamsZip(FileVersion, zipFile, 8);
        }


        [STAThread]
        public static XLFilteredResults Load(string[] loadPaths, bool isProtobuf = false)
        {
            List<string> fasta_files = new();
            List<string> search_params = new();
            List<string> file_errors = new();
            foreach (var loadPath in loadPaths)
            {
                try
                {
                    ZipFile zipFile = ScoutCore.FileManagement.Serializer.OpenZipFile(loadPath, out int total_split_files);

                    var total_results = new XLFilteredResults();
                    LoadParams(total_results, zipFile, isProtobuf);
                    search_params.Add(total_results.ParamsSearchSaveString);
                    fasta_files.Add(total_results.FastaPath);
                }
                catch (Exception)
                {
                    file_errors.Add(loadPath);
                    continue;
                }
            }

            if (search_params.Count != loadPaths.Length)
                throw new Exception($"ERROR: Unable to read the following result file(s): {String.Join(", ", file_errors)}");

            fasta_files = fasta_files.Distinct().ToList();
            search_params = search_params.Distinct().ToList();
            if (search_params.Count > 1 || fasta_files.Count > 1)
                throw new Exception("ERROR: Result files contain different parameters.");

            XLFilteredResults final_results = Load(loadPaths[0]);
            for (int i = 1; i < loadPaths.Length; i++)
            {
                var loadPath = loadPaths[i];
                var current_results = Load(loadPath);
                MergeResults(final_results, current_results);
            }
            return final_results;
        }

        [STAThread]
        public static XLFilteredResults Load(string loadPath)
        {
            XLFilteredResults r = null;
            try
            {
                r = LoadFile(loadPath);
            }
            catch (MemoryPack.MemoryPackSerializationException)
            {
                Console.WriteLine("WARNING: Results file has been generated using an older version of Scout.\nSome results may display errors. Please run the dataset again in this newer version.");
                r = LoadFile(loadPath, true);//It uses Protobuf lib to deserializer
            }
            catch (Exception)//Old version (it's not a zip file)
            {
                Console.WriteLine("WARNING: Results file has been generated using an older version of Scout.\nSome results may display errors. Please run the dataset again in this newer version.");
                r = ScoutCore.FileManagement.Serializer.FromProtoBufBinary<XLFilteredResults>(loadPath);
            }


            #region remove contaminants

            if (r.SearchParams.DontShowContaminants)
            {
                r.PackageCSMs = new CSMPackage(
                    r.PackageCSMs.AllCSMs.Where(a => !((a.AlphaMappings.Any(b => b.Locus != null && b.Locus.ToLower().Contains(CONTAMINAT))) || (a.BetaMappings.Any(b => b.Locus != null && b.Locus.ToLower().Contains(CONTAMINAT))))).ToList(),
                    r.PostParams.FDRMode, r.PostParams.CSM_FDR);
                r.PackageCSMsLoopLinks = new CSMPackage(
                   r.PackageCSMsLoopLinks.AllCSMs.Where(a => !
                   a.AlphaMappings.Any(b => b.Locus != null && b.Locus.ToLower().Contains(CONTAMINAT))
                   ).ToList(),
                   r.PostParams.FDRMode, r.PostParams.CSM_FDR);
                r.PackageResPairs = new ResiduePairPackage(
                    r.PackageResPairs.AllResPairs.Where(a => !a.CSMs.Any(b => (b.AlphaMappings.Any(c => c.Locus != null && c.Locus.ToLower().Contains(CONTAMINAT)))
                    ||
                    (b.BetaMappings != null && b.BetaMappings.Any(c => c.Locus != null && c.Locus.ToLower().Contains(CONTAMINAT))))
                    ).ToList(),
                    r.PostParams.FDRMode, r.PostParams.ResPair_FDR);
                r.PackagePPIs = new PPIPackage(
                    r.PackagePPIs.AllPPIs.Where(a => !a.PPI_ID.Contains(CONTAMINAT)).ToList(),
                    r.PostParams.FDRMode, r.PostParams.PPI_FDR);
            }

            #endregion

            return r;
        }

        private static XLFilteredResults LoadFile(string loadPath, bool isProtobuf = false)
        {
            int set_processed = 0;
            int old_progress = 0;
            int total_split_files = -1;
            ZipFile zipFile = ScoutCore.FileManagement.Serializer.OpenZipFile(loadPath, out total_split_files);

            var total_results = new XLFilteredResults();
            LoadParams(total_results, zipFile, isProtobuf);

            List<ScoredCSM> AllCSMs = new List<ScoredCSM>(total_split_files * ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED);
            List<ScoredCSM> AllCSMsLoop = new List<ScoredCSM>(total_split_files * ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED);
            List<ResPair> AllResPairs = new List<ResPair>();
            List<PPI> AllPPIs = new List<PPI>();

            Console.Write("INFO: Loading file: 0%");

            for (int count = 1; count <= total_split_files; count++)
            {
                var toDeserialize = ScoutCore.FileManagement.Serializer.GetPieceOfFile<XLFilteredResults>(count, zipFile, isProtobuf);
                if (toDeserialize != null)
                {
                    if (toDeserialize.PackageCSMs != null && toDeserialize.PackageCSMs.AllCSMs != null)
                        AllCSMs.AddRange(toDeserialize.PackageCSMs.AllCSMs);
                    if (toDeserialize.PackageCSMsLoopLinks != null && toDeserialize.PackageCSMsLoopLinks.AllCSMs != null)
                        AllCSMsLoop.AddRange(toDeserialize.PackageCSMsLoopLinks.AllCSMs);
                    if (toDeserialize.PackageResPairs != null && toDeserialize.PackageResPairs.AllResPairs != null)
                        AllResPairs.AddRange(toDeserialize.PackageResPairs.AllResPairs);
                    if (toDeserialize.PackagePPIs != null && toDeserialize.PackagePPIs.AllPPIs != null)
                        AllPPIs.AddRange(toDeserialize.PackagePPIs.AllPPIs);
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
            total_results.PackageCSMs = new CSMPackage(AllCSMs, total_results.PostParams.FDRMode, total_results.PostParams.CSM_FDR);
            total_results.PackageCSMsLoopLinks = new CSMPackage(AllCSMsLoop, total_results.PostParams.FDRMode, total_results.PostParams.CSM_FDR);
            total_results.PackageResPairs = new ResiduePairPackage(AllResPairs, total_results.PostParams.FDRMode, total_results.PostParams.ResPair_FDR);
            total_results.PackagePPIs = new PPIPackage(AllPPIs, total_results.PostParams.FDRMode, total_results.PostParams.PPI_FDR);

            return total_results;
        }
        private static void MergeResults(XLFilteredResults current_results, XLFilteredResults new_results)
        {
            string[] merge_buffiles = new string[current_results.BufFiles.Length + new_results.BufFiles.Length];
            Array.Copy(current_results.BufFiles, merge_buffiles, current_results.BufFiles.Length);
            Array.Copy(new_results.BufFiles, 0, merge_buffiles, current_results.BufFiles.Length, new_results.BufFiles.Length);
            current_results.BufFiles = merge_buffiles;
            if (!current_results.FileVersion.Equals(new_results.FileVersion))
                current_results.FileVersion = "0.0.0";
            current_results.FastaPath = new_results.FastaPath;
            current_results.ParamsPostSaveString = new_results.ParamsPostSaveString;
            current_results.ParamsSearchSaveString = new_results.ParamsSearchSaveString;
            current_results.PostParams = new_results.PostParams;
            current_results.SearchParams = new_results.SearchParams;

            foreach (var kvp in new_results.ProteinScores)
            {
                if (current_results.ProteinScores.ContainsKey(kvp.Key))
                    current_results.ProteinScores[kvp.Key] += kvp.Value;
                else
                    current_results.ProteinScores.Add(kvp.Key, kvp.Value);
            }

            if (!current_results.RawsFolder.Equals(new_results.RawsFolder))
                current_results.RawsFolder = "Mixed files";

            current_results.TotalProcessingTime += new_results.TotalProcessingTime;
            current_results.PackageCSMs.AllCSMs.AddRange(new_results.PackageCSMs.AllCSMs);
            current_results.PackageCSMsLoopLinks.AllCSMs.AddRange(new_results.PackageCSMsLoopLinks.AllCSMs);
            current_results.PackageResPairs.AllResPairs.AddRange(new_results.PackageResPairs.AllResPairs);
            current_results.PackagePPIs.AllPPIs.AddRange(new_results.PackagePPIs.AllPPIs);
        }
        private static void LoadParams(XLFilteredResults total_results, ZipFile zipFile, bool isProtobuf = false)
        {
            total_results.ParamsSearchSaveString = ScoutCore.FileManagement.Serializer.GetParams<string>(1, zipFile, isProtobuf);
            total_results.ParamsPostSaveString = ScoutCore.FileManagement.Serializer.GetParams<string>(2, zipFile, isProtobuf);
            total_results.FastaPath = ScoutCore.FileManagement.Serializer.GetParams<string>(3, zipFile, isProtobuf);
            try
            {
                total_results.BufFiles = ScoutCore.FileManagement.Serializer.GetParams<string[]>(4, zipFile, isProtobuf);
            }
            catch (Exception)
            {
                total_results.BufFiles = new string[0];
            }
            total_results.TotalProcessingTime = ScoutCore.FileManagement.Serializer.GetParams<double>(5, zipFile, isProtobuf);
            total_results.RawsFolder = ScoutCore.FileManagement.Serializer.GetParams<string>(6, zipFile, isProtobuf);
            try
            {
                total_results.ProteinScores = ScoutCore.FileManagement.Serializer.GetParams<Dictionary<string, int>>(7, zipFile, isProtobuf);
            }
            catch (Exception)
            {
                total_results.ProteinScores = new();
            }
            total_results.FileVersion = ScoutCore.FileManagement.Serializer.GetParams<string>(8, zipFile, isProtobuf);

            total_results.SearchParams = ScoutCore.FileManagement.Serializer.FromJson<ScoutParameters>(
                total_results.ParamsSearchSaveString, true);
            total_results.PostParams = ScoutCore.FileManagement.Serializer.FromJson<PostProcessingParameters>(
                total_results.ParamsPostSaveString, true);
        }
    }
}
