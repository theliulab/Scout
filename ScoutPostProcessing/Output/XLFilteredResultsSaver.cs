using Digestor;
using Ionic.Zip;
using Newtonsoft.Json.Linq;
using PatternTools.FastaTools;
using PatternTools.MSParserLight;
using ScoutCore;
using ScoutCore.Scoring;
using ScoutCore.SpectraOperations;
using ScoutPostProcessing.CSMLogic;
using ScoutPostProcessing.PPILogic;
using ScoutPostProcessing.ResiduePairLogic;
using SixLabors.ImageSharp.Formats;
using SpectrumWizard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThermoFisher.CommonCore.Data.Business;

namespace ScoutPostProcessing.Output
{
    public static class XLFilteredResultsSaver
    {

        public static bool SaveScoutDatabase(string savePath,
            ScoutParameters searchParams)
        {
            throw new NotImplementedException();
        }

        public static bool SaveScoutParameters(string savePath,
            ScoutParameters searchParams,
            PostProcessingParameters postProcessingParameters)
        {

            try
            {

                string fileName = Path.GetFileName(savePath);
                string[] splitFileName = Regex.Split(fileName, "\\.");

                string searchFile = Path.GetDirectoryName(savePath) + Path.DirectorySeparatorChar + splitFileName[0] + "_search.json";
                string json = searchParams.ToSaveJson();
                File.WriteAllText(searchFile, json);

                string postProcessingFile = Path.GetDirectoryName(savePath) + Path.DirectorySeparatorChar + splitFileName[0] + "_post_processing.json";
                json = postProcessingParameters.ToSaveJson();
                File.WriteAllText(postProcessingFile, json);
                Console.WriteLine($"Parameters exported to {Path.GetDirectoryName(savePath)}!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        public static bool SaveCSMsToQuant(
           string savePath,
           List<PPI> pack,
           ScoutParameters searchParams)
        {

            StringBuilder sb = new();
            List<(bool isInterProtein, ScoredCSM csm)> toSave = (from ppi in pack.AsParallel()
                                                                 from rp in ppi.ResPairs.AsParallel()
                                                                 from csm in rp.CSMs.AsParallel()
                                                                 select (ppi.IsInterLink, csm)).ToList();


            List<string> baseColumns = new()
            {
                "AlphaPeptide",
                "BetaPeptide",
                "AlphaPos",
                "BetaPos",
                "AlphaPtnPos",
                "BetaPtnPos",
                "ExperimentalMZ",
                "Charge",
                "TheoreticalMZ",
                "Isotopes",
                "ScanNumber",
                "RetentionTime",
                "ClassificationScore",
                "AlphaGeneMappings",
                "BetaGeneMappings",
                "AlphaProteinMappings",
                "BetaProteinMappings",
                "CrossLinker",
                "QuantitationTag",
                "ProteinLinkType",
                "PeptideLinkType",
                "FileName"
            };

            sb.AppendLine(string.Join(',', baseColumns));
            toSave.ForEach(a =>
                       sb.AppendLine(ToQuantCSVLine(baseColumns, a.csm, a.isInterProtein, ',', searchParams)));

            try
            {
                File.WriteAllText(savePath, sb.ToString());
                Console.WriteLine($"Results exported to {savePath}!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        private static string ToQuantCSVLine(
            List<string> header,
            ScoredCSM xl,
            bool isInterProtein,
            char separator,
            ScoutParameters searchParams)
        {
            string proteinLinkType = isInterProtein ? "Inter-link Protein" : "Intra-link Protein";
            string peptideLinkType = xl.IsInterLink ? "Inter-link Peptide" : "Intra-link Peptide";
            var sb = new StringBuilder();

            foreach (var headerItem in header)
            {
                switch (headerItem)
                {
                    case "AlphaPeptide":
                        sb.Append(xl.AlphaPSM.Peptide.AsSilacString);
                        break;
                    case "BetaPeptide":
                        sb.Append(xl.BetaPSM.Peptide.AsSilacString);
                        break;
                    case "AlphaPos":
                        sb.Append((xl.AlphaPSM.ReagentPosition1 + 1).ToString());
                        break;
                    case "BetaPos":
                        sb.Append((xl.BetaPSM.ReagentPosition1 + 1).ToString());
                        break;
                    case "AlphaPtnPos":
                        sb.Append(xl.AlphaMappings.Select(a => a.ProteinPosition + xl.AlphaPSM.ReagentPosition1 + 1).Distinct().FirstOrDefault());
                        break;
                    case "BetaPtnPos":
                        sb.Append(xl.BetaMappings.Select(a => a.ProteinPosition + xl.BetaPSM.ReagentPosition1 + 1).Distinct().FirstOrDefault());
                        break;
                    case "AlphaGeneMappings":
                        sb.Append(xl.AlphaMappings.Select(a => a.Gene).FirstOrDefault());
                        break;
                    case "BetaGeneMappings":
                        sb.Append(xl.BetaMappings.Select(a => a.Gene).FirstOrDefault());
                        break;
                    case "AlphaProteinMappings":
                        sb.Append(xl.AlphaMappings.Select(a => a.Locus).FirstOrDefault());
                        break;
                    case "BetaProteinMappings":
                        sb.Append(xl.BetaMappings.Select(a => a.Locus).FirstOrDefault());
                        break;
                    case "ExperimentalMZ":
                        sb.Append(xl.PrecursorMZ);
                        break;
                    case "Charge":
                        sb.Append(xl.PrecursorCharge);
                        break;
                    case "TheoreticalMZ":
                        sb.Append(xl.TheoreticalMZ);
                        break;
                    case "Isotopes":
                        sb.Append(xl.TotalPairIsotopes + Math.Abs(xl.AlphaPSM.IsotopeNumber) + Math.Abs(xl.BetaPSM.IsotopeNumber));
                        break;
                    case "ScanNumber":
                        sb.Append(xl.ScanNumber);
                        break;
                    case "RetentionTime":
                        sb.Append(xl.RetentionTime);
                        break;
                    case "ClassificationScore":
                        sb.Append(xl.ClassificationScore.ToString("N4"));
                        break;
                    case "CrossLinker":
                        sb.Append(searchParams.CXLReagent.Name);
                        break;
                    case "QuantitationTag":
                        sb.Append(xl.QuantitationTag);
                        break;
                    case "FileName":
                        sb.Append(xl.FileName);
                        break;
                    case "ProteinLinkType":
                        sb.Append(proteinLinkType);
                        break;
                    case "PeptideLinkType":
                        sb.Append(peptideLinkType);
                        break;
                    default:
                        break;

                }
                sb.Append(separator);
            }

            return sb.ToString().Trim(separator);
        }

        public static bool SaveCSMsPythonCSV(
           string savePath,
           CSMPackage pack,
           ScoutParameters parameters,
           bool filteredOnly = false,
           bool addParamsString = true)
        {
            StringBuilder sb = new();

            List<ScoredCSM> toSave = new();
            if (filteredOnly)
                toSave = pack.FilteredCSMs;
            else
                toSave = pack.AllCSMs;

            if (addParamsString == true)
            {
                string paramsString = $"#Params JSON: {parameters.ToSaveJson(false)}";
                sb.AppendLine(paramsString);
            }

            List<string> baseColumns = new()
            {
                "Type",
                "Class",
                "ClassNum",
                "ScanNumber",
                "Charge",
                "ExperimentalMZ",
                "Rank",
                "AlphaPeptide",
                "BetaPeptide",
                "AlphaPos",
                "BetaPos",
                "AlphaMappings",
                "BetaMappings",
                "Alpha protein(s) position(s)",
                "Beta protein(s) position(s)",
                "AlphaPairMH",
                "BetaPairMH",
                "AlphaTheoreticalMH",
                "BetaTheoreticalMH",
                "AlphaPeaksMatched",
                "BetaPeaksMatched",
                "ClassificationScore"
            };

            if (toSave.Count == 0)
                return true;

            baseColumns.AddRange(toSave.First().Scores.Keys);

            baseColumns.Add("FileName");

            sb.AppendLine(string.Join(',', baseColumns));

            if (filteredOnly == true)
            {
                toSave.ForEach(a =>
                       sb.AppendLine(ToCleaveCSVLine(baseColumns, a, ',')));
            }
            else
            {
                toSave.ForEach(a =>
                       sb.AppendLine(ToCleaveCSVLine(baseColumns, a, ',')));
            }

            try
            {
                File.WriteAllText(savePath, sb.ToString());
                Console.WriteLine($"Results saved to {savePath}!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        private static string ToCleaveCSVLine(
            List<string> header,
            ScoredCSM xl,
            char separator)
        {
            var sb = new StringBuilder();

            foreach (var headerItem in header)
            {
                switch (headerItem)
                {
                    case "Type":
                        sb.Append("Cleavable");
                        break;
                    case "ClassNum":
                        sb.Append(xl.ClassNum);
                        break;
                    case "Class":
                        sb.Append(xl._Class);
                        break;
                    case "ScanNumber":
                        sb.Append(xl.ScanNumber);
                        break;
                    case "Charge":
                        sb.Append(xl.PrecursorCharge);
                        break;
                    case "ExperimentalMZ":
                        sb.Append(xl.PrecursorMZ);
                        break;
                    case "Rank":
                        sb.Append(xl.Rank);
                        break;
                    case "AlphaPeptide":
                        sb.Append(xl.AlphaPSM.Peptide.AsString);
                        break;
                    case "BetaPeptide":
                        sb.Append(xl.BetaPSM.Peptide.AsString);
                        break;
                    case "AlphaPos":
                        sb.Append(xl.AlphaPSM.ReagentPosition1.ToString());
                        break;
                    case "BetaPos":
                        sb.Append(xl.BetaPSM.ReagentPosition1.ToString());
                        break;
                    case "AlphaMappings":
                        sb.Append(string.Join(";", xl.AlphaMappings.Select(a => a.Locus).ToList()));
                        break;
                    case "BetaMappings":
                        sb.Append(string.Join(";", xl.BetaMappings.Select(a => a.Locus).ToList()));
                        break;
                    case "Alpha protein(s) position(s)":
                        line.Add(string.Join("; ", GetProtPositions(csm.AlphaMappings, csm.AlphaPSM.ReagentPosition1)));
                        break;
                    case "Beta protein(s) position(s)":
                        if (csm.BetaPSM != null)
                            line.Add(string.Join("; ", GetProtPositions(csm.BetaMappings, csm.BetaPSM.ReagentPosition1)));
                        else
                            line.Add(string.Join("; ", GetProtPositions(csm.AlphaMappings, csm.AlphaPSM.ReagentPosition2)));
                        break;
                    case "AlphaTheoreticalMH":
                        sb.Append(xl.AlphaPSM.Peptide.MH);
                        break;
                    case "BetaTheoreticalMH":
                        sb.Append(xl.BetaPSM.Peptide.MH);
                        break;
                    case "AlphaPeaksMatched":
                        sb.Append(xl.AlphaPSM.PeaksMatched);
                        break;
                    case "BetaPeaksMatched":
                        sb.Append(xl.BetaPSM.PeaksMatched);
                        break;
                    case "AlphaPairMH":
                        sb.Append(xl.AlphaPairMH);
                        break;
                    case "BetaPairMH":
                        sb.Append(xl.BetaPairMH);
                        break;
                    case "FileName":
                        sb.Append(xl.FileName);
                        break;
                    case "ClassificationScore":
                        sb.Append(xl.ClassificationScore);
                        break;



                    default:
                        try
                        {
                            sb.Append(xl.Scores[headerItem]);
                        }
                        catch
                        {
                            throw new Exception($"Header \"{headerItem}\" unknown.");
                        }
                        break;

                }
                sb.Append(separator);
            }

            return sb.ToString().Trim(separator);
        }

        public static void SaveCSMsCSV(
            string fileName,
            List<ScoredCSM> csms,
            ScoutParameters searchParams,
            PostProcessingParameters postProcessingParameters)
        {
            List<string> GetModificationsString(
                Peptide peptide,
                Dictionary<int, AminoacidMod> modsDict)
            {
                if (peptide.Modifications == null || peptide.Modifications.Count == 0)
                    return new();

                List<string> mods = new();

                foreach (var (position, modIndex) in peptide.Modifications)
                {
                    var mod = modsDict[modIndex];

                    if (mod.IsNTerm && position == 0)
                        mods.Add("N-Term(" + mod.Name + ")");
                    else if (mod.IsCTerm && position == peptide.Length - 1)
                        mods.Add("C-Term(" + mod.Name + ")");
                    else
                        mods.Add($"{peptide.ResidueTuples[position].aminoacid}{position + 1} ({mod.Name})");
                }

                mods = (from mod in mods
                        where !string.IsNullOrEmpty(mod)
                        select mod.Replace(',', ';')).Distinct().ToList();
                return mods;
            }

            void BuildModificationsDictionaries(
                ScoutParameters searchParams,
                out Dictionary<int, AminoacidMod> modsDict)
            {
                modsDict = searchParams.VariableModifications.Concat(searchParams.StaticModifications).ToDictionary(a => a.ModIndex, b => b);
            }

            BuildModificationsDictionaries(searchParams, out var varModsDict);

            List<string> columns = new List<string>()
            {
                "Scan",
                "Experimental m/z",
                "Theoretical m/z",
                "Link-Type",
                "Alpha peptide",
                "Alpha m/z",
                "Alpha ppm error",
                "Beta peptide",
                "Beta m/z",
                "Beta ppm error",
                "Precursor charge",
                "Alpha peptide position",
                "Beta peptide position",
                "Alpha protein(s) position(s)",
                "Beta protein(s) position(s)",
                "Alpha modification(s)",
                "Beta modification(s)",
                "Alpha protein mapping(s)",
                "Beta protein mapping(s)",
                "Alpha gene mapping(s)",
                "Beta gene mapping(s)",
                "IsDecoy"
            };
            if (searchParams.SilacSearch)
                columns.Add("QuantitationTag");
            columns.Add("Score");
            columns.Add("ReactionSites (%)");
            columns.Add("File");

            List<int> GetProtPositions(List<PeptideMapping> peptideMappings, int reagent_position)
            {
                List<int> positions = new List<int>();

                foreach (var bestMapping in peptideMappings)
                {
                    positions.Add(bestMapping.ProteinPosition + reagent_position + 1);
                }

                return positions;
            }

            var lines = new List<string>(csms.Count)
            {
                string.Join(",", columns)
            };

            foreach (var csm in csms)
            {
                var line = new List<string>(columns.Count);
                bool end_last_columns = false;

                foreach (var column in columns)
                {
                    switch (column)
                    {
                        case "Scan":
                            line.Add(csm.ScanNumber.ToString());
                            break;
                        case "Experimental m/z":
                            line.Add(csm.PrecursorMZ.ToString());
                            break;
                        case "Link-Type":
                            if (csm.BetaPSM != null)
                                line.Add(GetLinkType(csm.IsInterLink, String.Join(", ", csm.AlphaMappings.Select(a => "sp|" + a.Locus + "|" + a.Gene).Distinct().ToList()), String.Join(", ", csm.BetaMappings.Select(a => "sp|" + a.Locus + "|" + a.Gene).Distinct().ToList()), String.Join(", ", GetProtPositions(csm.AlphaMappings, csm.AlphaPSM.ReagentPosition1)), String.Join(", ", GetProtPositions(csm.BetaMappings, csm.BetaPSM.ReagentPosition1))));
                            else
                                line.Add("Looplink");
                            break;
                        case "Alpha peptide":
                            line.Add(csm.AlphaPSM.Peptide.AsCleanString);
                            break;
                        case "Alpha m/z":
                            line.Add(ChargeOperations.ToMZ(csm.AlphaPSM.Peptide.MH, csm.PrecursorCharge).ToString());
                            break;
                        case "Alpha ppm error":
                            line.Add(Math.Abs(csm.AlphaPPM).ToString());
                            break;
                        case "Beta peptide":
                            if (csm.BetaPSM != null)
                                line.Add(csm.BetaPSM.Peptide.AsCleanString);
                            else
                                line.Add("NA");
                            break;
                        case "Beta m/z":
                            if (csm.BetaPSM != null)
                                line.Add(ChargeOperations.ToMZ(csm.BetaPSM.Peptide.MH, csm.PrecursorCharge).ToString());
                            else
                                line.Add("NA");
                            break;
                        case "Beta ppm error":
                            if (csm.BetaPSM != null)
                                line.Add(Math.Abs(csm.BetaPPM).ToString());
                            else
                                line.Add("NA");
                            break;
                        case "Theoretical m/z":
                            line.Add(csm.TheoreticalMZ.ToString());
                            break;
                        case "Precursor charge":
                            line.Add(csm.PrecursorCharge.ToString());
                            break;
                        case "Alpha peptide position":
                            line.Add((csm.AlphaPSM.ReagentPosition1 + 1).ToString());
                            break;
                        case "Beta peptide position":
                            if (csm.BetaPSM != null)
                                line.Add((csm.BetaPSM.ReagentPosition1 + 1).ToString());
                            else
                                line.Add("NA");
                            break;
                        case "Alpha protein(s) position(s)":
                            line.Add(string.Join("; ", GetProtPositions(csm.AlphaMappings, csm.AlphaPSM.ReagentPosition1)));
                            break;
                        case "Beta protein(s) position(s)":
                            if (csm.BetaPSM != null)
                                line.Add(string.Join("; ", GetProtPositions(csm.BetaMappings, csm.BetaPSM.ReagentPosition1)));
                            else
                                line.Add(string.Join("; ", GetProtPositions(csm.AlphaMappings, csm.AlphaPSM.ReagentPosition2)));
                            break;
                        case "Alpha modification(s)":
                            line.Add(string.Join("; ", GetModificationsString(csm.AlphaPSM.Peptide, varModsDict)));
                            break;
                        case "Beta modification(s)":
                            if (csm.BetaPSM != null)
                                line.Add(string.Join("; ", GetModificationsString(csm.BetaPSM.Peptide, varModsDict)));
                            else
                                line.Add("NA");
                            break;
                        case "Alpha protein mapping(s)":
                            line.Add(string.Join("; ", csm.AlphaMappings.Select(a => "sp|" + a.Locus + "|" + a.Gene)));
                            break;
                        case "Beta protein mapping(s)":
                            if (csm.BetaPSM != null)
                                line.Add(string.Join("; ", csm.BetaMappings.Select(a => "sp|" + a.Locus + "|" + a.Gene)));
                            else
                                line.Add("NA");
                            break;
                        case "Alpha gene mapping(s)":
                            line.Add(string.Join("; ", csm.AlphaMappings.Select(a => a.Gene)));
                            break;
                        case "Beta gene mapping(s)":
                            if (csm.BetaPSM != null)
                                line.Add(string.Join("; ", csm.BetaMappings.Select(a => a.Gene)));
                            else
                                line.Add("NA");
                            break;
                        case "QuantitationTag":
                            line.Add(csm.QuantitationTag);
                            end_last_columns = true;
                            break;
                        case "IsDecoy":
                            line.Add(csm.IsDecoy.ToString().ToUpper());
                            if (searchParams.SilacSearch)
                                end_last_columns = false;
                            else
                                end_last_columns = true;
                            break;

                    }

                    if (end_last_columns)
                    {
                        line.Add(Path.GetFileName(csm.FileName));
                        break;
                    }
                }

                lines.Add(string.Join(",", line));
            }

            try
            {
                File.Delete(fileName);
            }
            catch { }

            File.WriteAllLines(fileName, lines);
        }

        private static string GetLinkType(
                bool isInterLink,
                string alphaPtns,
                string betaPtns,
                string alphaProtPosition,
                string betaProtPosition)
        {
            if (isInterLink)
                return "Heteromeric-inter";
            else
            {
                if (alphaPtns.Equals(betaPtns) && alphaProtPosition.Equals(betaProtPosition))
                    return "Homomeric-inter";
                else
                    return "Intralink";
            }
        }

        public static void SaveResiduePairsCSV(
            string fileName,
            List<ResPair> respairs,
            PostProcessingParameters postProcessingParameters)
        {
            List<string> columns = new List<string>()
            {
                "Residue pair",
                "Link-Type",
                "Alpha peptide",
                "Beta peptide",
                "Alpha peptide position",
                "Beta peptide position",
                "Alpha protein(s) position(s)",
                "Beta protein(s) position(s)",
                "Alpha protein mapping(s)",
                "Beta protein mapping(s)",
                "Alpha gene mapping(s)",
                "Beta gene mapping(s)",
                "IsDecoy",
                "CSM count"
            };

            columns.Add("Score");

            var lines = new List<string>(respairs.Count)
            {
                string.Join(",", columns)
            };

            foreach (var rp in respairs)
            {
                var line = new List<string>(columns.Count);
                bool end_last_columns = false;

                foreach (var column in columns)
                {
                    switch (column)
                    {
                        case "Residue pair":
                            line.Add(rp.ResiduePairString.Replace(" \u2194 ", " <=> "));
                            break;
                        case "Link-Type":
                            line.Add(GetLinkType(rp.IsInterLink, rp.AlphaProteins, rp.BetaProteins, rp.AlphaProtPosition, rp.BetaProtPosition));
                            break;
                        case "Alpha peptide":
                            line.Add(rp.AlphaPeptideString);
                            break;
                        case "Beta peptide":
                            line.Add(rp.BetaPeptideString);
                            break;
                        case "Alpha peptide position":
                            line.Add(rp.AlphaPeptPosition.ToString());
                            break;
                        case "Beta peptide position":
                            line.Add(rp.BetaPeptPosition.ToString());
                            break;
                        case "Alpha protein(s) position(s)":
                            line.Add(rp.AlphaProtPosition.Replace(',', ';'));
                            break;
                        case "Beta protein(s) position(s)":
                            line.Add(rp.BetaProtPosition.Replace(',', ';'));
                            break;
                        case "Alpha protein mapping(s)":
                            line.Add(rp.AlphaProteins.Replace(',', ';'));
                            break;
                        case "Beta protein mapping(s)":
                            line.Add(rp.BetaProteins.Replace(',', ';'));
                            break;
                        case "Alpha gene mapping(s)":
                            line.Add(rp.AlphaGenes.Replace(',', ';'));
                            break;
                        case "Beta gene mapping(s)":
                            line.Add(rp.BetaGenes.Replace(',', ';'));
                            break;
                        case "IsDecoy":
                            line.Add(rp.IsDecoy.ToString().ToUpper());

                            break;
                        case "CSM count":
                            line.Add(rp.CSMs.Count().ToString());
                            end_last_columns = true;
                            break;
                    }

                    if (end_last_columns)
                    {
                        line.Add(rp.ClassificationScore.ToString());
                        break;
                    }
                }

                lines.Add(string.Join(",", line));
            }

            try
            {
                File.Delete(fileName);
            }
            catch { }

            File.WriteAllLines(fileName, lines);
        }

        public static void SavePPIsCSV(
            string fileName,
            List<PPI> ppis,
            PostProcessingParameters postProcessingParameters)
        {
            List<string> columns = new List<string>()
            {
                "PPI",
                "Alpha protein(s)",
                "Beta protein(s)",
                "IsDecoy",
                "Link-Type",
                "Residue pair count",
                "CSM count",
                "CSM scans",
            };
            columns.Add("Score");

            var lines = new List<string>(ppis.Count)
            {
                string.Join(",", columns)
            };

            foreach (var ppi in ppis)
            {
                var line = new List<string>(columns.Count);
                bool end_last_columns = false;

                foreach (var column in columns)
                {
                    switch (column)
                    {
                        case "PPI":
                            line.Add(ppi.PPIString.Replace(" \u2194 ", " <=> "));
                            break;
                        case "Alpha protein(s)":
                            line.Add(ppi.ProteinOneString.Replace(',', ';'));
                            break;
                        case "Beta protein(s)":
                            line.Add(ppi.ProteinTwoString.Replace(',', ';'));
                            break;
                        case "IsDecoy":
                            line.Add(ppi.IsDecoy.ToString().ToUpper());
                            break;
                        case "Link-Type":
                            if (ppi.IsInterLink)
                                line.Add("Inter");
                            else
                                line.Add("Intra");
                            break;
                        case "Residue pair count":
                            line.Add(ppi.ResPairCount.ToString());
                            break;
                        case "CSM count":
                            line.Add(ppi.CSMCount.ToString());
                            break;
                        case "CSM scans":
                            line.Add(string.Join(";", ppi.GetAllCSMs().Select(a => a.ID.Split('_')[1]).ToList()));
                            end_last_columns = true;
                            break;
                    }

                    if (end_last_columns)
                    {
                        line.Add(ppi.ClassificationScore.ToString());
                        break;
                    }
                }

                lines.Add(string.Join(",", line));
            }

            try
            {
                File.Delete(fileName);
            }
            catch { }

            File.WriteAllLines(fileName, lines);
        }


        private static PatternTools.FastaTools.FastaFileParser LoadFASTA(
                string fastaPath,
                ScoutParameters parameters
                )
        {
            var digestionParams = parameters.AssembleDigestionParemeters();
            var fp = Digestor.Digestor.AssembleFasta(fastaPath, digestionParams);

            return fp;
        }
        public static bool SavePPItoXlinkCyNET(
            string savePath,
            List<PPI> ppis,
            Dictionary<string, int> proteinScores,
            ScoutParameters parameters,
            PostProcessingParameters postProcessingParameters,
            string fastaPath)
        {
            PatternTools.FastaTools.FastaFileParser fp = null;
            const string POISSON_SCORE = "PoissonScore";
            try
            {
                fp = LoadFASTA(fastaPath, parameters);
            }
            catch (Exception)
            {

                throw new Exception("Fasta not found.");
            }

            StringBuilder sb = new();

            List<string> baseColumns = new()
            {
                "gene_a",
                "gene_b",
                "ppi_score_min",
                "ppi_score_integrated",
                "length_protein_a",
                "length_protein_b",
                "protein_a",
                "protein_b",
                "residue_pairs",
                "crosslinks_ab",
                "crosslinks_ba",
                "score_ab",
                "score_ba",
                "has_common_peptides",
                "unique_residue_pairs"
            };
            sb.AppendLine(string.Join(',', baseColumns));

            Dictionary<string, (string gene_a, string gene_b, string ppi_score_max, string ppi_score_integrated, string length_protein_a, string length_protein_b, string protein_a, string protein_b, string residue_pairs, string crosslinks_ab, string crosslinks_ba, string score_ab, string score_ba, string has_common_peptides, string unique_reaction_sites, string unique_crosslinks)> all_ppis = new();
            foreach (var _ppi in ppis)
            {
                #region check if ppi.ID contains the correct order of protein1 and protein2
                string ppi_ID = _ppi.ID;
                string[] cols_ppi = Regex.Split(_ppi.ID, "\\+");

                string[] cols_string_ptn1 = Regex.Split(_ppi.ProteinOneString, ",");
                var ptns1 = Regex.Split(cols_ppi[0], ";");

                string[] cols_string_ptn2 = Regex.Split(_ppi.ProteinTwoString, ",");
                var ptns2 = Regex.Split(cols_ppi[1], ";");

                #region check ptn1
                bool isValid1 = true;
                bool isInversed1 = false;
                int index_ptn = Math.Min(cols_string_ptn1.Length, ptns1.Length);
                //check
                for (int count_ppi = 0; count_ppi < index_ptn; count_ppi++)
                {
                    string ptn = ptns1[count_ppi];
                    string ptnA = Utils.getAccessionNumber(cols_string_ptn1[count_ppi]).Trim();
                    if (!ptn.Equals(ptnA))
                    {
                        isValid1 = false;
                        break;
                    }
                }
                if (isValid1 == false)
                {
                    isValid1 = true;
                    index_ptn = Math.Min(cols_string_ptn2.Length, ptns1.Length);
                    //check
                    for (int count_ppi = 0; count_ppi < index_ptn; count_ppi++)
                    {
                        string ptn = ptns1[count_ppi];
                        string ptnB = Utils.getAccessionNumber(cols_string_ptn2[count_ppi]).Trim();
                        if (!ptn.Equals(ptnB))
                        {
                            isValid1 = false;
                            break;
                        }
                        isInversed1 = true;
                    }
                }

                //check length; maybe AAA+AAA;BBB;CCC
                if (isValid1 == true && isInversed1 == false)
                {
                    if (ptns1.Length != cols_string_ptn1.Length)
                    {
                        isInversed1 = true;
                        if (ptns1.Length == cols_string_ptn2.Length)
                        {
                            for (int count_ppi = 0; count_ppi < ptns1.Length; count_ppi++)
                            {
                                string ptn = ptns1[count_ppi];
                                string ptnA = Utils.getAccessionNumber(cols_string_ptn2[count_ppi]).Trim();
                                if (!ptn.Equals(ptnA))
                                {
                                    isValid1 = false;
                                    break;
                                }
                            }
                        }
                        else isValid1 = false;
                    }
                }

                #endregion

                #region check ptn2
                bool isValid2 = true;
                bool isInversed2 = false;
                index_ptn = Math.Min(cols_string_ptn2.Length, ptns2.Length);
                //check
                for (int count_ppi = 0; count_ppi < index_ptn; count_ppi++)
                {
                    string ptn = ptns2[count_ppi];
                    string ptnA = Utils.getAccessionNumber(cols_string_ptn2[count_ppi]).Trim();
                    if (!ptn.Equals(ptnA))
                    {
                        isValid2 = false;
                        break;
                    }
                }
                if (isValid2 == false)
                {
                    isValid2 = true;
                    index_ptn = Math.Min(cols_string_ptn1.Length, ptns2.Length);
                    //check
                    for (int count_ppi = 0; count_ppi < index_ptn; count_ppi++)
                    {
                        string ptn = ptns2[count_ppi];
                        string ptnB = Utils.getAccessionNumber(cols_string_ptn1[count_ppi]).Trim();
                        if (!ptn.Equals(ptnB))
                        {
                            isValid2 = false;
                            break;
                        }
                        isInversed2 = true;
                    }
                }

                //check length; maybe AAA+AAA;BBB;CCC
                if (isValid2 == true && isInversed2 == false)
                {
                    if (ptns2.Length != cols_string_ptn2.Length)
                    {
                        isInversed2 = true;
                        if (ptns2.Length == cols_string_ptn1.Length)
                        {
                            for (int count_ppi = 0; count_ppi < ptns2.Length; count_ppi++)
                            {
                                string ptn = ptns2[count_ppi];
                                string ptnB = Utils.getAccessionNumber(cols_string_ptn1[count_ppi]).Trim();
                                if (!ptn.Equals(ptnB))
                                {
                                    isValid2 = false;
                                    break;
                                }
                            }
                        }
                        else isValid2 = false;
                    }
                }

                #endregion

                PPI ppi = new PPI(_ppi.PPI_ID, _ppi.ResPairs, _ppi.AlphaProteins, _ppi.BetaProteins, _ppi._Class, _ppi.IsDecoy, _ppi.ClassificationScore, _ppi.Scores, _ppi.IsUnique, _ppi.IsInterLink);
                if (isValid1 && isValid2)
                {
                    if (isInversed1 && isInversed2)
                    {
                        List<ScoredCSM> _new_csms = new();

                        List<ScoredCSM> current_csms = ppi.GetAllCSMs();
                        foreach (var csm in current_csms)
                        {
                            ScoredCSM _new_csm = null;
                            string[] _rpID_cols = Regex.Split(csm.ResPair_ID, "\\+");

                            isValid1 = true;
                            if (ptns2.Length == csm.AlphaPSM.Peptide.Mappings.Count)
                            {
                                foreach (var map in csm.AlphaPSM.Peptide.Mappings)
                                {
                                    if (!ptns2.Contains(map.Locus))
                                    {
                                        isValid1 = false;
                                        break;
                                    }
                                }

                                if (isValid1 == false &&
                                    ptns2.Length == csm.BetaPSM.Peptide.Mappings.Count)
                                {
                                    isValid1 = true;
                                    foreach (var map in csm.BetaPSM.Peptide.Mappings)
                                    {
                                        if (!ptns2.Contains(map.Locus))
                                        {
                                            isValid1 = false;
                                            break;
                                        }
                                    }
                                }

                                if (isValid1 == true)
                                {
                                    string _new_rp_ID = _rpID_cols[1] + "+" + _rpID_cols[0];
                                    _new_csm = new ScoredCSM(csm.CSM_ID, _new_rp_ID, _ppi.PPI_ID, csm.FileName, csm.FileIndex, csm.ScanNumber, csm.PrecursorMZ, csm.PrecursorCharge, csm.RetentionTime, csm.BetaPairMH, csm.AlphaPairMH, csm.BetaPairIntensity, csm.AlphaPairIntensity, csm.Rank, csm.BetaPSM, csm.AlphaPSM, csm.BetaMappings, csm.AlphaMappings, csm.Scores, csm._Class, csm.ClassificationScore, csm.IsInterLink, csm.IsLoopLink, csm.Spectrum, csm.TheoreticalMZ, csm.PrecursorPPM, csm.TotalPairIsotopes, csm.ReactionSitesProbabilities, csm.ReactionSitesProbabilitiesLoopLink);
                                }
                            }
                            else if (ptns1.Length == csm.AlphaPSM.Peptide.Mappings.Count)
                            {
                                foreach (var map in csm.AlphaPSM.Peptide.Mappings)
                                {
                                    if (!ptns1.Contains(map.Locus))
                                    {
                                        isValid1 = false;
                                        break;
                                    }
                                }

                                if (isValid1 == false &&
                                    ptns1.Length == csm.BetaPSM.Peptide.Mappings.Count)
                                {
                                    isValid1 = true;
                                    foreach (var map in csm.BetaPSM.Peptide.Mappings)
                                    {
                                        if (!ptns1.Contains(map.Locus))
                                        {
                                            isValid1 = false;
                                            break;
                                        }
                                    }
                                }

                                if (isValid1 == true)
                                {
                                    string _new_rp_ID = _rpID_cols[0] + "+" + _rpID_cols[1];
                                    _new_csm = new ScoredCSM(csm.CSM_ID, _new_rp_ID, _ppi.PPI_ID, csm.FileName, csm.FileIndex, csm.ScanNumber, csm.PrecursorMZ, csm.PrecursorCharge, csm.RetentionTime, csm.AlphaPairMH, csm.BetaPairMH, csm.AlphaPairIntensity, csm.BetaPairIntensity, csm.Rank, csm.AlphaPSM, csm.BetaPSM, csm.AlphaMappings, csm.BetaMappings, csm.Scores, csm._Class, csm.ClassificationScore, csm.IsInterLink, csm.IsLoopLink, csm.Spectrum, csm.TheoreticalMZ, csm.PrecursorPPM, csm.TotalPairIsotopes, csm.ReactionSitesProbabilities, csm.ReactionSitesProbabilitiesLoopLink);
                                }
                            }

                            csm.ID = _new_csm.ID;
                            csm.ResPair_ID = _new_csm.ResPair_ID;
                            csm.PPI_ID = _new_csm.PPI_ID;
                            csm.AlphaPairMH = _new_csm.AlphaPairMH;
                            csm.BetaPairMH = _new_csm.BetaPairMH;
                            csm.AlphaPairIntensity = _new_csm.AlphaPairIntensity;
                            csm.BetaPairIntensity = _new_csm.BetaPairIntensity;
                            csm.AlphaPSM = _new_csm.AlphaPSM;
                            csm.BetaPSM = _new_csm.BetaPSM;
                            csm.AlphaMappings = _new_csm.AlphaMappings;
                            csm.BetaMappings = _new_csm.BetaMappings;
                        }
                    }
                }

                #endregion

                string[] cols_ptn1_ptn2 = Regex.Split(ppi.ID, "\\+");
                List<string> ptn1 = new List<string>() { cols_ptn1_ptn2[0] };
                if (ptn1[0].Contains(";"))//There is more than one protein
                    ptn1 = new List<string>(Regex.Split(ptn1[0], ";"));

                List<string> ptn2 = new List<string>() { cols_ptn1_ptn2[1] };
                if (ptn2[0].Contains(";"))//There is more than one protein
                    ptn2 = new List<string>(Regex.Split(ptn2[0], ";"));

                List<string> combination_ppis = ptn1.SelectMany(item1 => ptn2.Select(item2 => $"{item1}+{item2}")).ToList();

                bool hasCommonResPairs = combination_ppis.Count > 1;
                for (int count = 0; count < combination_ppis.Count; count++)
                {
                    string _key = combination_ppis[count];
                    if (!all_ppis.ContainsKey(_key))
                        internal_method(_key, hasCommonResPairs, proteinScores, fp, ppi, all_ppis);
                    else
                    {
                        (string gene_a, string gene_b, string ppi_score_max, string ppi_score_integrated, string length_protein_a, string length_protein_b, string protein_a, string protein_b, string residue_pairs, string crosslinks_ab, string crosslinks_ba, string score_ab, string score_ba, string has_common_peptides, string unique_reaction_sites, string unique_crosslinks) current_values;
                        all_ppis.TryGetValue(_key, out current_values);
                        current_values.has_common_peptides = "true";

                        #region retrieve current_values from ppi object
                        (string gene_a, string gene_b, string ppi_score_max, string ppi_score_integrated, string length_protein_a, string length_protein_b, string protein_a, string protein_b, string csms, string crosslinks_ab, string crosslinks_ba, string score_ab, string score_ba, string has_common_peptides, string unique_reaction_sites, string unique_crosslinks) new_values = new();

                        var fullPtnA = Regex.Split(ppi.ProteinOneString, ",");
                        var fullPtnB = Regex.Split(ppi.ProteinTwoString, ",");

                        string current_p1 = fullPtnA.Where(a => a.Contains(_key.Split("+")[0])).FirstOrDefault();
                        if (current_p1 != null) current_p1 = current_p1.Trim();
                        string current_p2 = fullPtnB.Where(a => a.Contains(_key.Split("+")[1])).FirstOrDefault();
                        if (current_p2 != null) current_p2 = current_p2.Trim();

                        string ptnA = Utils.getAccessionNumber(current_p1).Trim();
                        string ptnB = Utils.getAccessionNumber(current_p2).Trim();

                        string[] cols_geneA = ppi.GeneOneString.Split(',');
                        string[] cols_geneB = ppi.GeneTwoString.Split(',');
                        string geneA = "";
                        string geneB = "";
                        if (cols_geneA.Length == 1)
                            geneA = ppi.GeneOneString.Split(',')[0].Trim();

                        if (cols_geneB.Length == 1)
                            geneB = ppi.GeneTwoString.Split(',')[0].Trim();

                        if (cols_geneA.Length == 1 &&
                            cols_geneB.Length == 1)
                        {
                            string ppi_alpha = current_p1;
                            string ppi_beta = current_p2;
                            new_values = add_single_ppi_info(proteinScores, fp, ppi, true, POISSON_SCORE, new_values, ptnA, ptnB, geneA + "_" + ptnA, geneB + "_" + ptnB);
                            all_ppis[_key] = update_ppi_xlinkcynet(current_values, new_values);
                        }
                        else
                        {
                            string[] cols_ptnA = ppi.ProteinOneString.Replace(',', ';').Split(';');
                            string[] cols_ptnB = ppi.ProteinTwoString.Replace(',', ';').Split(';');
                            List<string> ptn_combination = (from pA in cols_ptnA
                                                            from pB in cols_ptnB
                                                            select pA.Trim() + "###" + pB.Trim()).ToList();

                            List<string> gene_combination = (from pA in cols_ptnA
                                                             from pB in cols_ptnB
                                                             select Utils.getGene(pA.Trim()) + "###" + Utils.getGene(pB.Trim())).ToList();

                            cols_ptnA = (from pA in cols_ptnA
                                         select Utils.getAccessionNumber(pA)).ToArray();
                            cols_ptnB = (from pB in cols_ptnB
                                         select Utils.getAccessionNumber(pB)).ToArray();

                            List<string> ptn_access_number_combination = (from pA in cols_ptnA
                                                                          from pB in cols_ptnB
                                                                          select pA + "###" + pB).ToList();

                            string _current_key = Regex.Replace(_key, "\\+", "###");
                            int key_index = ptn_access_number_combination.FindIndex(a => a.Equals(_current_key));
                            if (key_index == -1) continue;

                            string gene_comb = gene_combination[key_index];
                            string ptn_access_number_comb = ptn_access_number_combination[key_index];
                            string ptnA_acc = Regex.Split(ptn_access_number_comb, "###")[0];
                            string ptnB_acc = Regex.Split(ptn_access_number_comb, "###")[1];
                            new_values.gene_a = Regex.Split(gene_comb, "###")[0] + "_" + ptnA_acc;
                            new_values.gene_b = Regex.Split(gene_comb, "###")[1] + "_" + ptnB_acc;

                            string ptn_comb = ptn_combination[key_index];
                            new_values.protein_a = Regex.Split(ptn_comb, "###")[0];
                            new_values.protein_b = Regex.Split(ptn_comb, "###")[1];

                            string _new_ppi_key = ptnA_acc + "+" + ptnB_acc;
                            new_values = add_single_ppi_info(proteinScores, fp, ppi, true, POISSON_SCORE, new_values, ptnA_acc, ptnB_acc, new_values.gene_a, new_values.gene_b);
                            if (all_ppis.ContainsKey(_new_ppi_key))
                            {
                                //merge
                                all_ppis.TryGetValue(_new_ppi_key, out current_values);
                                all_ppis[_new_ppi_key] = update_ppi_xlinkcynet(current_values, new_values);
                            }

                        }
                        #endregion
                    }
                }
            }

            foreach (var item in all_ppis)
                sb.AppendLine(item.Value.Item1 + "," + item.Value.Item2 + "," + item.Value.Item3 + "," + item.Value.Item4 + "," + item.Value.Item5 + "," + item.Value.Item6 + "," + item.Value.Item7 + "," + item.Value.Item8 + "," + item.Value.Item9 + "," + item.Value.Item10 + "," + item.Value.Item11 + "," + item.Value.Item12 + "," + item.Value.Item13 + "," + item.Value.Item14 + "," + item.Value.Item15);

            try
            {
                File.WriteAllText(savePath, sb.ToString());
                Console.WriteLine($"Results saved to {savePath}!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        private static void internal_method(
            string _key,
            bool hasCommonResPair,
            Dictionary<string, int> proteinScores,
            FastaFileParser fp,
            PPI ppi,
            Dictionary<string, (string gene_a, string gene_b, string ppi_score_max, string ppi_score_integrated, string length_protein_a, string length_protein_b, string protein_a, string protein_b, string residue_pairs, string crosslinks_ab, string crosslinks_ba, string score_ab, string score_ba, string has_common_peptides, string unique_reaction_sites, string unique_crosslinks)> all_ppis)
        {
            const string POISSON_SCORE = "PoissonScore";
            (string gene_a, string gene_b, string ppi_score_max, string ppi_score_integrated, string length_protein_a, string length_protein_b, string protein_a, string protein_b, string residue_pairs, string crosslinks_ab, string crosslinks_ba, string score_ab, string score_ba, string has_common_peptides, string unique_reaction_sites, string unique_crosslinks) values = new();

            var fullPtnA = Regex.Split(ppi.ProteinOneString, ",");
            var fullPtnB = Regex.Split(ppi.ProteinTwoString, ",");

            string current_p1 = fullPtnA.Where(a => a.Contains(_key.Split("+")[0])).FirstOrDefault();
            if (current_p1 != null) current_p1 = current_p1.Trim();
            string current_p2 = fullPtnB.Where(a => a.Contains(_key.Split("+")[1])).FirstOrDefault();
            if (current_p2 != null) current_p2 = current_p2.Trim();

            string ptnA = Utils.getAccessionNumber(current_p1).Trim();
            string ptnB = Utils.getAccessionNumber(current_p2).Trim();

            string[] cols_geneA = ppi.GeneOneString.Split(',');
            string[] cols_geneB = ppi.GeneTwoString.Split(',');
            string geneA = "";
            string geneB = "";
            if (cols_geneA.Length == 1)
                geneA = ppi.GeneOneString.Split(',')[0].Trim();

            if (cols_geneB.Length == 1)
                geneB = ppi.GeneTwoString.Split(',')[0].Trim();

            values.gene_a = geneA + "_" + ptnA;
            values.gene_b = geneB + "_" + ptnB;
            geneA = values.gene_a;
            geneB = values.gene_b;

            if (cols_geneA.Length == 1 &&
                cols_geneB.Length == 1)
            {
                if (hasCommonResPair == true)
                    values.has_common_peptides = "true";
                else
                    values.has_common_peptides = "false";
                string ppi_alpha = current_p1;
                string ppi_beta = current_p2;

                values.protein_a = (ppi_alpha.StartsWith(">") ? ppi_alpha.Substring(1, ppi_alpha.Length - 1) : ppi_alpha);
                values.protein_b = (ppi_beta.StartsWith(">") ? ppi_beta.Substring(1, ppi_beta.Length - 1) : ppi_beta);
                all_ppis.Add(_key, add_single_ppi_info(proteinScores, fp, ppi, hasCommonResPair, POISSON_SCORE, values, ptnA, ptnB, geneA, geneB));
            }
            else
            {
                string[] cols_ptnA = ppi.ProteinOneString.Replace(',', ';').Split(';');
                string[] cols_ptnB = ppi.ProteinTwoString.Replace(',', ';').Split(';');
                List<string> ptn_combination = (from pA in cols_ptnA
                                                from pB in cols_ptnB
                                                select pA.Trim() + "###" + pB.Trim()).ToList();

                List<string> gene_combination = (from pA in cols_ptnA
                                                 from pB in cols_ptnB
                                                 select Utils.getGene(pA.Trim()) + "###" + Utils.getGene(pB.Trim())).ToList();

                cols_ptnA = (from pA in cols_ptnA
                             select Utils.getAccessionNumber(pA)).ToArray();
                cols_ptnB = (from pB in cols_ptnB
                             select Utils.getAccessionNumber(pB)).ToArray();

                List<string> ptn_access_number_combination = (from pA in cols_ptnA
                                                              from pB in cols_ptnB
                                                              select pA + "###" + pB).ToList();

                string _current_key = Regex.Replace(_key, "\\+", "###");
                int key_index = ptn_access_number_combination.FindIndex(a => a.Equals(_current_key));
                if (key_index == -1) return;

                string gene_comb = gene_combination[key_index];
                (string gene_a, string gene_b, string ppi_score_max, string ppi_score_integrated, string length_protein_a, string length_protein_b, string protein_a, string protein_b, string residue_pairs, string crosslinks_ab, string crosslinks_ba, string score_ab, string score_ba, string has_common_peptides, string unique_reaction_sites, string unique_crosslinkss) new_values = new();

                string ptn_access_number_comb = ptn_access_number_combination[key_index];
                string ptnA_acc = Regex.Split(ptn_access_number_comb, "###")[0];
                string ptnB_acc = Regex.Split(ptn_access_number_comb, "###")[1];
                new_values.gene_a = Regex.Split(gene_comb, "###")[0] + "_" + ptnA_acc;
                new_values.gene_b = Regex.Split(gene_comb, "###")[1] + "_" + ptnB_acc;

                string ptn_comb = ptn_combination[key_index];
                new_values.protein_a = Regex.Split(ptn_comb, "###")[0];
                new_values.protein_b = Regex.Split(ptn_comb, "###")[1];

                string _new_ppi_key = ptnA_acc + "+" + ptnB_acc;
                new_values = add_single_ppi_info(proteinScores, fp, ppi, true, POISSON_SCORE, new_values, ptnA_acc, ptnB_acc, new_values.gene_a, new_values.gene_b);
                if (!all_ppis.ContainsKey(_new_ppi_key))
                {
                    new_values.has_common_peptides = "true";
                    new_values.unique_reaction_sites = "0";
                    new_values.unique_crosslinkss = "";
                    all_ppis.Add(_new_ppi_key, new_values);
                }
                else
                {
                    //merge
                    (string gene_a, string gene_b, string ppi_score_max, string ppi_score_integrated, string length_protein_a, string length_protein_b, string protein_a, string protein_b, string residue_pairs, string crosslinks_ab, string crosslinks_ba, string score_ab, string score_ba, string has_common_peptides, string unique_reaction_sites, string unique_crosslinks) current_values;
                    all_ppis.TryGetValue(_new_ppi_key, out current_values);
                    all_ppis[_new_ppi_key] = update_ppi_xlinkcynet(current_values, new_values);
                }
            }
        }

        internal static double GetComputedScores(List<double?> scores)
        {
            // total_score = Sqrt ((score1 ^ 2 + score2 ^ 2 + score3 ^ 2) / 3)
            double _total_score = Math.Sqrt(((from score in scores.AsParallel()
                                              select Math.Pow((double)score, 2)).Sum() / scores.Count));

            return _total_score;
        }

        internal static (string gene_a, string gene_b, string ppi_score_max, string ppi_score_integrated, string length_protein_a, string length_protein_b, string protein_a, string protein_b, string csms, string crosslinks_ab, string crosslinks_ba, string score_ab, string score_ba, string has_common_peptides, string unique_reaction_sites, string unique_crosslinks) add_single_ppi_info(
            Dictionary<string, int> proteinScores,
            FastaFileParser fp,
            PPI ppi,
            bool hasCommonResPair,
            string POISSON_SCORE,
            (string gene_a, string gene_b, string ppi_score_max, string ppi_score_integrated, string length_protein_a, string length_protein_b, string protein_a, string protein_b, string csms, string crosslinks_ab, string crosslinks_ba, string score_ab, string score_ba, string has_common_peptides, string unique_reaction_sites, string unique_crosslinks) values,
            string ptnA,
            string ptnB,
            string geneA,
            string geneB)
        {
            double poisson_score_max = Convert.ToDouble(ppi.ResPairs.First().CSMs.Max(csm => csm.Scores[POISSON_SCORE]));
            double? ppi_score_max = Math.Exp((-1) * poisson_score_max);
            values.ppi_score_max = Convert.ToString(ppi_score_max);

            double poisson_score_integrated = GetComputedScores(ppi.ResPairs.Select(a => a.Scores["BestPoisson"]).ToList());
            double? ppi_score_integrated = Math.Exp((-1) * poisson_score_integrated);
            values.ppi_score_integrated = Convert.ToString(ppi_score_integrated);

            int lengthPtnA = 0;
            int lengthPtnB = 0;
            var fastaA = fp.MyItems.Where(a => a.SequenceIdentifier.Contains(ptnA)).FirstOrDefault();
            if (fastaA != null)
                lengthPtnA = fastaA.Sequence.Length;

            var fastaB = fp.MyItems.Where(a => a.SequenceIdentifier.Contains(ptnB)).FirstOrDefault();
            if (fastaB != null)
                lengthPtnB = fastaB.Sequence.Length;

            values.length_protein_a = Convert.ToString(lengthPtnA);
            values.length_protein_b = Convert.ToString(lengthPtnB);

            List<ScoredCSM> allCSMs = ppi.GetAllCSMs();
            List<string> csmsList = new();
            StringBuilder csmsScoreSB = new();
            foreach (var csm in allCSMs)
            {
                PeptideMapping bestMappingAlpha = csm.AlphaMappings.Where(a => a.Locus.Equals(ptnA)).MaxBy(a => proteinScores[a.Locus]);
                if (bestMappingAlpha == null)
                    bestMappingAlpha = csm.AlphaMappings.Where(a => a.Locus.Equals(ptnB)).MaxBy(a => proteinScores[a.Locus]);

                PeptideMapping bestMappingBeta = csm.BetaMappings.Where(a => a.Locus.Equals(ptnB)).MaxBy(b => proteinScores[b.Locus]);
                if (bestMappingBeta == null)
                    bestMappingBeta = csm.BetaMappings.Where(a => a.Locus.Equals(ptnA)).MaxBy(b => proteinScores[b.Locus]);

                int alphaAminoacidPosition = -1;
                int betaAminoacidPosition = -1;

                string composed_gene_protein = bestMappingAlpha.Gene + "_" + ptnA;
                //Check order of alpha and beta genes
                if (composed_gene_protein.Equals(geneA))
                {
                    alphaAminoacidPosition = bestMappingAlpha.ProteinPosition + csm.AlphaPSM.ReagentPosition1 + 1;
                    betaAminoacidPosition = bestMappingBeta.ProteinPosition + csm.BetaPSM.ReagentPosition1 + 1;
                }
                else
                {
                    betaAminoacidPosition = bestMappingAlpha.ProteinPosition + csm.AlphaPSM.ReagentPosition1 + 1;
                    alphaAminoacidPosition = bestMappingBeta.ProteinPosition + csm.BetaPSM.ReagentPosition1 + 1;
                }

                string _csm = geneA.Trim() + "-" + alphaAminoacidPosition + "-" + geneB.Trim() + "-" + betaAminoacidPosition;
                if (!csmsList.Contains(_csm))
                {
                    csmsList.Add(_csm);

                    poisson_score_max = Convert.ToDouble(csm.Scores[POISSON_SCORE]);
                    double? csm_score = Math.Exp((-1) * poisson_score_max);
                    csmsScoreSB.Append(csm_score + "#");
                }
            }
            string csmsSB = string.Join("#", csmsList);
            if (csmsSB.ToString().EndsWith("#"))
                csmsSB.Remove(csmsSB.ToString().Length - 1, 1);
            values.crosslinks_ab = csmsSB;
            values.crosslinks_ba = csmsSB;
            values.csms = Convert.ToString(csmsList.Count);

            if (hasCommonResPair == true)
            {
                values.unique_reaction_sites = "0";
                values.unique_crosslinks = "";
            }
            else
            {
                values.unique_reaction_sites = Convert.ToString(csmsList.Count);
                values.unique_crosslinks = csmsSB;
            }

            if (csmsScoreSB.ToString().EndsWith("#"))
                csmsScoreSB.Remove(csmsScoreSB.ToString().Length - 1, 1);
            values.score_ab = csmsScoreSB.ToString();
            values.score_ba = csmsScoreSB.ToString();
            return values;
        }

        internal static (string gene_a, string gene_b, string ppi_score_max, string ppi_score_integrated, string length_protein_a, string length_protein_b, string protein_a, string protein_b, string residue_pairs, string crosslinks_ab, string crosslinks_ba, string score_ab, string score_ba, string has_common_peptides, string unique_reaction_sites, string unique_crosslinks) update_ppi_xlinkcynet(
            (string gene_a, string gene_b, string ppi_score_max, string ppi_score_integrated, string length_protein_a, string length_protein_b, string protein_a, string protein_b, string residue_pairs, string crosslinks_ab, string crosslinks_ba, string score_ab, string score_ba, string has_common_peptides, string unique_reaction_sites, string unique_crosslinks) current_values,
            (string gene_a, string gene_b, string ppi_score_max, string ppi_score_integrated, string length_protein_a, string length_protein_b, string protein_a, string protein_b, string residue_pairs, string crosslinks_ab, string crosslinks_ba, string score_ab, string score_ba, string has_common_peptides, string unique_reaction_sites, string unique_crosslinks) new_values)
        {

            List<string> current_unique_xls = Regex.Split(current_values.unique_crosslinks, "#").ToList();
            current_unique_xls.RemoveAll(a => String.IsNullOrEmpty(a));

            List<string> current_xls = Regex.Split(current_values.crosslinks_ab, "#").ToList();
            current_xls.RemoveAll(a => String.IsNullOrEmpty(a));

            List<string> current_scores = Regex.Split(current_values.score_ab, "#").ToList();
            current_scores.RemoveAll(a => String.IsNullOrEmpty(a));

            List<string> new_xls = Regex.Split(new_values.crosslinks_ab, "#").ToList();
            new_xls.RemoveAll(a => String.IsNullOrEmpty(a));
            
            List<string> new_scores = Regex.Split(new_values.score_ab, "#").ToList();
            new_scores.RemoveAll(a => String.IsNullOrEmpty(a));

            List<string> common_elements = new();
            for (int i = 0; i < current_unique_xls.Count; i++)
            {
                if (new_xls.Contains(current_unique_xls[i]))
                    common_elements.Add(current_unique_xls[i]);
            }

            current_unique_xls = current_unique_xls.Except(common_elements).ToList();

            for (int i = 0; i < new_xls.Count; i++)
            {
                if (!current_xls.Contains(new_xls[i]))
                {
                    current_xls.Add(new_xls[i]);
                    current_scores.Add(new_scores[i]);
                }
            }

            current_values.crosslinks_ab = String.Join("#", current_xls);
            current_values.crosslinks_ba = String.Join("#", current_xls);
            current_values.score_ab = String.Join("#", current_scores);
            current_values.score_ba = String.Join("#", current_scores);
            current_values.residue_pairs = Convert.ToString(current_scores.Count);
            current_values.has_common_peptides = "true";
            current_values.unique_crosslinks = String.Join("#", current_unique_xls);
            current_values.unique_reaction_sites = Convert.ToString(current_unique_xls.Count);
            return current_values;
        }

        public static bool SaveResultsToBinaryFile(string savePath, XLFilteredResults results)
        {
            try
            {
                Console.WriteLine("INFO: Creating ZIP file...");
                ZipFile zipFile = ScoutCore.FileManagement.Serializer.CreateZipFile();
                Console.WriteLine("INFO: Done...");

                int fileIndex = 1;
                SplitResultsToSendToProtBuf(zipFile, results, ref fileIndex);
                ScoutCore.FileManagement.Serializer.SaveZipFile(zipFile, savePath, fileIndex);
                return true;

            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Could not save results!\n" + e.Message);
                return false;
            }

        }

        private static void SplitResultsToSendToProtBuf(ZipFile zipFile, XLFilteredResults results, ref int fileIndex)
        {
            Console.WriteLine("INFO: Saving parameters...");
            SaveParams(zipFile, results);
            Console.WriteLine("INFO: Done...");

            int old_progress = 0;
            int total_file = results.PackageCSMs.AllCSMs.Count;

            Console.Write("Saving file: 0%");
            int count = 0;
            try
            {
                //small pieces. These pieces are saved in the same zip file, but in different FileCompressed subfiles.
                for (count = 0; count < total_file; count += ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED, fileIndex++)
                {
                    CSMPackage partial_csmPackage = new CSMPackage(
                        results.PackageCSMs.AllCSMs.Skip(count).Take(ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED).ToList(),
                        results.PostParams.FDRMode,
                        results.PostParams.CSM_FDR);
                    CSMPackage partial_csm_loopPackage = new CSMPackage(
                        results.PackageCSMsLoopLinks.AllCSMs.Skip(count).Take(ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED).ToList(),
                        results.PostParams.FDRMode,
                        results.PostParams.CSM_FDR);
                    ResiduePairPackage partial_ResPairPackage = new ResiduePairPackage(
                        results.PackageResPairs.AllResPairs.Skip(count).Take(ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED).ToList(),
                        results.PostParams.FDRMode,
                        results.PostParams.ResPair_FDR);
                    PPIPackage partial_PPIPackage = new PPIPackage(
                        results.PackagePPIs.AllPPIs.Skip(count).Take(ScoutCore.FileManagement.Serializer.MAX_ITEMS_TO_BE_SAVED).ToList(),
                        results.PostParams.FDRMode,
                        results.PostParams.PPI_FDR);
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
                    int new_progress = (int)((double)count / total_file * 100);
                    if (new_progress > old_progress)
                    {
                        old_progress = new_progress;
                        Console.Write("Saving file: " + old_progress + "%");
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

        private static void SaveParams(ZipFile zipFile, XLFilteredResults results)
        {
            results.ParamsSearchSaveString = ScoutCore.FileManagement.Serializer.ToJSON(results.SearchParams, false, false);
            results.ParamsPostSaveString = ScoutCore.FileManagement.Serializer.ToJSON(results.PostParams, false, false);

            ScoutCore.FileManagement.Serializer.AddParamsZip(results.ParamsSearchSaveString, zipFile, 1);
            ScoutCore.FileManagement.Serializer.AddParamsZip(results.ParamsPostSaveString, zipFile, 2);
            ScoutCore.FileManagement.Serializer.AddParamsZip(results.FastaPath, zipFile, 3);
            ScoutCore.FileManagement.Serializer.AddParamsZip(results.BufFiles, zipFile, 4);
            ScoutCore.FileManagement.Serializer.AddParamsZip(results.TotalProcessingTime, zipFile, 5);
            ScoutCore.FileManagement.Serializer.AddParamsZip(results.RawsFolder, zipFile, 6);
            ScoutCore.FileManagement.Serializer.AddParamsZip(results.ProteinScores, zipFile, 7);
            ScoutCore.FileManagement.Serializer.AddParamsZip(results.FileVersion, zipFile, 8);
        }

        public static bool SaveResultsToMzIdentlML_1_2(string savePath, XLFilteredResults results, out string error)
        {
            try
            {
                error = "";
                ParserMzIdentML_1_2 mzIdentML = new();
                if (mzIdentML.SaveMzIdML(savePath, results))
                {
                    Console.WriteLine("INFO: mzIdentML file has been created successfully!");
                    return true;
                }
                else
                {
                    error = mzIdentML.ErrorMsg;
                    return false;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Could not save results!\n" + e.Message);
                error = "Could not save results!\n" + e.Message;
                return false;
            }
        }

        public static bool SaveResultsToMzIdentlML_1_3(string savePath, XLFilteredResults results, out string error)
        {
            try
            {
                error = "";
                ParserMzIdentML_1_3 mzIdentML = new();
                if (mzIdentML.SaveMzIdML(savePath, results))
                {
                    Console.WriteLine("INFO: mzIdentML file has been created successfully!");
                    return true;
                }
                else
                {
                    error = mzIdentML.ErrorMsg;
                    return false;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Could not save results!\n" + e.Message);
                error = "ERROR: Could not save results!\n" + e.Message;
                return false;
            }
        }

        public static bool SaveSpectraToMS2(List<MSUltraLight> tmsList, string savePath)
        {
            if (tmsList == null || tmsList.Count == 0) return false;

            StreamWriter sw = new StreamWriter(savePath);
            return ToMS2(tmsList, sw);

        }

        private static bool ToMS2(List<MSUltraLight> tmsList,
                                         StreamWriter sw,
                                         int msnLevel = 2)
        {
            if (tmsList == null || tmsList.Count == 0) return false;

            int spectra_processed = 0;
            int old_progress = 0;
            double lengthFile = tmsList.Count;

            Console.WriteLine("INFO: Writing MS" + msnLevel + " File.");

            int firstScan = tmsList[0].ScanNumber;
            int lastScan = tmsList[tmsList.Count() - 1].ScanNumber;
            string version = ScoutCore.Utils.GetAppVersion();

            try
            {
                //StringBuilder sb_output = new();
                sw.Write("H\tCreation Date\t" + DateTime.Now.ToString() + "\n"
                + "H\tExtractor\tScout\n"
                + "H\tFirstScan\t" + firstScan + "\n"
                + "H\tLastScan\t" + lastScan + "\n"
                + "H\tVersion\t" + version + "\n"
                + "H\tComments\tThis converter was written by Milan Avila Clasen and Diogo Borges Lima, 2023.\n"
                );

            
                foreach (MSUltraLight tms in tmsList)
                {
                    if (msnLevel > 1)
                    {
                        sw.Write("S\t" + String.Format("{0:000000}", tms.ScanNumber) + "\t" + String.Format("{0:000000}", tms.ScanNumber) + "\t" + tms.Precursors[0].MZ + "\n" +
                            "I\tRetTime\t" + tms.CromatographyRetentionTime + "\n" +
                            "I\tActivationType\t" + tms.ActivationType + "\n" +
                            "I\tInstrumentType\t" + tms.InstrumentType + "\n" +
                            "I\tFilter\t" + tms.ScanHeader + "\n" +
                            "I\tPrecursorScan\t" + tms.PrecursorScanNumber + "\n"
                            );
                        foreach ((double MZ, short Z) precursor in tms.Precursors)
                        {
                            sw.WriteLine("Z\t" + precursor.Z + "\t" + ChargeOperations.ToMH(precursor.MZ, precursor.Z));
                        }
                    }
                    else
                    {
                        sw.Write("S\t" + String.Format("{0:000000}", tms.ScanNumber) + "\t" + String.Format("{0:000000}", tms.ScanNumber) + "\n" +
                            "I\tRetTime\t" + tms.CromatographyRetentionTime + "\n" +
                            "I\tActivationType\t" + tms.ActivationType + "\n" +
                            "I\tInstrumentType\t" + tms.InstrumentType + "\n" +
                            "I\tFilter\t" + tms.ScanHeader + "\n"
                            );
                    }

                    foreach ((double MZ, double Intensity) ion in tms.Ions)
                    {
                        sw.WriteLine(ion.MZ + "\t" + ion.Intensity);
                    }

                    spectra_processed++;
                    int new_progress = (int)((double)spectra_processed / (lengthFile) * 100);
                    if (new_progress > old_progress)
                    {
                        old_progress = new_progress;
                        Console.Write("INFO: Writing MS" + msnLevel + " File: " + old_progress + "%");
                    }

                }
                //sw.Write(sb_output.ToString());
                sw.Close();
            }
            catch (Exception e)
            {
                Console.Write("ERROR: Could not save MS2 file!\n" + e.Message);
            }
            return true;
        }
    }
}
