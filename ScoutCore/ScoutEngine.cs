using Accord.Math;
using Digestor;
using PatternTools.FastaTools;
using ScoutCore.FileManagement;
using ScoutCore.PSMEngines;
using ScoutCore.PSMEngines.Tripper;
using ScoutCore.QueryLogic;
using ScoutCore.Results;
using SpectrumWizard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using ThermoFisher.CommonCore.Data.Business;

namespace ScoutCore
{
    public class ScoutEngine
    {
        private bool printToConsole;
        private string checksum_path_file;
        public ScoutParameters Params { get; private set; }

        public IEngine PSMEngine { get; set; }

        public ScoutEngine(ScoutParameters param)
        {
            Params = param;
        }

        public FileInfo SearchFile(string rawFile, string fastaFile, CancellationToken token)
        {
            IEngine psmEngine = SelectPSMEngine();

            Console.WriteLine($"===============\nINFO: Starting search...\n===============");
            var sw = Stopwatch.StartNew();

            ScoutRawResults r = null;
            try
            {
                Console.WriteLine("INFO: Preparing database...");
                BatchDigestor dbDigestor = psmEngine.PrepareDatabase(fastaFile);

                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                r = psmEngine.PerformSearch(dbDigestor, rawFile, token);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR - SearchFile method: It's not possible to perform search in the file: " + rawFile + ".\n" + e.StackTrace + "\n" + e.Message);
                throw;
            }
            r.ProcessingTime = sw.Elapsed.TotalSeconds;

            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();

            string saveBufPath = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(r.RawFile),
                        System.IO.Path.GetFileNameWithoutExtension(r.RawFile) + ".buf");
            r.SaveToProtoBufFile(saveBufPath);

            Console.WriteLine($"INFO: Search finished in {sw.Elapsed.TotalSeconds} seconds");

            return new FileInfo(saveBufPath);
        }

        private IEngine SelectPSMEngine()
        {
            return new Tripper(Params);
        }

        public void RemoveChecksumFile()
        {
            ScoutCore.FileManagement.Serializer.RemoveChecksumFile(checksum_path_file);
        }

        public List<FileInfo> RunDirectorySearch(
            string rawsDirectory, string fastaPath, CancellationToken token,
            bool recursive = true, bool printToConsole = true)
        {
            this.printToConsole = printToConsole;

            var psmEngine = SelectPSMEngine();
            BatchDigestor dbDigestor = null;

            Console.WriteLine($"===============\nINFO: Starting search...\n===============");

            try
            {
                Console.WriteLine("INFO: Preparing database...");
                dbDigestor = psmEngine.PrepareDatabase(fastaPath);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Unable to prepare the database.\n" + e.StackTrace + "\n" + e.Message);
                throw new Exception(e.StackTrace + "\n" + e.Message);
            }


            List<string> files = FileManagement.SpectrumParser.GetFilesFromDirectory(rawsDirectory, Params.MSFileExtension, recursive);

            if (files.Count == 0)
            {
                List<string> allRaws = Directory.GetFiles(
                    rawsDirectory,
                    "*.*",
                    SearchOption.AllDirectories).Where(s => s.ToLower().EndsWith(".ms2") ||
                    s.ToLower().EndsWith(".mgf") ||
                    s.ToLower().EndsWith(".raw")).ToList();
                allRaws.RemoveAll(a => a.Contains(".d"));

                List<string> extensions = (from ext in allRaws.AsParallel()
                                           select new FileInfo(ext).Extension).Distinct().ToList();

                List<string> brukerFiles = Directory.GetDirectories(rawsDirectory, "*.d", SearchOption.AllDirectories).ToList();
                if (brukerFiles.Count > 0)
                    extensions.Add(".d");

                if (extensions.Count > 0)
                {
                    if (extensions.Count > 1)
                        Console.WriteLine("WARNING: There are raw files with different extensions in the selected directory.");

                    Params.MSFileExtension = extensions[0];
                    files = new List<string>(allRaws);
                    if (brukerFiles.Count > 0) files.AddRange(brukerFiles);
                }
                else
                {
                    Console.WriteLine("ERROR: There are no raw files in the selected directory.");
                    return null;
                }

            }

            List<ScoutRawResults> currentResults = new List<ScoutRawResults>();
            List<FileInfo> bufFiles = new();
            int totalFiles = files.Count;
            int rawIndex = -1;

            #region verify if the control file exists

            checksum_path_file = System.IO.Path.Combine(
                       System.IO.Path.GetDirectoryName(files[0]), "_scout_files.processed_scout");

            string params_string = FileManagement.Serializer.ToJSON(Params, false, false);
            ScoutCore.FileManagement.Serializer.ChecksumFile(checksum_path_file, false, params_string, out rawIndex);
            #endregion

            rawIndex++;


            for (; rawIndex < totalFiles; rawIndex++)
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                string rawFile = files[rawIndex];
                Stopwatch sw_f = Stopwatch.StartNew();
                if (printToConsole)
                {
                    Console.WriteLine($"===============\nSearching {(rawIndex + 1)} / {totalFiles} File {rawFile}\n===============");
                }

                ScoutRawResults r = null;
                try
                {
                    if (dbDigestor == null)
                        throw new Exception("ERROR: Unable to prepare the database.");
                    r = psmEngine.PerformSearch(dbDigestor, rawFile, token);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: It's not possible to perform search in the file " + rawFile + ".\n" + e.Message);
                    throw;
                }
                r.ProcessingTime = sw_f.Elapsed.TotalSeconds;

                string saveBufPath = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(r.RawFile),
                        System.IO.Path.GetFileNameWithoutExtension(r.RawFile) + ".buf");
                r.SaveToProtoBufFile(saveBufPath);

                #region create/update control file to check if all RAW files have been processed
                ScoutCore.FileManagement.Serializer.StoreChecksumFile(rawIndex, totalFiles, checksum_path_file, params_string);
                #endregion

                Console.WriteLine($"INFO: Scout .buf file saved to {saveBufPath}");
                bufFiles.Add(new FileInfo(saveBufPath));

                if (printToConsole)
                {
                    Console.WriteLine($"INFO: Finished searching file in {sw_f.ElapsedMilliseconds / 60000.0} min");
                }
            }

            RemoveChecksumFile();

            return bufFiles;
        }
    }
}
