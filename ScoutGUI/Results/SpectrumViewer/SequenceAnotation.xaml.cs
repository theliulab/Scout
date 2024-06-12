using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    /// Interaction logic for SequenceAnotation.xaml
    /// </summary>
    public partial class SequenceAnotation : UserControl
    {
        bool IsLoopLink { get; set; } = false;
        public SequenceAnotation()
        {
            InitializeComponent();
        }

        public double MyTopOffsetText { get; set; } = 15;
        public double MyFontSize { get; set; } = 18;
        public double MyStep { get; set; } = 10;

        private void PlotLoopLink(List<(char AA, string N_termSeries, string C_termSeries)> alpha_annotations,
            int alpha_position,
            int beta_position)
        {
            bool showInterPipeXL = alpha_annotations.Count > 0 ? true : false;

            double alpha_offset = 0;
            double beta_offset = 0;

            double total_width = 0;
            double widthSum = 0;
            MyTopOffsetText = 15;

            #region alpha peptide

            for (int countAnnot = 0; countAnnot < alpha_annotations.Count; countAnnot++)
            {
                TextBlock textBlock = new TextBlock()
                {
                    Foreground = new SolidColorBrush(Colors.Black),
                    FontFamily = new FontFamily("Courier New"),
                    FontSize = MyFontSize
                };

                if (alpha_annotations[countAnnot].AA == '(')
                {
                    textBlock.FontWeight = FontWeights.Bold;
                }

                textBlock.Text = Util.Util.CleanPeptide(alpha_annotations[countAnnot].AA.ToString(), true);

                Size tbSize = MeasureString(textBlock);

                if (countAnnot == 0)
                {
                    widthSum = tbSize.Width + MyStep;

                    #region add series label

                    #region add y series label
                    TextBlock tb_y_series = new TextBlock()
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        FontSize = 8,
                        Foreground = new SolidColorBrush(Colors.Blue)
                    };
                    tb_y_series.Text = "y";
                    CanvasSequence.Children.Add(tb_y_series);
                    Canvas.SetLeft(tb_y_series, tbSize.Width + widthSum - 2 * MyStep);
                    Canvas.SetTop(tb_y_series, MyTopOffsetText - (tbSize.Height * 0.15) - 23);
                    #endregion

                    #region add b series label
                    TextBlock tb_b_series = new TextBlock()
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        FontSize = 8,
                        Foreground = new SolidColorBrush(Colors.Red)
                    };
                    tb_b_series.Text = "b";
                    CanvasSequence.Children.Add(tb_b_series);
                    Canvas.SetLeft(tb_b_series, tbSize.Width + widthSum - 2 * MyStep);
                    Canvas.SetTop(tb_b_series, MyTopOffsetText - (tbSize.Height * 0.15) + (tbSize.Height * 1.65));
                    #endregion

                    #endregion
                }

                if (countAnnot == (alpha_position - 1))
                    alpha_offset = widthSum;
                if (countAnnot == (beta_position - 1))
                    beta_offset = widthSum;

                Canvas.SetLeft(textBlock, widthSum);
                Canvas.SetTop(textBlock, MyTopOffsetText);

                CanvasSequence.Children.Add(textBlock);

                //include the pipe
                if (alpha_annotations[countAnnot].Item2.Length > 0 || alpha_annotations[countAnnot].Item3.Length > 0)
                {
                    Line _pipe = new Line()
                    {
                        X1 = 0,
                        X2 = 0,
                        Y1 = 0,
                        Y2 = tbSize.Height * 1.1,

                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };

                    Canvas.SetTop(_pipe, MyTopOffsetText - (tbSize.Height * 0.15));
                    CanvasSequence.Children.Add(_pipe);

                    double tickSize = 10;

                    // y series
                    if (alpha_annotations[countAnnot].Item2.Length > 0)
                    {
                        Line l2 = new Line()
                        {
                            X1 = 0,
                            X2 = tickSize,  // 150 too far
                            Y1 = tickSize,
                            Y2 = 0,

                            Stroke = Brushes.Blue,
                            StrokeThickness = 1
                        };

                        Canvas.SetLeft(_pipe, tbSize.Width + widthSum + 4);
                        Canvas.SetLeft(l2, tbSize.Width + widthSum + 4);
                        Canvas.SetTop(l2, MyTopOffsetText - (tbSize.Height * 0.15) - tickSize);
                        CanvasSequence.Children.Add(l2);

                        //And now add the label
                        TextBlock tb = new TextBlock()
                        {
                            HorizontalAlignment = HorizontalAlignment.Left,
                            FontSize = 8
                        };
                        tb.Text = "" + (alpha_annotations.Count - countAnnot);
                        CanvasSequence.Children.Add(tb);
                        Canvas.SetLeft(tb, tbSize.Width + widthSum + 10);
                        Canvas.SetTop(tb, MyTopOffsetText - (tbSize.Height * 0.15) - 2 * tickSize - 3);
                    }

                    // b series
                    if (alpha_annotations[countAnnot].Item3.Length > 0)
                    {
                        Line l3 = new Line()
                        {
                            X1 = 0,
                            X2 = -tickSize,
                            Y1 = 0,
                            Y2 = tickSize,

                            Stroke = Brushes.Red,
                            StrokeThickness = 1
                        };

                        Canvas.SetLeft(_pipe, tbSize.Width + widthSum + 4);
                        Canvas.SetLeft(l3, tbSize.Width + widthSum + 4);
                        Canvas.SetTop(l3, (MyTopOffsetText - (tbSize.Height * 0.15)) + (tbSize.Height * 1.1));
                        CanvasSequence.Children.Add(l3);

                        //And now add the label
                        TextBlock tb = new TextBlock()
                        {
                            HorizontalAlignment = HorizontalAlignment.Left,
                            FontSize = 8
                        };
                        tb.Text = "" + (countAnnot + 1);
                        CanvasSequence.Children.Add(tb);
                        Canvas.SetLeft(tb, tbSize.Width + widthSum - MyStep);
                        Canvas.SetTop(tb, MyTopOffsetText - (tbSize.Height * 0.15) + (tbSize.Height * 1.65));
                    }

                }

                widthSum += tbSize.Width + MyStep;
            }
            total_width = widthSum;
            widthSum = 0;
            #endregion

            MyTopOffsetText += 42;

            #region vertical pipe

            if (showInterPipeXL)
            {
                Line _pipeXL1 = new Line()
                {
                    X1 = 0,
                    X2 = 0,
                    Y1 = 0,
                    Y2 = 20,

                    Stroke = Brushes.Black,
                    StrokeThickness = 3
                };

                Canvas.SetLeft(_pipeXL1, alpha_offset + 2.5 * MyStep);
                Canvas.SetTop(_pipeXL1, MyTopOffsetText);
                CanvasSequence.Children.Add(_pipeXL1);

                Line _pipeXL2 = new Line()
                {
                    X1 = 0,
                    X2 = 0,
                    Y1 = 0,
                    Y2 = 20,

                    Stroke = Brushes.Black,
                    StrokeThickness = 3
                };

                Canvas.SetLeft(_pipeXL2, beta_offset + 2.5 * MyStep);
                Canvas.SetTop(_pipeXL2, MyTopOffsetText);
                CanvasSequence.Children.Add(_pipeXL2);

                Line _pipeXL3 = new Line()
                {
                    X1 = -1,
                    X2 = beta_offset - alpha_offset,
                    Y1 = 20,
                    Y2 = 20,

                    Stroke = Brushes.Black,
                    StrokeThickness = 3
                };

                Canvas.SetLeft(_pipeXL3, alpha_offset + 2.5 * MyStep);
                Canvas.SetTop(_pipeXL3, MyTopOffsetText);
                CanvasSequence.Children.Add(_pipeXL3);
            }

            #endregion

            MyTopOffsetText += 70;

            total_width += widthSum;

            CanvasSequence.Width = total_width;
        }

        private void PlotCrossLink(List<(char AA, string N_termSeries, string C_termSeries)> alpha_annotations,
            List<(char AA, string N_termSeries, string C_termSeries)> beta_annotations,
            int alpha_position,
            int beta_position)
        {
            bool showInterPipeXL = alpha_annotations.Count > 0 && beta_annotations.Count > 0 ? true : false;

            bool alpha_forward = false;
            double alpha_offset = 0;
            double beta_offset = 0;

            if (beta_position > alpha_position)
                alpha_forward = true;

            if (alpha_forward == true)
                alpha_offset = beta_position - alpha_position;
            else
                beta_offset = alpha_position - beta_position;

            double total_width = 0;
            double widthSum = 0;
            MyTopOffsetText = 15;

            #region alpha peptide

            for (int countAnnot = 0; countAnnot < alpha_annotations.Count; countAnnot++)
            {
                TextBlock textBlock = new TextBlock()
                {
                    Foreground = new SolidColorBrush(Colors.Black),
                    FontFamily = new FontFamily("Courier New"),
                    FontSize = MyFontSize
                };

                if (alpha_annotations[countAnnot].AA == '(')
                {
                    textBlock.FontWeight = FontWeights.Bold;
                }

                textBlock.Text = Util.Util.CleanPeptide(alpha_annotations[countAnnot].AA.ToString(), true);

                Size tbSize = MeasureString(textBlock);

                if (countAnnot == 0)
                {
                    if (showInterPipeXL)
                    {
                        widthSum = alpha_offset * (tbSize.Width + MyStep);
                        alpha_offset = widthSum;
                    }

                    #region add series label

                    #region add y series label
                    TextBlock tb_y_series = new TextBlock()
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        FontSize = 8,
                        Foreground = new SolidColorBrush(Colors.Red)
                    };
                    tb_y_series.Text = "y";
                    CanvasSequence.Children.Add(tb_y_series);
                    Canvas.SetLeft(tb_y_series, tbSize.Width + widthSum - 2 * MyStep);
                    Canvas.SetTop(tb_y_series, MyTopOffsetText - (tbSize.Height * 0.15) - 23);
                    #endregion

                    #region add b series label
                    TextBlock tb_b_series = new TextBlock()
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        FontSize = 8,
                        Foreground = new SolidColorBrush(Colors.Red)
                    };
                    tb_b_series.Text = "b";
                    CanvasSequence.Children.Add(tb_b_series);
                    Canvas.SetLeft(tb_b_series, tbSize.Width + widthSum - 2 * MyStep);
                    Canvas.SetTop(tb_b_series, MyTopOffsetText - (tbSize.Height * 0.15) + (tbSize.Height * 1.65));
                    #endregion

                    #endregion
                }

                Canvas.SetLeft(textBlock, widthSum);
                Canvas.SetTop(textBlock, MyTopOffsetText);

                CanvasSequence.Children.Add(textBlock);

                //include the pipe
                if (alpha_annotations[countAnnot].Item2.Length > 0 || alpha_annotations[countAnnot].Item3.Length > 0)
                {
                    Line _pipe = new Line()
                    {
                        X1 = 0,
                        X2 = 0,
                        Y1 = 0,
                        Y2 = tbSize.Height * 1.1,

                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };

                    Canvas.SetTop(_pipe, MyTopOffsetText - (tbSize.Height * 0.15));
                    CanvasSequence.Children.Add(_pipe);

                    double tickSize = 10;

                    // y series
                    if (alpha_annotations[countAnnot].Item2.Length > 0)
                    {
                        Line l2 = new Line()
                        {
                            X1 = 0,
                            X2 = tickSize,  // 150 too far
                            Y1 = tickSize,
                            Y2 = 0,

                            Stroke = Brushes.Red,
                            StrokeThickness = 1
                        };

                        Canvas.SetLeft(_pipe, tbSize.Width + widthSum + 4);
                        Canvas.SetLeft(l2, tbSize.Width + widthSum + 4);
                        Canvas.SetTop(l2, MyTopOffsetText - (tbSize.Height * 0.15) - tickSize);
                        CanvasSequence.Children.Add(l2);

                        //And now add the label
                        TextBlock tb = new TextBlock()
                        {
                            HorizontalAlignment = HorizontalAlignment.Left,
                            FontSize = 8
                        };
                        tb.Text = "" + (alpha_annotations.Count - countAnnot);
                        CanvasSequence.Children.Add(tb);
                        Canvas.SetLeft(tb, tbSize.Width + widthSum + 10);
                        Canvas.SetTop(tb, MyTopOffsetText - (tbSize.Height * 0.15) - 2 * tickSize - 3);
                    }

                    // b series
                    if (alpha_annotations[countAnnot].Item3.Length > 0)
                    {
                        Line l3 = new Line()
                        {
                            X1 = 0,
                            X2 = -tickSize,
                            Y1 = 0,
                            Y2 = tickSize,

                            Stroke = Brushes.Red,
                            StrokeThickness = 1
                        };

                        Canvas.SetLeft(_pipe, tbSize.Width + widthSum + 4);
                        Canvas.SetLeft(l3, tbSize.Width + widthSum + 4);
                        Canvas.SetTop(l3, (MyTopOffsetText - (tbSize.Height * 0.15)) + (tbSize.Height * 1.1));
                        CanvasSequence.Children.Add(l3);

                        //And now add the label
                        TextBlock tb = new TextBlock()
                        {
                            HorizontalAlignment = HorizontalAlignment.Left,
                            FontSize = 8
                        };
                        tb.Text = "" + (countAnnot + 1);
                        CanvasSequence.Children.Add(tb);
                        Canvas.SetLeft(tb, tbSize.Width + widthSum - MyStep);
                        Canvas.SetTop(tb, MyTopOffsetText - (tbSize.Height * 0.15) + (tbSize.Height * 1.65));
                    }

                }

                if (countAnnot == alpha_position && showInterPipeXL)
                    alpha_offset = widthSum + MyStep / 2;

                widthSum += tbSize.Width + MyStep;
            }
            total_width = widthSum;
            widthSum = 0;
            #endregion

            MyTopOffsetText += 42;

            #region vertical pipe

            if (showInterPipeXL)
            {
                Line _pipeXL = new Line()
                {
                    X1 = 0,
                    X2 = 0,
                    Y1 = 0,
                    Y2 = 40,

                    Stroke = Brushes.Black,
                    StrokeThickness = 3
                };

                Canvas.SetLeft(_pipeXL, alpha_offset);
                Canvas.SetTop(_pipeXL, MyTopOffsetText);
                CanvasSequence.Children.Add(_pipeXL);
            }

            #endregion

            MyTopOffsetText += 70;

            #region beta peptide

            for (int countAnnot = 0; countAnnot < beta_annotations.Count; countAnnot++)
            {
                TextBlock textBlock = new TextBlock()
                {
                    Foreground = new SolidColorBrush(Colors.Black),
                    FontFamily = new FontFamily("Courier New"),
                    FontSize = MyFontSize
                };

                if (beta_annotations[countAnnot].AA == '(')
                {
                    textBlock.FontWeight = FontWeights.Bold;
                }

                textBlock.Text = Util.Util.CleanPeptide(beta_annotations[countAnnot].AA.ToString(), true);

                Size tbSize = MeasureString(textBlock);

                if (countAnnot == 0)
                {
                    if (showInterPipeXL)
                    {
                        widthSum = beta_offset * (tbSize.Width + MyStep);
                        beta_offset = widthSum;
                    }

                    #region add series label

                    #region add y series label
                    TextBlock tb_y_series = new TextBlock()
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        FontSize = 8,
                        Foreground = new SolidColorBrush(Colors.Blue)
                    };
                    tb_y_series.Text = "y";
                    CanvasSequence.Children.Add(tb_y_series);
                    Canvas.SetLeft(tb_y_series, tbSize.Width + widthSum - 2 * MyStep);
                    Canvas.SetTop(tb_y_series, MyTopOffsetText - (tbSize.Height * 0.15) - 23);
                    #endregion

                    #region add b series label
                    TextBlock tb_b_series = new TextBlock()
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        FontSize = 8,
                        Foreground = new SolidColorBrush(Colors.Blue)
                    };
                    tb_b_series.Text = "b";
                    CanvasSequence.Children.Add(tb_b_series);
                    Canvas.SetLeft(tb_b_series, tbSize.Width + widthSum - 2 * MyStep);
                    Canvas.SetTop(tb_b_series, MyTopOffsetText - (tbSize.Height * 0.15) + (tbSize.Height * 1.65));
                    #endregion

                    #endregion
                }

                Canvas.SetLeft(textBlock, widthSum);
                Canvas.SetTop(textBlock, MyTopOffsetText);

                CanvasSequence.Children.Add(textBlock);

                //include the pipe
                if (beta_annotations[countAnnot].Item2.Length > 0 || beta_annotations[countAnnot].Item3.Length > 0)
                {
                    Line _pipe = new Line()
                    {
                        X1 = 0,
                        X2 = 0,
                        Y1 = 0,
                        Y2 = tbSize.Height * 1.1,

                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };

                    Canvas.SetTop(_pipe, MyTopOffsetText - (tbSize.Height * 0.15));
                    CanvasSequence.Children.Add(_pipe);

                    double tickSize = 10;

                    // y series
                    if (beta_annotations[countAnnot].Item2.Length > 0)
                    {
                        Line l2 = new Line()
                        {
                            X1 = 0,
                            X2 = tickSize,  // 150 too far
                            Y1 = tickSize,
                            Y2 = 0,

                            Stroke = Brushes.Blue,
                            StrokeThickness = 1
                        };

                        Canvas.SetLeft(_pipe, tbSize.Width + widthSum + 4);
                        Canvas.SetLeft(l2, tbSize.Width + widthSum + 4);
                        Canvas.SetTop(l2, MyTopOffsetText - (tbSize.Height * 0.15) - tickSize);
                        CanvasSequence.Children.Add(l2);

                        //And now add the label
                        TextBlock tb = new TextBlock()
                        {
                            HorizontalAlignment = HorizontalAlignment.Left,
                            FontSize = 8
                        };
                        tb.Text = "" + (beta_annotations.Count - countAnnot);
                        CanvasSequence.Children.Add(tb);
                        Canvas.SetLeft(tb, tbSize.Width + widthSum + 10);
                        Canvas.SetTop(tb, MyTopOffsetText - (tbSize.Height * 0.15) - 2 * tickSize - 3);
                    }

                    // b series
                    if (beta_annotations[countAnnot].Item3.Length > 0)
                    {
                        Line l3 = new Line()
                        {
                            X1 = 0,
                            X2 = -tickSize,
                            Y1 = 0,
                            Y2 = tickSize,

                            Stroke = Brushes.Blue,
                            StrokeThickness = 1
                        };

                        Canvas.SetLeft(_pipe, tbSize.Width + widthSum + 4);
                        Canvas.SetLeft(l3, tbSize.Width + widthSum + 4);
                        Canvas.SetTop(l3, (MyTopOffsetText - (tbSize.Height * 0.15)) + (tbSize.Height * 1.1));
                        CanvasSequence.Children.Add(l3);

                        //And now add the label
                        TextBlock tb = new TextBlock()
                        {
                            HorizontalAlignment = HorizontalAlignment.Left,
                            FontSize = 8
                        };
                        tb.Text = "" + (countAnnot + 1);
                        CanvasSequence.Children.Add(tb);
                        Canvas.SetLeft(tb, tbSize.Width + widthSum - MyStep);
                        Canvas.SetTop(tb, MyTopOffsetText - (tbSize.Height * 0.15) + (tbSize.Height * 1.65));
                    }

                }

                if (countAnnot == alpha_position && showInterPipeXL)
                    beta_offset = widthSum + MyStep / 2;

                widthSum += tbSize.Width + MyStep;
            }
            #endregion

            total_width += widthSum;

            if (showInterPipeXL)
                CanvasSequence.Width = total_width / 2;
            else
                CanvasSequence.Width = total_width;
        }

        /// <summary>
        /// Plot peaks
        /// </summary>
        /// <param name="alpha_annotations">The Sequence Element, Top Anotation, Bottom Annotation</param>
        public void Plot(List<(char AA, string N_termSeries, string C_termSeries)> alpha_annotations,
            List<(char AA, string N_termSeries, string C_termSeries)> beta_annotations,
            int alpha_position,
            int beta_position)
        {
            CanvasSequence.Children.Clear();

            IsLoopLink = beta_annotations == null;

            if (IsLoopLink)
                PlotLoopLink(alpha_annotations, alpha_position, beta_position);
            else
                PlotCrossLink(alpha_annotations, beta_annotations, alpha_position, beta_position);

        }

        private Size MeasureString(TextBlock candidate)
        {
            var formattedText = new FormattedText(
                candidate.Text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(candidate.FontFamily, candidate.FontStyle, candidate.FontWeight, candidate.FontStretch),
                candidate.FontSize,
                Brushes.Black);

            return new Size(formattedText.Width, formattedText.Height);
        }
    }
}
