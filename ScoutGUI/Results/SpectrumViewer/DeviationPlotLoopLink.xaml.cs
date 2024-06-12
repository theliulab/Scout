using PatternTools.MSParserLight;
using SpectrumWizard;
using SpectrumWizard.Predictors.CleavableXL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Scout.Results.SpectrumViewer
{
    /// <summary>
    /// Interaction logic for DeviationPlotLoopLink.xaml
    /// </summary>
    public partial class DeviationPlotLoopLink : UserControl
    {
        internal class CandidateIon
        {
            public AnnotationIon PredictedIon { get; set; }
            public (double MZ, double Intensity) ExperimentalIon { get; set; }
            public double ppm { get; set; }
            public double ABS_ppm { get; set; }

            public CandidateIon(AnnotationIon predictedIon, (double MZ, double Intensity) experimentalIon, double ppm, double aBS_ppm)
            {
                PredictedIon = predictedIon;
                ExperimentalIon = experimentalIon;
                this.ppm = ppm;
                ABS_ppm = aBS_ppm;
            }
        }

        List<ScatterDataPoint> dict_b_alpha = new();
        List<ScatterDataPoint> dict_y_alpha = new();

        List<ScatterDataPoint> dict_b_beta = new();
        List<ScatterDataPoint> dict_y_beta = new();

        public DeviationPlotLoopLink()
        {
            InitializeComponent();
        }

        private void AddLoopLinkPoint(CandidateIon ion)
        {
            if (ion.PredictedIon.Series == SpectrumWizard.Predictors.FragmentSeries.B)
            {
                ScatterDataPoint sdp = new ScatterDataPoint();
                sdp.DependentValue = ion.ppm;
                sdp.IndependentValue = ion.ExperimentalIon.MZ;
                sdp.ToolTip = "Series: b" + ion.PredictedIon.Number + "\nm/z: " + ion.ExperimentalIon.MZ.ToString("0.0000") + "\nppm: " + ion.ppm.ToString("0.0000") + "\nCharge: " + ion.PredictedIon.Charge + "+";
                dict_b_alpha.Add(sdp);
            }
            else if (ion.PredictedIon.Series == SpectrumWizard.Predictors.FragmentSeries.Y)
            {
                ScatterDataPoint sdp = new ScatterDataPoint();
                sdp.DependentValue = ion.ppm;
                sdp.IndependentValue = ion.ExperimentalIon.MZ;
                sdp.ToolTip = "Series: y" + ion.PredictedIon.Number + "\nm/z: " + ion.ExperimentalIon.MZ.ToString("0.0000") + "\nppm: " + ion.ppm.ToString("0.0000") + "\nCharge: " + ion.PredictedIon.Charge + "+";
                dict_y_alpha.Add(sdp);
            }
        }

        public bool Plot(
            MSUltraLight ms,
            CleaveAnnotationSpectrum annotationSpectrum,
            double acceptableppm,
            List<Tuple<double, double>> pointsYRANSAC = null,
            bool isZoom = false)
        {
            #region load ions list

            dict_b_alpha = new();
            dict_y_alpha = new();

            List<AnnotationIon> _annotationIons = annotationSpectrum.AllIons.Where(a => a.Series != SpectrumWizard.Predictors.FragmentSeries.Pair && a.Series != SpectrumWizard.Predictors.FragmentSeries.Precursor && a.MatchedIons != null).ToList();
            if (_annotationIons == null) return false;

            #endregion

            Dictionary<double, double> pointsXY = new Dictionary<double, double>();
            foreach (var pIon in _annotationIons)
            {
                var candidateIons = (from expIon in ms.Ions.AsParallel()
                                     where Math.Abs(MatchingHelper.PPM(expIon.MZ, pIon.MZ)) <= acceptableppm
                                     select new CandidateIon(pIon, expIon, MatchingHelper.PPM(expIon.MZ, pIon.MZ), Math.Abs(MatchingHelper.PPM(expIon.MZ, pIon.MZ))))
                                         .OrderBy(a => a.ABS_ppm);

                foreach (var ion in candidateIons)
                {
                    AddLoopLinkPoint(ion);
                }
            }

            ((ScatterSeries)chartDeviationPlot.Series[0]).ItemsSource = dict_b_alpha;
            ((ScatterSeries)chartDeviationPlot.Series[1]).ItemsSource = dict_y_alpha;

            return true;
        }

        private void DataPoint_MouseEnter(object sender, MouseEventArgs e)
        {
            ((ScatterDataPoint)sender).ToolTip = ((ScatterDataPoint)((ScatterDataPoint)sender).DataContext).ToolTip;
        }
    }
}
