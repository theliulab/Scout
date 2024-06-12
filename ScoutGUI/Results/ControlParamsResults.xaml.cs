using Accord.Math;
using Accord.Math.Geometry;
using ScoutPostProcessing;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Scout.Results
{
    /// <summary>
    /// Interaction logic for ControlParamsResults.xaml
    /// </summary>
    public partial class ControlParamsResults : System.Windows.Controls.UserControl
    {
        private XLFilteredResults _post { get; set; }
        private WindowResultsBrowser _windowResults { get; set; }

        public ControlParamsResults()
        {
            InitializeComponent();
        }

        internal void Load(XLFilteredResults post, WindowResultsBrowser windowResults)
        {
            _post = post;
            _windowResults = windowResults;
            DataGridSearchParams.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { DataGridSearchParams.ItemsSource = createDataTableSearchParams(post).AsDataView(); }));
            DataGridPostProcessingParams.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { DataGridPostProcessingParams.ItemsSource = createDataTablePostProcessingParams(post).AsDataView(); }));
        }

        /// <summary>
        /// Method responsible for crete data table for Search params
        /// </summary>
        /// <returns>data table</returns>
        private DataTable createDataTableSearchParams(XLFilteredResults post)
        {
            DataTable dtCSM = new DataTable();

            dtCSM.Columns.Add("Property");
            dtCSM.Columns.Add("Value");

            var row = dtCSM.NewRow();
            row["Property"] = "File version";
            row["Value"] = post.FileVersion;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Database file";
            row["Value"] = System.IO.Path.GetFileName(post.SearchParams.FastaFile);
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Fasta batch size";
            row["Value"] = post.SearchParams.FastaBatchSize;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Raw directory";
            row["Value"] = post.RawsFolder;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Spectra saved in the results";
            row["Value"] = post.SearchParams.SaveSpectraToResults;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Add contaminants";
            row["Value"] = post.SearchParams.AddContaminants;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Add decoys";
            row["Value"] = post.SearchParams.AddDecoys;
            dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Add precursor to Dot Product";
            //row["Value"] = post.SearchParams.AddPrecursorToDotProduct;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Add ion pairs to Dot Product";
            //row["Value"] = post.SearchParams.AddIonPairsToDotProduct;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Add protein accession number to peptides";
            //row["Value"] = post.SearchParams.AddLocusStringToPeptides;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Add -1 isotope";
            //row["Value"] = post.SearchParams.AddMinusOneIsotope;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Add unlabelled decoys";
            //row["Value"] = post.SearchParams.AddUnlabelledDecoys;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Add XL as variable modification";
            //row["Value"] = post.SearchParams.AddXLasVariableMod;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Apply m/z range weighting";
            //row["Value"] = post.SearchParams.ApplyMZRangeWeighting;
            //dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Fragment bin tolerance";
            row["Value"] = post.SearchParams.BinSize;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Fragment bin offset";
            row["Value"] = post.SearchParams.Offset;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Max fragment bin m/z";
            row["Value"] = post.SearchParams.MaxBinMZ;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Min fragment bin m/z";
            row["Value"] = post.SearchParams.MinBinMZ;
            dtCSM.Rows.Add(row);


            //row = dtCSM.NewRow();
            //row["Property"] = "Calculate pair intensities";
            //row["Value"] = post.SearchParams.CalculatePairIntensities;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Carbon Isotope shift (Da)";
            //row["Value"] = post.SearchParams.CarbonIsotopeShift;
            //dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Deconvolution for MS Searching";
            row["Value"] = post.SearchParams.DeconvolutionForMSScoring;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Deconvolution for Ion Pair Searching";
            row["Value"] = post.SearchParams.DeconvolutionForPairSearching;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Cross-linker";
            row["Value"] = post.SearchParams.CXLReagent.Name;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "  Mass";
            row["Value"] = post.SearchParams.CXLReagent.WholeMass;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "  Target N-term";
            row["Value"] = post.SearchParams.CXLReagent.TargetNTerm;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "  Light fragment";
            row["Value"] = post.SearchParams.CXLReagent.LightFragment;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "  Heavy fragment";
            row["Value"] = post.SearchParams.CXLReagent.HeavyFragment;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "  Reaction residue(s)";
            row["Value"] = post.SearchParams.CXLReagent.Targets;
            dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Decoy generation mode";
            //row["Value"] = post.SearchParams.DecoyGenerationMode;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Decoy tag";
            //row["Value"] = post.SearchParams.DecoyTag;
            //dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Enzyme";
            row["Value"] = post.SearchParams.Enzyme;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "  Enzyme specificity";
            row["Value"] = post.SearchParams.EnzymeSpecificity;
            dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Full pairs only";
            //row["Value"] = post.SearchParams.FullPairsOnly;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Ion pair max charge";
            //row["Value"] = post.SearchParams.IonPairMaxCharge;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Isotopes in theoretical spectrum";
            //row["Value"] = post.SearchParams.IsotopesInTheoreticalMS;
            //dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Isotopic possibilities precursor";
            row["Value"] = post.SearchParams.IsotopicPossibilitiesPrecursor;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Min peptide length";
            row["Value"] = post.SearchParams.MinPepLength;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Max peptide length";
            row["Value"] = post.SearchParams.MaxPepLength;
            dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Max charge in theoretical spectrum";
            //row["Value"] = post.SearchParams.MaxChargeInTheoreticalMS;
            //dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Max variable modifications per peptide";
            row["Value"] = post.SearchParams.MaximumVariableModsPerPeptide;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Min peptide mass";
            row["Value"] = post.SearchParams.MinPepMass;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Max peptide mass";
            row["Value"] = post.SearchParams.MaxPepMass;
            dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Maximum query results";
            //row["Value"] = post.SearchParams.MaxQueryResults;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Methionine initiator";
            //row["Value"] = post.SearchParams.MethionineInitiator;
            //dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Max miscleavages";
            row["Value"] = post.SearchParams.MiscleavageNum;
            dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "MS2 normalization method";
            //row["Value"] = post.SearchParams.MS2NormalizationTypes;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Normalization window peaks kept";
            //row["Value"] = post.SearchParams.NormalizationWindowPeaksKept;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Normalization window width";
            //row["Value"] = post.SearchParams.NormalizationWindowWidth;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "MS file extension";
            //row["Value"] = post.SearchParams.MSFileExtension;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Offset unlabelled decoy";
            //row["Value"] = post.SearchParams.OffsetUnlabelledDecoy;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Offset unlabelled";
            //row["Value"] = post.SearchParams.OffsetUnlabelled;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Pair finder ppm";
            //row["Value"] = post.SearchParams.PairFinderPPM;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Search in parallel";
            //row["Value"] = post.SearchParams.ParallelPSMs;
            //dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Ppm error on MS1 level";
            row["Value"] = post.SearchParams.PPMMS1Tolerance;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Ppm error on MS2 level";
            row["Value"] = post.SearchParams.PPMMS2Tolerance;
            dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Remove ion pairs from experimental spectrum";
            //row["Value"] = post.SearchParams.RemoveIonPairsFromExperimentalMS;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Remove precursor from experimental spectrum";
            //row["Value"] = post.SearchParams.RemovePrecursorFromExperimentalMS;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Reorder cleave candidate scores";
            //row["Value"] = post.SearchParams.ReorderCleaveCandidateScores;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Score function";
            //row["Value"] = post.SearchParams.ScoreFunction;
            //dtCSM.Rows.Add(row);

            if (post.SearchParams.SilacSearch)
            {
                row = dtCSM.NewRow();
                row["Property"] = "Silac Search";
                row["Value"] = post.SearchParams.SilacSearch;
                dtCSM.Rows.Add(row);

                row = dtCSM.NewRow();
                row["Property"] = "Silac Hybrid mode";
                row["Value"] = post.SearchParams.SilacHybridMode;
                dtCSM.Rows.Add(row);

                for (int i = 0; i < post.SearchParams.SilacGroups.Count; i++)
                {
                    var silacGroup = post.SearchParams.SilacGroups[i];
                    row = dtCSM.NewRow();
                    row["Property"] = "  " + (i + 1) + ")  Name";
                    row["Value"] = silacGroup.GroupName;
                    dtCSM.Rows.Add(row);

                    for (int j = 0; j < silacGroup.GroupAminoacids.Count; j++)
                    {
                        var silacAA = silacGroup.GroupAminoacids[j];
                        row = dtCSM.NewRow();
                        row["Property"] = "    " + (i + 1) + "." + (j + 1) + ")  Amino acid";
                        row["Value"] = silacAA.TargetResidue;
                        dtCSM.Rows.Add(row);

                        row = dtCSM.NewRow();
                        row["Property"] = "            Mass shift";
                        row["Value"] = silacAA.MassShift;
                        dtCSM.Rows.Add(row);
                    }
                }
            }

            //row = dtCSM.NewRow();
            //row["Property"] = "Star Intensity";
            //row["Value"] = post.SearchParams.StarIntensity;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Unlabelled decoys generation mode";
            //row["Value"] = post.SearchParams.UnlabelledDecoysGenerationMode;
            //dtCSM.Rows.Add(row);

            //row = dtCSM.NewRow();
            //row["Property"] = "Unlabelled tag";
            //row["Value"] = post.SearchParams.UnlabelledDecoyTag;
            //dtCSM.Rows.Add(row);

            if (post.SearchParams.StaticModifications.Count > 0)
            {
                row = dtCSM.NewRow();
                row["Property"] = "Static Modification(s)";
                dtCSM.Rows.Add(row);

                for (int i = 0; i < post.SearchParams.StaticModifications.Count; i++)
                {
                    var mod = post.SearchParams.StaticModifications[i];
                    row = dtCSM.NewRow();
                    row["Property"] = "  " + (i + 1) + ")  Name";
                    row["Value"] = mod.Name;
                    dtCSM.Rows.Add(row);

                    row = dtCSM.NewRow();
                    row["Property"] = "       Mass";
                    row["Value"] = mod.MassShift;
                    dtCSM.Rows.Add(row);

                    row = dtCSM.NewRow();
                    row["Property"] = "       Target residues";
                    row["Value"] = !String.IsNullOrEmpty(mod.TargetResidues) ? mod.TargetResidues : "None";
                    dtCSM.Rows.Add(row);

                    row = dtCSM.NewRow();
                    row["Property"] = "       N-term";
                    row["Value"] = mod.IsNTerm;
                    dtCSM.Rows.Add(row);

                    row = dtCSM.NewRow();
                    row["Property"] = "       C-term";
                    row["Value"] = mod.IsCTerm;
                    dtCSM.Rows.Add(row);

                }
            }

            if (post.SearchParams.VariableModifications.Count > 0)
            {

                row = dtCSM.NewRow();
                row["Property"] = "Variable Modification(s)";
                dtCSM.Rows.Add(row);

                for (int i = 0; i < post.SearchParams.VariableModifications.Count; i++)
                {
                    var mod = post.SearchParams.VariableModifications[i];
                    row = dtCSM.NewRow();
                    row["Property"] = "  " + (i + 1) + ")  Name";
                    row["Value"] = mod.Name;
                    dtCSM.Rows.Add(row);

                    row = dtCSM.NewRow();
                    row["Property"] = "       Mass";
                    row["Value"] = mod.MassShift;
                    dtCSM.Rows.Add(row);

                    row = dtCSM.NewRow();
                    row["Property"] = "       Target residues";
                    row["Value"] = !String.IsNullOrEmpty(mod.TargetResidues) ? mod.TargetResidues : "None";
                    dtCSM.Rows.Add(row);

                    row = dtCSM.NewRow();
                    row["Property"] = "       N-term";
                    row["Value"] = mod.IsNTerm;
                    dtCSM.Rows.Add(row);

                    row = dtCSM.NewRow();
                    row["Property"] = "       C-term";
                    row["Value"] = mod.IsCTerm;
                    dtCSM.Rows.Add(row);

                }
            }

            return dtCSM;
        }

        /// <summary>
        /// Method responsible for crete data table for Post processing params
        /// </summary>
        /// <returns>data table</returns>
        private DataTable createDataTablePostProcessingParams(XLFilteredResults post)
        {
            DataTable dtCSM = new DataTable();

            dtCSM.Columns.Add("Property");
            dtCSM.Columns.Add("Value");

            var row = dtCSM.NewRow();
            row["Property"] = "Use only unique XLs into PPIs";
            row["Value"] = post.PostParams.UniquePPIsOnly;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "Separate protein intra- and inter-crosslinks";
            row["Value"] = post.PostParams.FDRMode == FDRModes.SeparateIntraInter;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "FDR";
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "  CSM level";
            row["Value"] = post.PostParams.CSM_FDR;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "  Residue pair level";
            row["Value"] = post.PostParams.ResPair_FDR;
            dtCSM.Rows.Add(row);

            row = dtCSM.NewRow();
            row["Property"] = "  PPI level";
            row["Value"] = post.PostParams.PPI_FDR;
            dtCSM.Rows.Add(row);

            return dtCSM;
        }

        private void ButtonParamsEdit_Click(object sender, RoutedEventArgs e)
        {
            var w = new WindowPostParams(_post.PostParams);
            w.ShowDialog();

            if (w.Accepted)
            {
                bool noRun = false;
                if (CheckChangesOnPostParams(w.PostParams))
                {
                    var r = System.Windows.Forms.MessageBox.Show("Some post processing parameters have been modified. Do you want to filter again the results?", "Scout (beta version) :: Information", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Information);
                    if (r == System.Windows.Forms.DialogResult.Yes)
                    {
                        WindowResultsBrowser._post.PostParams = w.PostParams;
                        _post.PostParams = w.PostParams;
                        _windowResults.RunFilter();
                    }
                    else
                        noRun = true;
                }
                else
                    noRun = true;

                if (noRun == true)
                {
                    System.Windows.Forms.MessageBox.Show("Post processing parameters have not been modified.", "Scout (beta version) :: Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
            }
        }

        private bool CheckChangesOnPostParams(PostProcessingParameters postParamsCameFromAdvancedParams)
        {
            if (_post.PostParams.CSM_FDR != postParamsCameFromAdvancedParams.CSM_FDR)
                return true;
            else if (_post.PostParams.ResPair_FDR != postParamsCameFromAdvancedParams.ResPair_FDR)
                return true;
            else if (_post.PostParams.PPI_FDR != postParamsCameFromAdvancedParams.PPI_FDR)
                return true;
            else if (_post.PostParams.UniquePPIsOnly != postParamsCameFromAdvancedParams.UniquePPIsOnly)
                return true;
            else if (_post.PostParams.GroupedByGene != postParamsCameFromAdvancedParams.GroupedByGene)
                return true;
            else if (_post.PostParams.ApplyBoostFDR != postParamsCameFromAdvancedParams.ApplyBoostFDR)
                return true;
            else if (_post.PostParams.FDRMode != postParamsCameFromAdvancedParams.FDRMode)
                return true;
            return false;
        }
    }
}
