using Microsoft.FSharp.Core;
using PatternTools.MSParserLight;
using ScoutCore;
using ScoutCore.Results;
using Scout.Results;
using ScoutPostProcessing;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.PPILogic;
using ScoutPostProcessing.ResiduePairLogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ThermoFisher.CommonCore.Data.Business;
using ScoutPostProcessing.Output;
using System.Threading;

namespace Scout
{
    public class Program
    {
        private const string CONTAMINANT = "contaminant";
        private string _defaultRawCSMsCSVName = @"scout_raw_csms.csv";
        public ScoutParameters SearchParameters { get; set; }
        public PostProcessingParameters PostParameters { get; set; }
        public XLFilteredResults? PostResults { get; set; }
        public List<FileInfo> Packs { get; private set; }
        public bool FolderMode { get; set; }
        public string WarningMSG { get; set; }

        public event EventHandler ThreadDone;

        public Program()
        {
        }

        public void PerformSearch(CancellationToken token)
        {
            if (String.IsNullOrEmpty(SearchParameters.RawPath)) return;

            Console.WriteLine($"INFO: Starting run at {Path.GetDirectoryName(SearchParameters.RawPath)}.");
            Console.WriteLine($"INFO: Database: {Path.GetFileName(SearchParameters.FastaFile)}.");
            SearchParameters.PrintToConsole();
            Console.WriteLine();
            PostParameters.PrintToConsole();

            string _warnmsg = "";

            if (!FolderMode)
            {
                var pack = SearchFile(token);

                PostProcessing(token,
                    SearchParameters.FastaFile,
                    out _warnmsg,
                    pack);
            }
            else
            {
                var packs = SearchFolder(token);

                PostProcessing(token,
                    SearchParameters.FastaFile,
                    out _warnmsg,
                    packs.ToArray());
            }

            WarningMSG = _warnmsg;
        }

        private FileInfo SearchFile(CancellationToken token)
        {
            var se = new ScoutEngine(SearchParameters);
            var pack = se.SearchFile(SearchParameters.RawPath, SearchParameters.FastaFile, token);

            return pack;
        }

        private List<FileInfo> SearchFolder(CancellationToken token)
        {
            var se = new ScoutEngine(SearchParameters);
            try
            {
                List<FileInfo> packs = se.RunDirectorySearch(SearchParameters.RawPath, SearchParameters.FastaFile, token);
                return packs;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("The operation was canceled") ||
                    e.Message.Contains("The query has been canceled"))
                    se.RemoveChecksumFile();
                throw;
            }
        }

