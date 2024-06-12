using PatternTools.MSParserLight;
using SpectrumWizard;
using SpectrumWizard.Predictors.CleavableXL;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Windows.Threading;
using ThermoFisher.CommonCore.Data;

namespace Scout.Results.SpectrumViewer
{
    /// <summary>
    /// Interaction logic for ControlTheoreticalIonsTable.xaml
    /// </summary>
    public partial class ControlTheoreticalIonsTable : System.Windows.Controls.UserControl
    {
        public class DatagridItemMatchedIon
        {
            public AnnotationIon AnnotationIon { get; set; }

            public string Fragment { get => $"{AnnotationIon.Tag}-{Series}{AnnotationIon.Number}({Charge})"; }
            public double MZ { get => AnnotationIon.MZ; }
            public double NeutralMass { get => AnnotationIon.NeutralMass; }
            public double Intensity { get => AnnotationIon.MatchedIntensity; }
            public double RelativeIntensity { get => AnnotationIon.MatchedRelativeIntensity; }
            public int Charge { get => AnnotationIon.Charge; }
            public int Isotope { get => AnnotationIon.IsotopeNumber; }
            public string Loss { get => AnnotationIon.Loss.ToString(); }
            public string Series { get => AnnotationIon.Series.ToString(); }
            public string Mod { get => AnnotationIon.Mod; }

            public DatagridItemMatchedIon(AnnotationIon a)
            {
                this.AnnotationIon = a;
            }
        }


        private MSUltraLight _ms;
        private CleaveAnnotationSpectrum _annotationSpectrum;
        private double ppm = 20;
        private List<AnnotationIon> _annotationIons;
        private string _alpha_peptide;
        private string? _beta_peptide;
        private bool _isLoopLink;
        private List<AnnotationIon> bseries_or_alpha;
        private List<AnnotationIon> cseries_or_alpha;
        private List<AnnotationIon> yseries_or_alpha;
        private List<AnnotationIon> zseries_or_alpha;
        private List<AnnotationIon> bseries_or_beta;
        private List<AnnotationIon> cseries_or_beta;
        private List<AnnotationIon> yseries_or_beta;
        private List<AnnotationIon> zseries_or_beta;
        private DataTable dtTheoreticalIons;
        private List<string> matchedIons;

        /// <summary>
        /// Style
        /// </summary>
        private string superscripts = @"⁰¹²³⁴⁵⁶⁷⁸⁹";
        private string superPlusSign = "\x207A";
        private Style centerCellStyle;
        private Style centerHeaderStyle;
        private Style centerMiddleNumberStyle;
        private Style centerPeptSeq;
        private Style matchPeakAlpha;
        private Style matchPeakBeta;

        public ControlTheoreticalIonsTable()
        {
            InitializeComponent();
        }

        private void loadMZIntoDatatable(DataRow newRow, int maxChargeSeries, string label, int peptCount, List<AnnotationIon> pIonListOriginal)
        {
            for (int _column = 1; _column <= maxChargeSeries; _column++)
            {
                List<AnnotationIon> pIonList = pIonListOriginal.Where(a => /*a..Contains(_alpha_peptide[peptCount].ToString()) &&*/ a.Charge == _column && a.Number == (peptCount + 1)).ToList();
                if (pIonList.Count > 0)
                {
                    AnnotationIon _ion = pIonList.MaxBy(a => a.MatchedRelativeIntensity);
                    string mz = _ion.MZ.ToString("0.0000");
                    if (_ion.MatchedIons != null)
                        matchedIons.Add(mz);

                    if (_column == 1)
                        newRow[label] = mz;
                    else
                        newRow[label + superscripts[_column] + superPlusSign] = mz;
                }
            }
        }
        private void addColumnIntoDatatable(string label, int maxChargeSeries)
        {
            dtTheoreticalIons.Columns.Add(label, typeof(string));
            for (int bColumns = 2; bColumns <= maxChargeSeries; bColumns++)
            {
                dtTheoreticalIons.Columns.Add(label + superscripts[bColumns] + "\x207A", typeof(string));
            }
        }

