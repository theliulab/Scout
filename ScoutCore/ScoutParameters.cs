using Digestor;
using ScoutCore.SpectraOperations;
using Newtonsoft.Json;
using SpectrumWizard;
using SpectrumWizard.Predictors.CleavableXL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Math.Geometry;
using System.Data;
using Digestor.Modification;

namespace ScoutCore
{
    [Serializable]
    public class ScoutParameters
    {

        public bool BDP_Mode { get; set; } = false;

        #region general
        //[ProtoMember(1)]
        public string MSFileExtension { get; set; } = ".raw";


        public double PPMMS1Tolerance { get; set; } = 10;
        public double PPMMS2Tolerance { get; set; } = 20;

        public bool PerformShotgunSearch { get; set; } = false;

        public bool PerformCleaveXLSearch { get; set; } = true;

        public bool SaveSpectraToResults { get; set; } = false;

        public int MaxQueryResults { get; set; } = 4;

        public int MinPepLength { get; set; } = 6;
        public int MaxPepLength { get; set; } = 60;

        public double MinPepMass { get; set; } = 500;
        public double MaxPepMass { get; set; } = 6000;
        public string FastaFile { get; set; }
        public string RawPath { get; set; }

        /// <summary>
        /// Max charge tested when MS has no precursor information
        /// </summary>
        //public short MaxPrecursorCharge { get; set; } = 4;
        /// <summary>
        /// Used for low res data.
        /// </summary>
        public bool AddMinusOneIsotope { get; set; } = false;

        public float CarbonIsotopeShift { get; set; } = 1.00335483778f;
        public int IsotopicPossibilitiesPrecursor { get; set; } = 1;

        public int MiscleavageNum { get; set; } = 3;

        public Enzyme Enzyme { get; set; } = Enzyme.Trypsin;

        public Enzyme.EnzymeSpecificity EnzymeSpecificity { get; set; } = Digestor.Enzyme.EnzymeSpecificity.FullySpecific;

        public int MaximumVariableModsPerPeptide { get; set; } = 2;

        public List<SpectrumWizard.AminoacidMod> StaticModifications { get; set; } = defaultStaticMods;
        public List<SpectrumWizard.AminoacidMod> VariableModifications { get; set; } = defaultVariableMods;
        public List<Aminoacid> ExtraAminoacids { get; set; }
        public bool SearchLoopLinks { get; set; } = true;

        #endregion

        #region CleavableXL

        public int IonPairMaxCharge { get; set; } = 2;

        public CleaveReagent CXLReagent { get; set; } = CleaveReagent.DSSO_KSYT;

        public bool FullPairsOnly { get; set; } = false;

        public float PairFinderPPM { get; set; } = 10;


        #endregion

        #region Scoring

        public double MinBinMZ { get; set; } = 200;
        public double MaxBinMZ { get; set; } = 1800;
        public double BinSize { get; set; } = 0.02;
        public double Offset { get; set; } = 0;

        public ScoreFunctionTypes ScoreFunction { get; set; } = ScoreFunctionTypes.SpectralAngle;

        public NormalizationTypes MS2NormalizationTypes { get; set; } =
            NormalizationTypes.SetTopToOneIntensity;

        public double NormalizationWindowWidth { get; set; } = 80;
        public int NormalizationWindowPeaksKept { get; set; } = 13;

        public bool AddPrecursorToDotProduct { get; set; } = false;
        public bool AddIonPairsToDotProduct { get; set; } = false;
        public bool ApplyMZRangeWeighting { get; set; } = false;

        public bool ReorderCleaveCandidateScores { get; set; } = true;
        public bool RemoveIonPairsFromExperimentalMS { get; set; } = true;
        public bool RemovePrecursorFromExperimentalMS { get; set; } = true;
        public bool ReplaceNullPoissonToXLScore { get; set; } = true;

        public enum ReorderMethods
        {
            None,
            ByPair,
            PSMCombinatorial
        };

