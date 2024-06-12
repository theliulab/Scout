using Newtonsoft.Json;
using PythonRunner;
using ScoutCore;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.PPILogic;
using ScoutPostProcessing.ResiduePairLogic;
using ScoutPostProcessing.Scoring;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ScoutPostProcessing
{
    public class PythonScout
    {
        private readonly string m_AA00 = @"./PythonScripts/CSMScript.py";
        private readonly string m_AA01 = @"./PythonScripts/ResPairScript.py";
        private readonly string m_AA02 = @"./PythonScripts/PPIScript.py";
        private readonly string m_AA03 = @"./PythonScripts/final_scoring.py";
        private readonly static string m_AA04 = @"./PythonScripts/requirements.txt";
        private const int m_AA05 = 2;

        public ScoutParameters m_AB00 { get; set; }
        public PostProcessingParameters m_AB01 { get; }
        public FileInfo[] m_AB02 { get; }

        public PythonScout(
            ScoutParameters scoutParams,
            PostProcessingParameters postParams,
            System.IO.FileInfo[] bufFiles)
        {
            m_AB00 = scoutParams;
            m_AB01 = postParams;
            m_AB02 = bufFiles;

            var m_AB03 = new PythonHelper();
            m_AB03.RunMode = RunMode.ProgressEvents;
            m_AB03.OnScriptError += internal_method3;
            m_AB03.OnScriptPrint += internal_method3;
        }

        public static bool VerifyPython()
        {
            try
            {
                bool hasPython = PythonHelper.TestPython(out string message);

                if (hasPython == false)
                    throw new Exception(message);
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR: Python is not recognized.\n" + ex.Message + "\n" + ex.StackTrace);
            }

            try
            {
                var ph = new PythonHelper()
                {
                    RunMode = RunMode.ProgressEvents
                };


                System.Diagnostics.DataReceivedEventHandler printEvent = (object sender, DataReceivedEventArgs e) =>
                {
                    if (e.Data != null)
                    {
                        Console.WriteLine(e.Data);
                    }
                };

                ph.OnScriptPrint += printEvent;
                ph.OnScriptError += printEvent;

                bool setupPath = ph.SetupEnviromentVariable(EnvironmentVariableTarget.Process);
                if (setupPath == false)
                    throw new Exception($"ERROR: Could not set python enviroment variable.");

                bool updatePip = ph.TryUpdatePip();
                if (updatePip == false)
                    throw new Exception($"ERROR: Could not update pip.");

                bool installedPackages = ph.TryInstallPackages(m_AA04);
                if (installedPackages == false)
                    throw new Exception($"ERROR: Could not install python packages.");

            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR: It's not possible to set up Python.\n" + ex.Message + "\n" + ex.StackTrace);
            }

            return true;
        }

        private void internal_method0(string m_A000, List<IFDRElement> m_A001, List<string> m_A002, double m_A003)
        {
            Console.WriteLine("INFO: Creating classification script csv file...");
            FileInfo m_A007 = internal_method2(
                m_A001,
                m_A002);
            Console.WriteLine("INFO: Done...");

            Console.WriteLine("INFO: Creating json params to classification script...");
            FileInfo m_A008 = internal_method1();
            Console.WriteLine("INFO: Done...");

            var m_A004 = new PythonHelper();
            m_A004.RunMode = RunMode.ProgressEvents;
            m_A004.OnScriptError += internal_method3;
            m_A004.OnScriptPrint += internal_method3;
            m_A004.RunConsoleCommand(m_A000, m_A007.FullName, m_A008.FullName);

            if ((m_A004.OutputLog.FindIndex(a => a.StartsWith("ERROR: There is no")) != -1) ||
                m_A004.ErrorLog.Count > 0) return;

            string m_A005 = m_A004.OutputLog.Find(a => a.StartsWith("p:CSV:")).Replace("p:CSV:", "").Trim();

            var m_A006 = File.ReadLines(m_A005).ToList();
            Dictionary<string, List<string>> dict = m_A006.Skip(1)
                .Select(line => line.Split(','))
                .ToDictionary(line => line[0], line => line.Skip(1).ToList());

            foreach (var el in m_A001)
            {
                string[] headers = m_A006[0].Split(',');
                for (int i = 0; i < headers.Length; i++)
                {
                    if (headers[i] == "ID") continue;

                    double score = double.Parse(dict[el.ID][i - 1]);
                    if (headers[i] == "Score")
                    {
                        el.ClassificationScore = score;
                    }
                    else
                    {
                        if (el.Scores.ContainsKey(headers[i]))
                            el.Scores[headers[i]] = score;
                        else
                            el.Scores.Add(headers[i], score);
                    }

                }
            }

            Console.WriteLine("INFO: Removing tmp files...");
            try
            {
                File.Delete(m_A005);
                m_A007.Delete();
                m_A008.Delete();
                Console.WriteLine("INFO: Done...");
            }
            catch
            {
                Console.WriteLine("ERROR: Unable to remove tmp files...");
            }
        }
        private FileInfo internal_method1()
        {
            string output = JsonConvert.SerializeObject(m_AB01);
            string path = System.IO.Path.GetTempPath() + @$"{Guid.NewGuid().ToString()}_postParams.json";

            try
            {
                File.WriteAllText(path, output);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Could not create {path} file.\n" + ex.Message);
                throw new Exception($"Could not create {path} file.\n" + ex.Message);
            }

            return new FileInfo(path);
        }
        private FileInfo internal_method2(List<IFDRElement> m_A000, List<string> m_A001)
        {
            string path = System.IO.Path.GetTempPath() + $"{Guid.NewGuid().ToString()}_elements.csv";
            List<string> lines = new();
            lines.Add($"ID,IsDecoy,Class,{string.Join(',', m_A001)}");
            foreach (var el in m_A000)
            {
                string newLine = $"{el.ID},{el.IsDecoy},{el._Class}";

                foreach (var feature in m_A001)
                {
                    object score = el.Scores[feature];
                    if (score == null)
                        score = 0;
                    newLine += $",{score.ToString()}";
                }

                lines.Add(newLine);
            }

            try
            {
                File.WriteAllLines(path, lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Could not create {path} file.\n" + ex.Message);
                throw new Exception($"Could not create {path} file.\n" + ex.Message);
            }

            return new FileInfo(path);
        }
        private void internal_method3(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
                Console.WriteLine(e.Data);
        }
        private bool internal_method4(List<ScoredCSM> m_A000)
        {
            Console.WriteLine("INFO: Creating final_scoring.csv file...");
            FileInfo m_A001 = internal_method2(
                m_A000.Cast<IFDRElement>().ToList(),
                m_AB01.GetFinalScoringFeatures());
            Console.WriteLine("INFO: Done...");

            Console.WriteLine("INFO: Creating json params to final_scoring script...");
            FileInfo paramsJson = internal_method1();
            Console.WriteLine("INFO: Done...");

            var m_A002 = new PythonHelper();
            m_A002.RunMode = RunMode.ProgressEvents;
            m_A002.OnScriptError += internal_method3;
            m_A002.OnScriptPrint += internal_method3;

            bool m_A003 = m_A002.RunConsoleCommand(m_AA03, m_A001.FullName, paramsJson.FullName);
            string m_A004 = m_A002.OutputLog.Find(a => a.StartsWith("p:CSV:")).Replace("p:CSV:", "").Trim();
            Dictionary<string, List<string>> dict = File.ReadLines(m_A004)
                .Select(line => line.Split(','))
                .ToDictionary(line => line[0], line => line.Skip(1).ToList());

            foreach (var m_A005 in m_A000)
            {
                double score = double.Parse(dict[m_A005.ID.ToString()][0]);
                m_A005.Scores.Add("MinFinalScore", score);
            }

            Console.WriteLine("INFO: Removing final_scoring tmp files...");
            try
            {
                File.Delete(m_A004);
                m_A001.Delete();
                paramsJson.Delete();
                Console.WriteLine("INFO: Done...");
            }
            catch
            {
                Console.WriteLine("ERROR: Could not remove tmp files...");
            }

            return m_A003;
        }

        internal ResiduePairPackage GetResPairPackage(List<ResPair> respairs)
        {
            Console.WriteLine("INFO: Computing FDR on Residue Pair level...");

            if (respairs.All(a => !a.IsDecoy))
            {
                respairs.ForEach(a => a.ClassificationScore = 1);
                return new ResiduePairPackage(
                    respairs.OrderByDescending(a => a.ClassificationScore).ToList(),
                    m_AB01.FDRMode,
                    m_AB01.PPI_FDR
                    );
            }
            internal_method0(
                m_AA01,
                respairs.Cast<IFDRElement>().ToList(),
                m_AB01.ResPair_Features,
                m_AB01.ResPair_FDR
            );

            var r = new ResiduePairPackage(
                respairs.OrderByDescending(a => a.ClassificationScore).ToList(),
                m_AB01.FDRMode,
                m_AB01.ResPair_FDR);

            return r;
        }
        internal PPIPackage GetPPIPackage(List<PPI> ppis, bool uniqueOnly)
        {
            if (uniqueOnly)
                ppis = ppis.Where(a => a.IsUnique).ToList();

            if (ppis.All(a => !a.IsDecoy))
            {
                ppis.ForEach(a => a.ClassificationScore = 1);

                return new PPIPackage(
                    ppis.OrderByDescending(a => a.ClassificationScore).ToList(),
                    m_AB01.FDRMode,
                    m_AB01.PPI_FDR
                    );
            }

            Console.WriteLine("INFO: Computing FDR on PPI level...");

            var okPPIs = ppis
                .Where(a =>
                    (
                        a.Scores["UniquePepsA_Any"] > 1.5
                        &&
                        a.Scores["UniquePepsB_Any"] > 1.5
                    )
                    ||
                    a.IsDecoy)
                .ToList();
            internal_method0(
                m_AA02,
                okPPIs.Cast<IFDRElement>().ToList(),
                m_AB01.PPI_Features,
                m_AB01.PPI_FDR
            );

            var r = new PPIPackage(
                okPPIs.OrderByDescending(a => a.ClassificationScore).ToList(),
                m_AB01.FDRMode,
                m_AB01.PPI_FDR
                );

            return r;
        }
        internal CSMPackage GetCSMPackage(List<ScoredCSM> allCSMs, bool isLooplink = false)
        {
            if (allCSMs.Count == 0)
            {
                return new CSMPackage(new(),
                    m_AB01.FDRMode,
                    m_AB01.CSM_FDR);
            }

            if (allCSMs.All(a => !a.IsDecoy))
            {
                allCSMs.ForEach(a => a.ClassificationScore = 1);

                return new CSMPackage(
                    allCSMs.OrderByDescending(a => a.ClassificationScore).ToList(),
                    m_AB01.FDRMode,
                    m_AB01.PPI_FDR
                    );
            }

            bool isLoopLink = allCSMs.Count > 0 && allCSMs[0].IsLoopLink;

            List<string> features_to_use = null;
            if (isLooplink)
                m_AB01.IsLooplinkFilter = true;
            else
                m_AB01.IsLooplinkFilter = false;
            features_to_use = m_AB01.CSM_Features.ToList();

            if (m_AB01.UseFinalScoringCSMs)
            {
                var alphaTargetCount = allCSMs.Count(a => a.AlphaPSM.Peptide.IsDecoy == false);
                var alphaDecoyCount = allCSMs.Count(a => a.AlphaPSM.Peptide.IsDecoy == true);
                var betaTargetCount = allCSMs.Count(a => a.BetaPSM != null && a.BetaPSM.Peptide.IsDecoy == false);//BetaPSM is null when csm is looplink
                var betaDecoyCount = allCSMs.Count(a => a.BetaPSM != null && a.BetaPSM.Peptide.IsDecoy == true);//BetaPSM is null when csm is looplink

                if ((isLoopLink == false &&
                    (alphaTargetCount == 0
                    || alphaDecoyCount == 0
                    || betaTargetCount == 0
                    || betaDecoyCount == 0))
                    ||
                    (isLoopLink == true &&
                    (alphaTargetCount == 0
                    || alphaDecoyCount == 0)
                    ))
                {
                    Console.WriteLine($"Cannot run final_scoring.py. Not enough CSMs.");
                    m_AB01.UseFinalScoringCSMs = false;

                    features_to_use = features_to_use.Where(a => a != "MinFinalScore").ToList();

                    if (m_AB01.CSM_Features.Any(a => a == "MinFinalScore") == true)
                        m_AB01.CSM_Features.Remove("MinFinalScore");
                }
                else
                {
                    bool success = internal_method4(allCSMs);
                    if (success == false)
                    {
                        throw new Exception("Could not run python on CSMs final scoring scripts!");
                    }

                    if (features_to_use.Any(a => a == "MinFinalScore") == false)
                        features_to_use.Add("MinFinalScore");
                }

            }
            else if (m_AB01.CSM_Features.Any(a => a == "MinFinalScore") == true)
            {
                m_AB01.CSM_Features.Remove("MinFinalScore");
                features_to_use.Remove("MinFinalScore");
            }

            Console.WriteLine("INFO: Computing FDR on CSM level...");

            internal_method0(
                m_AA00,
                allCSMs.Cast<IFDRElement>().ToList(),
                features_to_use,
                m_AB01.CSM_FDR
            );

            allCSMs = PostScoresHelper.ApplyThresholdFilters(
                allCSMs,
                m_AB00,
                m_AB01);

            var r = new CSMPackage(
                allCSMs.OrderByDescending(a => a.ClassificationScore).ToList(),
                m_AB01.FDRMode,
                m_AB01.CSM_FDR);

            return r;
        }
    }
}