        private DataTable createDataTableTheoreticalIons()
        {
            dtTheoreticalIons = new DataTable();
            matchedIons = new(_annotationIons.Count);

            #region header -> columns
            if (_isLoopLink)
            {
                if (bseries_or_alpha.Count > 0)
                    addColumnIntoDatatable("b", bseries_or_alpha.Max(a => a.Charge));
                if (cseries_or_alpha.Count > 0)
                    addColumnIntoDatatable("c", cseries_or_alpha.Max(a => a.Charge));
            }
            else
            {
                if (bseries_or_alpha.Count > 0 || bseries_or_beta.Count > 0)
                    addColumnIntoDatatable("b", Math.Max(bseries_or_alpha.Count > 0 ? bseries_or_alpha.Max(a => a.Charge) : 0, bseries_or_beta.Count > 0 ? bseries_or_beta.Max(a => a.Charge) : 0));
                if (cseries_or_alpha.Count > 0 || cseries_or_beta.Count > 0)
                    addColumnIntoDatatable("c", Math.Max(cseries_or_alpha.Count > 0 ? cseries_or_alpha.Max(a => a.Charge) : 0, cseries_or_beta.Count > 0 ? cseries_or_beta.Max(a => a.Charge) : 0));
            }

            dtTheoreticalIons.Columns.Add("posBAlphaPept", typeof(int));//b-ion series
            dtTheoreticalIons.Columns.Add("alphaPept", typeof(string));//alpha peptide
            dtTheoreticalIons.Columns.Add("posYAlphaPept", typeof(int));//y-ion series

            if (_isLoopLink)
            {
                if (yseries_or_alpha.Count > 0)
                    addColumnIntoDatatable("y", yseries_or_alpha.Max(a => a.Charge));
                if (zseries_or_alpha.Count > 0)
                    addColumnIntoDatatable("z", zseries_or_alpha.Max(a => a.Charge));
            }
            else
            {
                if (yseries_or_alpha.Count > 0 || yseries_or_beta.Count > 0)
                    addColumnIntoDatatable("y", Math.Max(yseries_or_alpha.Count > 0 ? yseries_or_alpha.Max(a => a.Charge) : 0, yseries_or_beta.Count > 0 ? yseries_or_beta.Max(a => a.Charge) : 0));
                if (zseries_or_alpha.Count > 0 || zseries_or_alpha.Count > 0)
                    addColumnIntoDatatable("z", Math.Max(zseries_or_alpha.Count > 0 ? zseries_or_alpha.Max(a => a.Charge) : 0, zseries_or_beta.Count > 0 ? zseries_or_beta.Max(a => a.Charge) : 0));
            }
            #endregion

            #region alpha peptide
            for (int peptCount = 0; peptCount < _alpha_peptide.Length; peptCount++)
            {
                DataRow newRow = dtTheoreticalIons.NewRow();

                if (bseries_or_alpha.Count > 0)
                    loadMZIntoDatatable(newRow, bseries_or_alpha.Max(a => a.Charge), "b", peptCount, bseries_or_alpha);
                if (cseries_or_alpha.Count > 0)
                    loadMZIntoDatatable(newRow, cseries_or_alpha.Max(a => a.Charge), "c", peptCount, cseries_or_alpha);

                newRow["posBAlphaPept"] = peptCount + 1;
                newRow["alphaPept"] = _alpha_peptide[peptCount];
                newRow["posYAlphaPept"] = _alpha_peptide.Count() - peptCount;

                if (yseries_or_alpha.Count > 0)
                    loadMZIntoDatatable(newRow, yseries_or_alpha.Max(a => a.Charge), "y", _alpha_peptide.Count() - peptCount - 1, yseries_or_alpha);
                if (zseries_or_alpha.Count > 0)
                    loadMZIntoDatatable(newRow, zseries_or_alpha.Max(a => a.Charge), "z", _alpha_peptide.Count() - peptCount - 1, zseries_or_alpha);

                dtTheoreticalIons.Rows.Add(newRow);

            }
            #endregion

            if (!_isLoopLink)
            {
                dtTheoreticalIons.Rows.Add(dtTheoreticalIons.NewRow());

                #region beta peptide

                for (int peptCount = 0; peptCount < _beta_peptide.Length; peptCount++)
                {
                    DataRow newRow = dtTheoreticalIons.NewRow();

                    if (bseries_or_beta.Count > 0)
                        loadMZIntoDatatable(newRow, bseries_or_beta.Max(a => a.Charge), "b", peptCount, bseries_or_beta);
                    if (cseries_or_beta.Count > 0)
                        loadMZIntoDatatable(newRow, cseries_or_beta.Max(a => a.Charge), "c", peptCount, cseries_or_beta);

                    newRow["posBAlphaPept"] = peptCount + 1;
                    newRow["alphaPept"] = _beta_peptide[peptCount];
                    newRow["posYAlphaPept"] = _beta_peptide.Count() - peptCount;

                    if (yseries_or_beta.Count > 0)
                        loadMZIntoDatatable(newRow, yseries_or_beta.Max(a => a.Charge), "y", _beta_peptide.Count() - peptCount - 1, yseries_or_beta);
                    if (zseries_or_beta.Count > 0)
                        loadMZIntoDatatable(newRow, zseries_or_beta.Max(a => a.Charge), "z", _beta_peptide.Count() - peptCount - 1, zseries_or_beta);

                    dtTheoreticalIons.Rows.Add(newRow);

                }
                #endregion
            }

            return dtTheoreticalIons;
        }