        public ReorderMethods ReorderingMethod { get; set; } = ReorderMethods.None;
        public int ReorderingCombinatorialDepthHeavy { get; set; } = 3;
        public int ReorderingCombinatorialDepthLight { get; set; } = 2;

        #endregion

        #region Advanced

        public int FastaBatchSize { get; set; } = 30000;

        public bool ParallelPSMs { get; set; } = true;

        public int? QueryBatches { get; set; } = null;
        public bool MergeDatabase { get; set; } = false;
        #endregion

        #region Digestion
        public bool MethionineInitiator { get; set; } = true;

        public bool AddDecoys { get; set; } = true;
        public bool AddContaminants { get; set; } = true;
        public bool DontShowContaminants { get; set; } = true;
        public string DecoyTag { get; set; } = "Reverse";
        public DecoyGeneration.DecoyGenerationMode DecoyGenerationMode { get; set; } = DecoyGeneration.DecoyGenerationMode.ReverseProteins;

        public bool AddUnlabelledDecoys { get; set; } = false;
        public string UnlabelledDecoyTag { get; set; } = "Unlabelled";
        public bool OffsetUnlabelledDecoy { get; set; } = true;
        public int OffsetUnlabelled { get; set; } = 2;

        public DecoyGeneration.DecoyGenerationMode UnlabelledDecoysGenerationMode { get; set; } = DecoyGeneration.DecoyGenerationMode.ReversePeptides;

        #endregion

        #region Debugging

        public bool AddLocusStringToPeptides { get; set; } = true;
        #endregion

        #region Deconvolution
        public bool DeconvolutionForPairSearching { get; set; } = true;

        public bool DeconvolutionForMSScoring { get; set; } = false;
        #endregion

        #region Silac

        public bool SilacSearch { get; set; } = false;
        public bool SilacHybridMode { get; set; } = false;

        public List<SilacGroup> SilacGroups { get; set; } = new()
        {
            new SilacGroup("Light", new SilacAminoacid('K', 0), new SilacAminoacid('R', 0)),
            //new SilacGroup("Heavy", new SilacAminoacid('K', 4.025107), new SilacAminoacid('R', 6.020129)),
            //new SilacGroup("Heavy", new SilacAminoacid('K', 6.020129), new SilacAminoacid('R', 0)),
            //new SilacGroup("Heavy", new SilacAminoacid('K', 8.0141988131999), new SilacAminoacid('R', 0)),
            //new SilacGroup("Heavy", new SilacAminoacid('K', 6.020129), new SilacAminoacid('R', 10.0082685996)),
            new SilacGroup("Heavy", new SilacAminoacid('K', 8.0141988131999), new SilacAminoacid('R', 10.0082685996)),
        };

        public bool SilacSearchNormalPeptides { get; set; } = false;
        #endregion

        #region Isobaric Labelling
        public bool IsobaricLabelling_Search { get; set; } = false;
        public List<AminoacidMod> IsobaricLabelling_Mods { get; set; } = defaultIsobaricReagents;
        public AminoacidMod SelectedIsobaricLabelling_Mod { get; set; } = AminoacidMod.TMT_16plex;
        public int IsobaricLabelling_AllowedFreeResidues { get; set; } = 2;

        private static List<AminoacidMod> defaultIsobaricReagents = new List<AminoacidMod>()
        {
            AminoacidMod.iTRAQ_4,
            AminoacidMod.iTRAQ_8,
            AminoacidMod.TMT_6plex,
            AminoacidMod.TMT_10plex,
            AminoacidMod.TMT_11plex,
            AminoacidMod.TMT_16plex,
            AminoacidMod.TMT_18plex
        };
        #endregion
        public bool CalculatePairIntensities { get; set; } = true;

        public bool AddXLasVariableMod { get; set; } = false;
        public int IsotopesInTheoreticalMS { get; internal set; } = 2;
        public int MaxChargeInTheoreticalMS { get; internal set; } = 2;
        public bool BonusMode { get; set; } = true;
        public double BonusScore { get; set; } = 0.001;
        public bool ApplyNeutralLossH2O { get; set; } = false;
        public bool ApplyNeutralLossNH3 { get; set; } = false;
        public float StarIntensity { get; set; } = 0.7f;
        public bool FastaDistinctByLocus { get; set; } = true;