        public void PostProcessing(
            CancellationToken token,
           string fastaPath,
           out string errorMSG,
           params FileInfo[] pack_paths)
        {
            Console.WriteLine("==============\nINFO: Starting filter...\n==============");

            if (SearchParameters.PerformCleaveXLSearch)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var factory = new XLPostProcessingFactory(
                    SearchParameters,
                    PostParameters);
                XLFilteredResults? post = factory.AssemblePostProcessingResults(
                    token,
                   fastaPath,
                   pack_paths);
                string? rawsFolder = pack_paths[0].DirectoryName;
                errorMSG = factory.WarningMsg;

                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                string scoutSavePath = Path.Combine(rawsFolder, $"{Path.GetFileName(rawsFolder)}.scout");

                //Don't replace scout file
                int newID = 1;
                while (File.Exists(scoutSavePath))
                {
                    scoutSavePath = Path.Combine(rawsFolder, $"{Path.GetFileName(rawsFolder)}_{newID}.scout");
                    newID++;
                }

                XLFilteredResultsSaver.SaveResultsToBinaryFile(scoutSavePath, post);
                Console.WriteLine($"INFO: Scout results have been saved to {scoutSavePath}.");

                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Console.WriteLine("INFO: Search has been finished successfully!");
                if (post.TotalProcessingTime > 0)
                {
                    TimeSpan dt = TimeSpan.FromSeconds(post.TotalProcessingTime);
                    Console.WriteLine($"INFO: Idle : Processing time: {dt.Days} Day(s) {dt.Hours} Hour(s) {dt.Minutes} Minute(s) {dt.Seconds} Second(s)");
                }
                else
                    Console.WriteLine("INFO: Idle : Processing time: 0 seconds");

                PostResults = post;

                #region remove contaminants

                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                if (PostResults.SearchParams.DontShowContaminants)
                {
                    PostResults.PackagePPIs = new PPIPackage(
                        PostResults.PackagePPIs.AllPPIs.Where(a => !a.PPI_ID.Contains(CONTAMINANT)).ToList(),
                        PostParameters.FDRMode,
                        PostParameters.PPI_FDR);
                    PostResults.PackageResPairs = new ResiduePairPackage(
                        PostResults.PackageResPairs.AllResPairs.Where(a => !a.CSMs.Any(b => (b.AlphaMappings.Any(c => c.Locus != null && c.Locus.ToLower().Contains(CONTAMINANT))) || (b.BetaMappings.Any(c => c.Locus != null && c.Locus.ToLower().Contains(CONTAMINANT))))).ToList(),
                        PostParameters.FDRMode,
                        PostParameters.ResPair_FDR);
                    PostResults.PackageCSMs = new CSMPackage(
                        PostResults.PackageCSMs.AllCSMs.Where(a => !((a.AlphaMappings.Any(b => b.Locus != null && b.Locus.ToLower().Contains(CONTAMINANT))) || (a.BetaMappings.Any(b => b.Locus != null && b.Locus.ToLower().Contains(CONTAMINANT))))).ToList(),
                        PostParameters.FDRMode,
                        PostParameters.CSM_FDR);
                }

                #endregion
            }
            else
                errorMSG = "";
        }


        public void ImportSpectra(CancellationToken token,
                                  string rawsPath,
                                  ScoutPostProcessing.XLFilteredResults _post)
        {
            List<string> allNewRaws = Directory.GetFiles(
                rawsPath,
                "*.*",
                SearchOption.AllDirectories).Where(s => s.ToLower().EndsWith(".ms2") ||
                s.ToLower().EndsWith(".mgf") ||
                s.ToLower().EndsWith(".raw")).ToList();
            allNewRaws.RemoveAll(a => a.Contains(".d"));

            List<string> brukerFiles = Directory.GetDirectories(rawsPath, "*.d", SearchOption.AllDirectories).ToList();
            if (brukerFiles.Count > 0)
                allNewRaws.AddRange(brukerFiles);


            #region select csms
            List<ScoredCSM> allScoredCsms = _post.PackageCSMs.AllCSMs;
            allScoredCsms.AddRange(_post.PackageCSMsLoopLinks.AllCSMs);
            List<string> allIdentifiedRawFiles = (from spec in allScoredCsms
                                                  select spec.FileName).Distinct().ToList();
            #endregion

            #region select residue pairs
            ResiduePairPackage residuePairPackage = _post.PackageResPairs;
            List<ResPair> allResPairs = residuePairPackage.AllResPairs;
            #endregion

            #region select ppis
            PPIPackage ppiPackage = _post.PackagePPIs;
            List<PPI> allPPIs = ppiPackage.AllPPIs;
            #endregion

            for (int i = 0; i < allIdentifiedRawFiles.Count; i++)
            {
                Console.WriteLine($"INFO: Parsing file {(i + 1)} of {allIdentifiedRawFiles.Count}");

                #region select the current raw file
                string rawFile = allIdentifiedRawFiles[i];
                string fileName = Path.GetFileNameWithoutExtension(rawFile);
                string? newRawFile = allNewRaws.Where(a => Path.GetFileNameWithoutExtension(a).Equals(fileName))?.FirstOrDefault();
                if (String.IsNullOrEmpty(newRawFile))
                {
                    Console.WriteLine("WARNING: No spectra have been found in {0}.", newRawFile);
                    continue;
                }
                List<ScoredCSM> currentScoredCsms = allScoredCsms.Where(a => Path.GetFileNameWithoutExtension(a.FileName).Equals(Path.GetFileNameWithoutExtension(newRawFile))).ToList();
                if (currentScoredCsms == null || currentScoredCsms.Count == 0)
                {
                    Console.WriteLine("WARNING: No spectra have been found in {0}.", newRawFile);
                    continue;
                }

                List<int> searchedScanNumbers = (from csm in currentScoredCsms.AsParallel()
                                                 select csm.ScanNumber).Distinct().ToList();
                searchedScanNumbers.Sort((a, b) => a.CompareTo(b));

                var _spectra = ScoutCore.FileManagement.SpectrumParser.Parse(token,
                newRawFile, true, 2, 400, searchedScanNumbers)
                .ToDictionary(a => a.ScanNumber, a => a);
                #endregion

                #region import csms
                int threadsToUse = Environment.ProcessorCount - 2;

                try
                {
                    Parallel.ForEach(
                        currentScoredCsms,
                        new ParallelOptions()
                        {
                            MaxDegreeOfParallelism = threadsToUse
                        },
                        (csm) =>
                        {
                            MSUltraLight ms = _spectra[csm.ScanNumber];
                            csm.Spectrum = ScoutCore.FileManagement.Serializer.ToJSON(ms, false, false);
                        }
                    );
                    Console.WriteLine("INFO: Spectra have been imported successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: Could not import spectra!\n{0}\n{1}", ex.Message, ex.StackTrace);
                    throw;
                }
                #endregion

                #region update associated csms into ResPairs
                List<string> csms_ids = currentScoredCsms.Select(a => a.CSM_ID).ToList();
                List<ScoredCSM> allAssociatedResPairs = allResPairs.SelectMany(a => a.CSMs).Where(b => csms_ids.Contains(b.CSM_ID)).ToList();

                try
                {
                    Parallel.ForEach(
                        allAssociatedResPairs,
                        new ParallelOptions()
                        {
                            MaxDegreeOfParallelism = threadsToUse
                        },
                        (csm) =>
                        {
                            ScoredCSM updatedCSM = currentScoredCsms.Where(a => a.CSM_ID == csm.CSM_ID).First();
                            csm.Spectrum = updatedCSM.Spectrum;
                        }
                    );
                    Console.WriteLine("INFO: Residue pairs have been imported successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: Could not update residue pairs!\n{0}\n{1}", ex.Message, ex.StackTrace);
                    throw;
                }
                #endregion

                #region update associated residue pairs into ppis
                List<ScoredCSM> allAssociatedPPIs = allPPIs.SelectMany(a => a.GetAllCSMs()).Where(b => csms_ids.Contains(b.CSM_ID)).ToList();

                try
                {
                    Parallel.ForEach(
                        allAssociatedPPIs,
                        new ParallelOptions()
                        {
                            MaxDegreeOfParallelism = threadsToUse
                        },
                        (csm) =>
                        {
                            ScoredCSM updatedCSM = currentScoredCsms.Where(a => a.CSM_ID == csm.CSM_ID).First();
                            csm.Spectrum = updatedCSM.Spectrum;
                        }
                    );
                    Console.WriteLine("INFO: PPIs have been imported successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: Could not update ppis!\n{0}\n{1}", ex.Message, ex.StackTrace);
                    throw;
                }
                #endregion
            }
        }
    }
}