        private void addColumnIntoDatagrid(string label, int maxChargeSeries, Style centerCellStyle, Style centerHeaderStyle)
        {
            DataGridTheoreticalIons.Columns.Add(createColumn(label, label, centerCellStyle, centerHeaderStyle));

            for (int _column = 2; _column <= maxChargeSeries; _column++)
            {
                string label_from2 = label + superscripts[_column] + superPlusSign;
                DataGridTheoreticalIons.Columns.Add(createColumn(label_from2, label_from2, centerCellStyle, centerHeaderStyle));
            }
        }
        private DataGridTextColumn createColumn(string header, string bind, Style cellStyle, Style headerStyle)
        {
            DataGridTextColumn _column = new DataGridTextColumn();
            _column.Header = header;
            _column.HeaderStyle = headerStyle;
            _column.Binding = new System.Windows.Data.Binding(bind);
            _column.Width = 75;
            _column.CellStyle = cellStyle;
            _column.IsReadOnly = true;

            return _column;
        }

        private void clearDatagrid()
        {
            //Set AutoGenerateColumns False
            DataGridTheoreticalIons.AutoGenerateColumns = false;

            //Verify if can clear datagridview
            try
            {
                this.DataGridTheoreticalIons.Columns.Clear();
            }
            catch (Exception) { }
        }

        private void loadDataGridLoopLink()
        {
            if (bseries_or_alpha.Count > 0)
                addColumnIntoDatagrid("b", bseries_or_alpha.Max(a => a.Charge), matchPeakBeta, centerHeaderStyle);
            if (cseries_or_alpha.Count > 0)
                addColumnIntoDatagrid("c", cseries_or_alpha.Max(a => a.Charge), matchPeakBeta, centerHeaderStyle);

            DataGridTheoreticalIons.Columns.Add(createColumn("", "posBAlphaPept", centerMiddleNumberStyle, centerPeptSeq));
            DataGridTheoreticalIons.Columns.Add(createColumn("", "alphaPept", centerPeptSeq, centerPeptSeq));
            DataGridTheoreticalIons.Columns.Add(createColumn("", "posYAlphaPept", centerMiddleNumberStyle, centerPeptSeq));

            if (yseries_or_alpha.Count > 0)
                addColumnIntoDatagrid("y", yseries_or_alpha.Max(a => a.Charge), matchPeakAlpha, centerHeaderStyle);
            if (zseries_or_alpha.Count > 0)
                addColumnIntoDatagrid("z", zseries_or_alpha.Max(a => a.Charge), matchPeakAlpha, centerHeaderStyle);

        }