        public bool ApplyQuantileIntensityThreshold { get; set; } = false;
        public double QuantileIntensityThreshold { get; set; } = 0.3;

        public ScoutParameters() { }
        public DigestionParameters AssembleDigestionParemeters()
        {
            var dbParams = new DigestionParameters()
            {
                EnzymeSpecificity = EnzymeSpecificity,
                Enzyme = Enzyme,
                FastaBatchSize = FastaBatchSize,
                MaximumVariableModsPerPeptide = MaximumVariableModsPerPeptide,
                MethionineInitiator = MethionineInitiator,
                MinPepLength = MinPepLength,
                MaxPepLength = MaxPepLength,
                MiscleavageNum = MiscleavageNum,
                VariableModifications = VariableModifications,
                StaticModifications = StaticModifications,
                AddDecoys = AddDecoys,
                AddContaminants = AddContaminants,
                DecoyTag = DecoyTag,
                DecoyGenerationMode = DecoyGenerationMode,
                AddUnlabelledDecoys = AddUnlabelledDecoys,
                UnlabelledDecoysTag = UnlabelledDecoyTag,
                UnlabelledDecoyGenerationMode = UnlabelledDecoysGenerationMode,
                OffsetUnlabelledDecoys = OffsetUnlabelledDecoy,
                UnlabelledOffset = OffsetUnlabelled,
                AddLocusStringToPeptide = AddLocusStringToPeptides,
                FastaDistinctByLocus = FastaDistinctByLocus,
                SilacSearch = SilacSearch,
                SilacGroups = SilacGroups,
                SilacHybridMode = SilacHybridMode,
                TMT_Search = IsobaricLabelling_Search,
                TMT_Mod = SelectedIsobaricLabelling_Mod,
                TMT_AllowedFreeResidues = IsobaricLabelling_AllowedFreeResidues,
                ExtraAminoacids = ExtraAminoacids
            };

            return dbParams;
        }

        private DataTable CreateParamsDatatable()
        {
            DataTable dtParams = new DataTable();

            dtParams.Columns.Add("Property");
            dtParams.Columns.Add("Value");

            var row = dtParams.NewRow();
            row["Property"] = "Database file";
            row["Value"] = System.IO.Path.GetFileName(FastaFile);
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Fasta batch size";
            row["Value"] = FastaBatchSize;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Raw directory";
            row["Value"] = Path.GetDirectoryName(RawPath);
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Spectra saved in the results";
            row["Value"] = SaveSpectraToResults;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Add contaminants";
            row["Value"] = AddContaminants;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Add decoys";
            row["Value"] = AddDecoys;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Fragment bin tolerance";
            row["Value"] = BinSize;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Fragment bin offset";
            row["Value"] = Offset;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Max fragment bin m/z";
            row["Value"] = MaxBinMZ;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Min fragment bin m/z";
            row["Value"] = MinBinMZ;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Deconvolution for MS Searching";
            row["Value"] = DeconvolutionForMSScoring;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Deconvolution for Ion Pair Searching";
            row["Value"] = DeconvolutionForPairSearching;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Cross-linker";
            row["Value"] = CXLReagent.Name;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "  Mass";
            row["Value"] = CXLReagent.WholeMass;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "  Target N-term";
            row["Value"] = CXLReagent.TargetNTerm;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "  Light fragment";
            row["Value"] = CXLReagent.LightFragment;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "  Heavy fragment";
            row["Value"] = CXLReagent.HeavyFragment;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "  Reaction residue(s)";
            row["Value"] = CXLReagent.Targets;
            dtParams.Rows.Add(row);


            row = dtParams.NewRow();
            row["Property"] = "Enzyme";
            row["Value"] = Enzyme;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "  Enzyme specificity";
            row["Value"] = EnzymeSpecificity;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Isotopic possibilities precursor";
            row["Value"] = IsotopicPossibilitiesPrecursor;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Min peptide length";
            row["Value"] = MinPepLength;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Max peptide length";
            row["Value"] = MaxPepLength;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Max variable modifications per peptide";
            row["Value"] = MaximumVariableModsPerPeptide;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Min peptide mass";
            row["Value"] = MinPepMass;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Max peptide mass";
            row["Value"] = MaxPepMass;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Max miscleavages";
            row["Value"] = MiscleavageNum;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Ppm error on MS1 level";
            row["Value"] = PPMMS1Tolerance;
            dtParams.Rows.Add(row);

            row = dtParams.NewRow();
            row["Property"] = "Ppm error on MS2 level";
            row["Value"] = PPMMS2Tolerance;
            dtParams.Rows.Add(row);

            if (SilacSearch)
            {
                row = dtParams.NewRow();
                row["Property"] = "Silac Search";
                row["Value"] = SilacSearch;
                dtParams.Rows.Add(row);

                row = dtParams.NewRow();
                row["Property"] = "Silac Hybrid mode";
                row["Value"] = SilacHybridMode;
                dtParams.Rows.Add(row);

                for (int i = 0; i < SilacGroups.Count; i++)
                {
                    var silacGroup = SilacGroups[i];
                    row = dtParams.NewRow();
                    row["Property"] = "  " + (i + 1) + ")  Name";
                    row["Value"] = silacGroup.GroupName;
                    dtParams.Rows.Add(row);

                    for (int j = 0; j < silacGroup.GroupAminoacids.Count; j++)
                    {
                        var silacAA = silacGroup.GroupAminoacids[j];
                        row = dtParams.NewRow();
                        row["Property"] = "    " + (i + 1) + "." + (j + 1) + ")  Amino acid";
                        row["Value"] = silacAA.TargetResidue;
                        dtParams.Rows.Add(row);

                        row = dtParams.NewRow();
                        row["Property"] = "            Mass shift";
                        row["Value"] = silacAA.MassShift;
                        dtParams.Rows.Add(row);
                    }
                }
            }

            if (IsobaricLabelling_Search)
            {
                row = dtParams.NewRow();
                row["Property"] = "Isobaric Labelling Search";
                row["Value"] = IsobaricLabelling_Search;
                dtParams.Rows.Add(row);

                row = dtParams.NewRow();
                row["Property"] = "Reagent";
                row["Value"] = SelectedIsobaricLabelling_Mod.Name;
                dtParams.Rows.Add(row);

                row = dtParams.NewRow();
                row["Property"] = "       Mass";
                row["Value"] = SelectedIsobaricLabelling_Mod.MassShift;
                dtParams.Rows.Add(row);

                row = dtParams.NewRow();
                row["Property"] = "       Target residues";
                row["Value"] = !String.IsNullOrEmpty(SelectedIsobaricLabelling_Mod.TargetResidues) ? SelectedIsobaricLabelling_Mod.TargetResidues : "None";
                dtParams.Rows.Add(row);

                //row = dtParams.NewRow();
                //row["Property"] = "Free residue tolerance";
                //row["Value"] = IsobaricLabelling_AllowedFreeResidues;
                //dtParams.Rows.Add(row);
            }

            if (StaticModifications.Count > 0)
            {
                row = dtParams.NewRow();
                row["Property"] = "Static Modification(s)";
                dtParams.Rows.Add(row);

                for (int i = 0; i < StaticModifications.Count; i++)
                {
                    var mod = StaticModifications[i];
                    row = dtParams.NewRow();
                    row["Property"] = "  " + (i + 1) + ")  Name";
                    row["Value"] = mod.Name;
                    dtParams.Rows.Add(row);

                    row = dtParams.NewRow();
                    row["Property"] = "       Mass";
                    row["Value"] = mod.MassShift;
                    dtParams.Rows.Add(row);

                    row = dtParams.NewRow();
                    row["Property"] = "       Target residues";
                    row["Value"] = !String.IsNullOrEmpty(mod.TargetResidues) ? mod.TargetResidues : "None";
                    dtParams.Rows.Add(row);

                    row = dtParams.NewRow();
                    row["Property"] = "       N-term";
                    row["Value"] = mod.IsNTerm;
                    dtParams.Rows.Add(row);

                    row = dtParams.NewRow();
                    row["Property"] = "       C-term";
                    row["Value"] = mod.IsCTerm;
                    dtParams.Rows.Add(row);

                }
            }

            if (VariableModifications.Count > 0)
            {

                row = dtParams.NewRow();
                row["Property"] = "Variable Modification(s)";
                dtParams.Rows.Add(row);

                for (int i = 0; i < VariableModifications.Count; i++)
                {
                    var mod = VariableModifications[i];
                    row = dtParams.NewRow();
                    row["Property"] = "  " + (i + 1) + ")  Name";
                    row["Value"] = mod.Name;
                    dtParams.Rows.Add(row);

                    row = dtParams.NewRow();
                    row["Property"] = "       Mass";
                    row["Value"] = mod.MassShift;
                    dtParams.Rows.Add(row);

                    row = dtParams.NewRow();
                    row["Property"] = "       Target residues";
                    row["Value"] = !String.IsNullOrEmpty(mod.TargetResidues) ? mod.TargetResidues : "None";
                    dtParams.Rows.Add(row);

                    row = dtParams.NewRow();
                    row["Property"] = "       N-term";
                    row["Value"] = mod.IsNTerm;
                    dtParams.Rows.Add(row);

                    row = dtParams.NewRow();
                    row["Property"] = "       C-term";
                    row["Value"] = mod.IsCTerm;
                    dtParams.Rows.Add(row);

                }
            }

            return dtParams;
        }