        private void loadDataGridCrossLink()
        {
            if (bseries_or_alpha.Count > 0 || bseries_or_beta.Count > 0)
                addColumnIntoDatagrid("b", Math.Max(bseries_or_alpha.Count > 0 ? bseries_or_alpha.Max(a => a.Charge) : 0, bseries_or_beta.Count > 0 ? bseries_or_beta.Max(a => a.Charge) : 0), centerCellStyle, centerHeaderStyle);
            if (cseries_or_alpha.Count > 0 || cseries_or_beta.Count > 0)
                addColumnIntoDatagrid("c", Math.Max(cseries_or_alpha.Count > 0 ? cseries_or_alpha.Max(a => a.Charge) : 0, cseries_or_beta.Count > 0 ? cseries_or_beta.Max(a => a.Charge) : 0), centerCellStyle, centerHeaderStyle);

            DataGridTheoreticalIons.Columns.Add(createColumn("", "posBAlphaPept", centerMiddleNumberStyle, centerPeptSeq));
            DataGridTheoreticalIons.Columns.Add(createColumn("", "alphaPept", centerPeptSeq, centerPeptSeq));
            DataGridTheoreticalIons.Columns.Add(createColumn("", "posYAlphaPept", centerMiddleNumberStyle, centerPeptSeq));

            if (yseries_or_alpha.Count > 0 || yseries_or_beta.Count > 0)
                addColumnIntoDatagrid("y", Math.Max(yseries_or_alpha.Count > 0 ? yseries_or_alpha.Max(a => a.Charge) : 0, yseries_or_beta.Count > 0 ? yseries_or_beta.Max(a => a.Charge) : 0), centerCellStyle, centerHeaderStyle);
            if (zseries_or_alpha.Count > 0 || zseries_or_alpha.Count > 0)
                addColumnIntoDatagrid("z", Math.Max(zseries_or_alpha.Count > 0 ? zseries_or_alpha.Max(a => a.Charge) : 0, zseries_or_beta.Count > 0 ? zseries_or_beta.Max(a => a.Charge) : 0), centerCellStyle, centerHeaderStyle);

        }

        private void loadDatagrid()
        {
            clearDatagrid();
            if (_isLoopLink)
                loadDataGridLoopLink();
            else
                loadDataGridCrossLink();

            this.DataGridTheoreticalIons.ItemsSource = createDataTableTheoreticalIons().AsDataView();
        }