        public void PrintToConsole()
        {
            Console.WriteLine($"=============\nSearch parameters:\n=============");
            foreach (DataRow row in CreateParamsDatatable().Rows)
            {
                string property = row["Property"].ToString();
                string value = row["Value"].ToString();
                Console.WriteLine("{0}: {1}", property, value);
            }
        }

        public PredictionParameters GetLinearPredictorParameters()
        {
            var p = new PredictionParameters()
            {
                IsotopeMax = DeconvolutionForMSScoring ? 0 : IsotopesInTheoreticalMS,
                ChargeMax = DeconvolutionForMSScoring ? 1 : MaxChargeInTheoreticalMS,
                Offset = Offset,
                BinSize = BinSize,
                MaxMZ = DeconvolutionForMSScoring ? 3 * MaxBinMZ : MaxBinMZ,
                MinMZ = MinBinMZ,
                AddPrecursor = AddPrecursorToDotProduct,
                AddASeries = false,
                AddBSeries = true,
                AddCSeries = false,
                AddXSeries = false,
                AddYSeries = true,
                AddZSeries = false,
                ApplyMZRangeIntensityWeighting = false,
                NeutralLossH2O = ApplyNeutralLossH2O,
                NeutralLossNH3 = ApplyNeutralLossNH3,
                AlwaysAddExtraIons = false,
                ExtraIonMassThreshold = 500,
                ExtraAminoacids = ExtraAminoacids != null ? ExtraAminoacids.Select(a => (a.Tag, a.Mass)).ToList() : null
            };



            return p;

        }

        public CleavePredictionParameters GetCleavePredictorParameters()
        {
            var p = new CleavePredictionParameters()
            {
                IsotopeMax = DeconvolutionForMSScoring ? 0 : IsotopesInTheoreticalMS,
                ChargeMax = DeconvolutionForMSScoring ? 1 : MaxChargeInTheoreticalMS,
                AlwaysAddExtraIons = false,
                ExtraIonMassThreshold = DeconvolutionForMSScoring ? double.PositiveInfinity : 1000,
                BinSize = BinSize,
                Offset = Offset,
                MinMZ = MinBinMZ,
                MaxMZ = MaxBinMZ,
                NeutralLossH2O = ApplyNeutralLossH2O,
                NeutralLossNH3 = ApplyNeutralLossNH3,
                StarIntensity = StarIntensity,
                AddIonPairs = AddIonPairsToDotProduct,
                AddPrecursor = AddPrecursorToDotProduct,
                ApplyMZRangeIntensityWeighting = ApplyMZRangeWeighting,
                RegularIntensity = 1,
                Reagent = CXLReagent,
                ExtraAminoacids = ExtraAminoacids != null ? ExtraAminoacids.Select(a => (a.Tag, a.Mass)).ToList() : null
            };
            return p;
        }