        public void Load(MSUltraLight ms, CleaveAnnotationSpectrum annotationSpectrum, string alpha_peptide, string? beta_peptide = null)
        {
            _ms = ms;
            _annotationSpectrum = annotationSpectrum;
            _alpha_peptide = alpha_peptide;
            _beta_peptide = beta_peptide;
            _isLoopLink = beta_peptide == null;
            centerCellStyle = new();
            centerCellStyle.Setters.Add(new Setter(System.Windows.Controls.TextBox.TextAlignmentProperty, TextAlignment.Center));

            #region load ions list
            _annotationIons = annotationSpectrum.AllIons;
            if (_annotationIons == null) return;

            if (!_isLoopLink)
            {
                bseries_or_alpha = _annotationIons.Where(a => (a.Tag == "α") && a.Series == SpectrumWizard.Predictors.FragmentSeries.B).ToList();
                cseries_or_alpha = _annotationIons.Where(a => (a.Tag == "α") && a.Series == SpectrumWizard.Predictors.FragmentSeries.C).ToList();
                yseries_or_alpha = _annotationIons.Where(a => (a.Tag == "α") && a.Series == SpectrumWizard.Predictors.FragmentSeries.Y).ToList();
                zseries_or_alpha = _annotationIons.Where(a => (a.Tag == "α") && a.Series == SpectrumWizard.Predictors.FragmentSeries.Z).ToList();
                bseries_or_beta = _annotationIons.Where(a => (a.Tag == "β") && a.Series == SpectrumWizard.Predictors.FragmentSeries.B).ToList();
                cseries_or_beta = _annotationIons.Where(a => (a.Tag == "β") && a.Series == SpectrumWizard.Predictors.FragmentSeries.C).ToList();
                yseries_or_beta = _annotationIons.Where(a => (a.Tag == "β") && a.Series == SpectrumWizard.Predictors.FragmentSeries.Y).ToList();
                zseries_or_beta = _annotationIons.Where(a => (a.Tag == "β") && a.Series == SpectrumWizard.Predictors.FragmentSeries.Z).ToList();
            }
            else
            {
                bseries_or_alpha = _annotationIons.Where(a => a.Series == SpectrumWizard.Predictors.FragmentSeries.B).ToList();
                cseries_or_alpha = _annotationIons.Where(a => a.Series == SpectrumWizard.Predictors.FragmentSeries.C).ToList();
                yseries_or_alpha = _annotationIons.Where(a => a.Series == SpectrumWizard.Predictors.FragmentSeries.Y).ToList();
                zseries_or_alpha = _annotationIons.Where(a => a.Series == SpectrumWizard.Predictors.FragmentSeries.Z).ToList();
            }
            #endregion

            #region load cell styles
            centerHeaderStyle = new();
            centerHeaderStyle.Setters.Add(new Setter(System.Windows.Controls.TextBox.TextAlignmentProperty, TextAlignment.Center));

            centerMiddleNumberStyle = new();
            centerMiddleNumberStyle.Setters.Add(new Setter(System.Windows.Controls.TextBox.TextAlignmentProperty, TextAlignment.Center));
            centerMiddleNumberStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontWeightProperty, FontWeights.Bold));
            centerMiddleNumberStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, Brushes.LightGray));

            centerPeptSeq = new();
            centerPeptSeq.Setters.Add(new Setter(System.Windows.Controls.TextBox.TextAlignmentProperty, TextAlignment.Center));
            centerPeptSeq.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontWeightProperty, FontWeights.Bold));

            matchPeakAlpha = new();
            matchPeakAlpha.Setters.Add(new Setter(System.Windows.Controls.TextBox.TextAlignmentProperty, TextAlignment.Center));
            matchPeakAlpha.Setters.Add(new Setter(System.Windows.Controls.TextBlock.ForegroundProperty, Brushes.Red));

            matchPeakBeta = new();
            matchPeakBeta.Setters.Add(new Setter(System.Windows.Controls.TextBox.TextAlignmentProperty, TextAlignment.Center));
            matchPeakBeta.Setters.Add(new Setter(System.Windows.Controls.TextBlock.ForegroundProperty, Brushes.Blue));
            #endregion

            loadDatagrid();

        }

        private void Button_SaveAsCSV(object sender, RoutedEventArgs e)
        {
            string sep = ",";
            string numberFormatting = "{0.000}";

            var sfd = new SaveFileDialog()
            {
                Title = "Save Fragment Ions",
                Filter = "Fragment Ions file|*.csv",
                AddExtension = true
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            var items = _annotationIons.Select(a => new DatagridItemMatchedIon(a)).ToList();
            StringBuilder sb = new StringBuilder();

            List<string> Headers = new()
            {
                "Fragment", "MZ", "NeutralMass", "Intensity", "RelativeIntensity", "Charge", "Isotope", "Loss", "Mod", "Series"
            };

            sb.AppendLine(string.Join(sep, Headers));

            foreach (var item in items)
            {
                var line = new List<string>(Headers.Count);
                foreach (var header in Headers)
                {
                    switch (header)
                    {
                        case "Fragment":
                            line.Add(item.Fragment);
                            break;
                        case "MZ":
                            line.Add(item.MZ.ToString(numberFormatting));
                            break;
                        case "NeutralMass":
                            line.Add(item.NeutralMass.ToString(numberFormatting));
                            break;
                        case "Intensity":
                            line.Add(item.Intensity.ToString(numberFormatting));
                            break;
                        case "RelativeIntensity":
                            line.Add(item.RelativeIntensity.ToString(numberFormatting));
                            break;
                        case "Charge":
                            line.Add(item.Charge.ToString());
                            break;
                        case "Isotope":
                            line.Add(item.Isotope.ToString());
                            break;
                        case "Loss":
                            if (item.Loss != null)
                                line.Add(item.Loss.ToString());
                            break;
                        case "Mod":
                            if (item.Mod != null)
                                line.Add(item.Mod.ToString());
                            break;
                        case "Series":
                            line.Add(item.Series.ToString());
                            break;
                    }
                }

                sb.AppendLine(string.Join(sep, line));
            }

            File.WriteAllText(sfd.FileName, sb.ToString());
        }

        private void DataGridTheoreticalIons_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => AlterRow(e)));
        }

        private DataRow ApplyCellStyle(DataGridRow row, int columnIndex, int middleArray)
        {
            DataGridCell cell = GetCell(DataGridTheoreticalIons, row, columnIndex);
            if (cell == null) return null;
            DataRowView? drw = row.Item as DataRowView;
            if (drw == null) return null;

            DataRow dr = drw.Row;

            if (!String.IsNullOrEmpty(dr.ItemArray[columnIndex]?.ToString()) && matchedIons.Contains(dr.ItemArray[columnIndex]?.ToString()))
            {
                if (!_isLoopLink)
                {
                    if (row.GetIndex() <= _alpha_peptide.Length)
                        cell.Style = matchPeakAlpha;
                    else
                        cell.Style = matchPeakBeta;
                }
                else
                {
                    if (columnIndex > middleArray)
                        cell.Style = matchPeakBeta;
                    else
                        cell.Style = matchPeakAlpha;
                }
            }
            else
                cell.Foreground = Brushes.Black;

            return dr;
        }

        private int GetMiddleIonsArray(object[]? array)
        {
            int i = 0;
            for (; i < array.Length; i++)
            {
                try
                {
                    Convert.ToDouble(array[i]);
                }
                catch (Exception)
                {
                    break;
                }
            }
            return i;
        }

        private void AlterRow(DataGridRowEventArgs e)
        {
            DataRow dr = ApplyCellStyle(e.Row, 0, int.MaxValue);
            if (dr == null) return;

            int totalColumns = dr.ItemArray.Length;
            int middleArray = GetMiddleIonsArray(dr.ItemArray);

            for (int i = 1; i < totalColumns; i++)
                ApplyCellStyle(e.Row, i, middleArray);
        }
        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                var v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T ?? GetVisualChild<T>(v);
                if (child != null) break;
            }
            return child;
        }
        private static DataGridCell GetCell(DataGrid host, DataGridRow row, int columnIndex)
        {
            if (row == null) return null;

            var presenter = GetVisualChild<DataGridCellsPresenter>(row);
            if (presenter == null) return null;

            // try to get the cell but it may possibly be virtualized
            var cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex);
            if (cell == null)
            {
                // now try to bring into view and retreive the cell
                host.ScrollIntoView(row, host.Columns[columnIndex]);
                cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex);
            }
            return cell;

        }
    }
}