        #region StaticDefaults

        public static readonly List<SpectrumWizard.AminoacidMod> defaultStaticMods = new()
        {
            AminoacidMod.CysteinCarbamidomethylation
        };

        public static readonly List<SpectrumWizard.AminoacidMod> defaultVariableMods = new()
        {
            AminoacidMod.MethinineOxidation,
        };
        #endregion

        #region SavingAndLoading
        public static ScoutParameters Load(string path)
        {
            string content = File.ReadAllText(path);

            var parameters = FileManagement.Serializer.FromJson<ScoutParameters>(
                content, true);

            ScoutParameters _searchParams = new ScoutParameters();
            _searchParams.SaveSpectraToResults = parameters.SaveSpectraToResults;
            _searchParams.AddContaminants = parameters.AddContaminants;
            _searchParams.AddDecoys = parameters.AddDecoys;
            _searchParams.FastaBatchSize = parameters.FastaBatchSize;
            _searchParams.BinSize = parameters.BinSize;
            _searchParams.Offset = parameters.Offset;
            _searchParams.MinBinMZ = parameters.MinBinMZ;
            _searchParams.MaxBinMZ = parameters.MaxBinMZ;
            _searchParams.IsotopicPossibilitiesPrecursor = parameters.IsotopicPossibilitiesPrecursor;
            _searchParams.SilacSearch = parameters.SilacSearch;
            _searchParams.SilacHybridMode = parameters.SilacHybridMode;
            _searchParams.SilacGroups = parameters.SilacGroups;
            _searchParams.IsobaricLabelling_Search = parameters.IsobaricLabelling_Search;
            _searchParams.IsobaricLabelling_AllowedFreeResidues = parameters.IsobaricLabelling_AllowedFreeResidues;
            _searchParams.IsobaricLabelling_Mods = parameters.IsobaricLabelling_Mods;
            _searchParams.SelectedIsobaricLabelling_Mod = parameters.SelectedIsobaricLabelling_Mod;
            _searchParams.PPMMS1Tolerance = parameters.PPMMS1Tolerance;
            _searchParams.PPMMS2Tolerance = parameters.PPMMS2Tolerance;
            _searchParams.MinPepLength = parameters.MinPepLength;
            _searchParams.MaxPepLength = parameters.MaxPepLength;
            _searchParams.MinPepMass = parameters.MinPepMass;
            _searchParams.MaxPepMass = parameters.MaxPepMass;
            _searchParams.MiscleavageNum = parameters.MiscleavageNum;
            _searchParams.MaximumVariableModsPerPeptide = parameters.MaximumVariableModsPerPeptide;
            _searchParams.StaticModifications = parameters.StaticModifications;
            _searchParams.VariableModifications = parameters.VariableModifications;
            _searchParams.PairFinderPPM = parameters.PairFinderPPM;
            _searchParams.DeconvolutionForPairSearching = parameters.DeconvolutionForPairSearching;
            _searchParams.DeconvolutionForMSScoring = parameters.DeconvolutionForMSScoring;
            _searchParams.Enzyme = parameters.Enzyme;
            _searchParams.EnzymeSpecificity = parameters.EnzymeSpecificity;
            _searchParams.CXLReagent = parameters.CXLReagent;
            _searchParams.ReplaceNullPoissonToXLScore = parameters.ReplaceNullPoissonToXLScore;
            _searchParams.SearchLoopLinks = parameters.SearchLoopLinks;

            return _searchParams;
        }


        public static ScoutParameters LoadFromString(string content)
        {

            var parameters = FileManagement.Serializer.FromJson<ScoutParameters>(
                content, true);

            return parameters;
        }

        public string ToSaveJson(bool indentJson = true)
        {
            string content = FileManagement.Serializer.ToJSON(
                this, false, indentJson);
            return content;
        }

        public ScoutParameters Copy()
        {
            return LoadFromString(ToSaveJson());
        }

        public ScoutParameters Copy(ScoutParameters current_params)
        {
            ScoutParameters scoutParameters = new ScoutParameters();
            scoutParameters.BDP_Mode = current_params.BDP_Mode;
            scoutParameters.MSFileExtension = current_params.MSFileExtension;
            scoutParameters.PPMMS1Tolerance = current_params.PPMMS1Tolerance;
            scoutParameters.PPMMS2Tolerance = current_params.PPMMS2Tolerance;
            scoutParameters.PerformShotgunSearch = current_params.PerformShotgunSearch;
            scoutParameters.PerformCleaveXLSearch = current_params.PerformCleaveXLSearch;
            scoutParameters.SaveSpectraToResults = current_params.SaveSpectraToResults;
            scoutParameters.MaxQueryResults = current_params.MaxQueryResults;
            scoutParameters.MinPepLength = current_params.MinPepLength;
            scoutParameters.MaxPepLength = current_params.MaxPepLength;
            scoutParameters.MinPepMass = current_params.MinPepMass;
            scoutParameters.MaxPepMass = current_params.MaxPepMass;
            scoutParameters.FastaFile = current_params.FastaFile;
            scoutParameters.RawPath = current_params.RawPath;
            scoutParameters.AddMinusOneIsotope = current_params.AddMinusOneIsotope;
            scoutParameters.CarbonIsotopeShift = current_params.CarbonIsotopeShift;
            scoutParameters.IsotopicPossibilitiesPrecursor = current_params.IsotopicPossibilitiesPrecursor;
            scoutParameters.MiscleavageNum = current_params.MiscleavageNum;
            scoutParameters.Enzyme = current_params.Enzyme;
            scoutParameters.EnzymeSpecificity = current_params.EnzymeSpecificity;
            scoutParameters.MaximumVariableModsPerPeptide = current_params.MaximumVariableModsPerPeptide;
            scoutParameters.StaticModifications = current_params.StaticModifications;
            scoutParameters.VariableModifications = current_params.VariableModifications;
            scoutParameters.ExtraAminoacids = current_params.ExtraAminoacids;
            scoutParameters.SearchLoopLinks = current_params.SearchLoopLinks;
            scoutParameters.IonPairMaxCharge = current_params.IonPairMaxCharge;
            scoutParameters.CXLReagent = current_params.CXLReagent;
            scoutParameters.FullPairsOnly = current_params.FullPairsOnly;
            scoutParameters.PairFinderPPM = current_params.PairFinderPPM;
            scoutParameters.MinBinMZ = current_params.MinBinMZ;
            scoutParameters.MaxBinMZ = current_params.MaxBinMZ;
            scoutParameters.BinSize = current_params.BinSize;
            scoutParameters.Offset = current_params.Offset;
            scoutParameters.ScoreFunction = current_params.ScoreFunction;
            scoutParameters.MS2NormalizationTypes = current_params.MS2NormalizationTypes;
            scoutParameters.NormalizationWindowWidth = current_params.NormalizationWindowWidth;
            scoutParameters.NormalizationWindowPeaksKept = current_params.NormalizationWindowPeaksKept;
            scoutParameters.AddPrecursorToDotProduct = current_params.AddPrecursorToDotProduct;
            scoutParameters.AddIonPairsToDotProduct = current_params.AddIonPairsToDotProduct;
            scoutParameters.ApplyMZRangeWeighting = current_params.ApplyMZRangeWeighting;
            scoutParameters.ReorderCleaveCandidateScores = current_params.ReorderCleaveCandidateScores;
            scoutParameters.RemoveIonPairsFromExperimentalMS = current_params.RemoveIonPairsFromExperimentalMS;
            scoutParameters.RemovePrecursorFromExperimentalMS = current_params.RemovePrecursorFromExperimentalMS;
            scoutParameters.ReplaceNullPoissonToXLScore = current_params.ReplaceNullPoissonToXLScore;
            scoutParameters.ReorderingMethod = current_params.ReorderingMethod;
            scoutParameters.ReorderingCombinatorialDepthHeavy = current_params.ReorderingCombinatorialDepthHeavy;
            scoutParameters.ReorderingCombinatorialDepthLight = current_params.ReorderingCombinatorialDepthLight;
            scoutParameters.FastaBatchSize = current_params.FastaBatchSize;
            scoutParameters.ParallelPSMs = current_params.ParallelPSMs;
            scoutParameters.QueryBatches = current_params.QueryBatches;
            scoutParameters.MergeDatabase = current_params.MergeDatabase;
            scoutParameters.MethionineInitiator = current_params.MethionineInitiator;
            scoutParameters.AddDecoys = current_params.AddDecoys;
            scoutParameters.AddContaminants = current_params.AddContaminants;
            scoutParameters.DontShowContaminants = current_params.DontShowContaminants;
            scoutParameters.DecoyTag = current_params.DecoyTag;
            scoutParameters.DecoyGenerationMode = current_params.DecoyGenerationMode;
            scoutParameters.AddUnlabelledDecoys = current_params.AddUnlabelledDecoys;
            scoutParameters.UnlabelledDecoyTag = current_params.UnlabelledDecoyTag;
            scoutParameters.OffsetUnlabelledDecoy = current_params.OffsetUnlabelledDecoy;
            scoutParameters.OffsetUnlabelled = current_params.OffsetUnlabelled;
            scoutParameters.UnlabelledDecoysGenerationMode = current_params.UnlabelledDecoysGenerationMode;
            scoutParameters.AddLocusStringToPeptides = current_params.AddLocusStringToPeptides;
            scoutParameters.DeconvolutionForPairSearching = current_params.DeconvolutionForPairSearching;
            scoutParameters.DeconvolutionForMSScoring = current_params.DeconvolutionForMSScoring;
            scoutParameters.SilacSearch = current_params.SilacSearch;
            scoutParameters.SilacHybridMode = current_params.SilacHybridMode;
            scoutParameters.SilacGroups = current_params.SilacGroups;
            scoutParameters.SilacSearchNormalPeptides = current_params.SilacSearchNormalPeptides;
            scoutParameters.IsobaricLabelling_Search = current_params.IsobaricLabelling_Search;
            scoutParameters.IsobaricLabelling_Mods = current_params.IsobaricLabelling_Mods;
            scoutParameters.SelectedIsobaricLabelling_Mod = current_params.SelectedIsobaricLabelling_Mod;
            scoutParameters.IsobaricLabelling_AllowedFreeResidues = current_params.IsobaricLabelling_AllowedFreeResidues;
            scoutParameters.CalculatePairIntensities = current_params.CalculatePairIntensities;
            scoutParameters.AddXLasVariableMod = current_params.AddXLasVariableMod;
            scoutParameters.IsotopesInTheoreticalMS = current_params.IsotopesInTheoreticalMS;
            scoutParameters.MaxChargeInTheoreticalMS = current_params.MaxChargeInTheoreticalMS;
            scoutParameters.BonusMode = current_params.BonusMode;
            scoutParameters.BonusScore = current_params.BonusScore;
            scoutParameters.ApplyNeutralLossH2O = current_params.ApplyNeutralLossH2O;
            scoutParameters.ApplyNeutralLossNH3 = current_params.ApplyNeutralLossNH3;
            scoutParameters.StarIntensity = current_params.StarIntensity;
            scoutParameters.FastaDistinctByLocus = current_params.FastaDistinctByLocus;
            scoutParameters.ApplyQuantileIntensityThreshold = current_params.ApplyQuantileIntensityThreshold;
            scoutParameters.QuantileIntensityThreshold = current_params.QuantileIntensityThreshold;
            return scoutParameters;
        }

        #endregion


    }
}
