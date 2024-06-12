using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Windows.Controls;
using ListBox = System.Windows.Controls.ListBox;
using System.Windows.Threading;

namespace Scout.Util
{
    public class ListBoxStreamWriter : TextWriter
    {
        ListBox output = null;
        Mutex bufferAccess = new Mutex();

        public ListBoxStreamWriter(ListBox output)
        {
            this.output = output;
        }

        public override void Write(string value)
        {
            base.Write(value);
            try
            {
                bufferAccess.WaitOne();

                if (value.StartsWith("Reading"))//Spectrum parser
                    value = "INFO: " + value;
                else if (value.Contains("The operation was canceled"))
                    value = "WARNING: The operation has been canceled.";
                else if (value.Contains("The query has been canceled"))
                    value = "WARNING: The operation has been canceled.";

                if (value.Contains("%"))
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (output != null && output.Items != null && output.Items.Count > 0 && output.Items[output.Items.Count - 1].ToString().Contains("%"))
                        output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                        {
                            if (output.Items.Count - 1 > 0)
                                output.Items.RemoveAt(output.Items.Count - 1);
                        }));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    // When character data is written, append it to the text box.
                    output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { output.Items.Add(value.ToString()); }));
                    output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { output.SelectedIndex = output.Items.Count - 1; }));
                    output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { output.ScrollIntoView(output.SelectedItem); }));
                }

                else if (String.IsNullOrEmpty(value) ||
                    value.Contains("spectra skipped (0 isotopic envelopes)") ||
                    value.Contains("Plot time"))
                {
                    // When character data is written, append it to the text box.
                    output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { output.Items.Add(value.ToString()); }));
                    output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                    {
                        if (output.Items.Count - 1 > 0)
                            output.Items.RemoveAt(output.Items.Count - 1);
                    }));
                    output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { output.SelectedIndex = output.Items.Count - 1; }));
                    output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { output.ScrollIntoView(output.SelectedItem); }));
                }

            }
            catch (NullReferenceException) { }
            catch (Exception) { }
            finally
            {
                bufferAccess.ReleaseMutex();
            }
        }

        public override void WriteLine(string value)
        {
            base.Write(value);
            try
            {
                bufferAccess.WaitOne();

                if (value.StartsWith("Parsing file"))//Spectrum Parser
                    value = "INFO: " + value;
                else if (value.StartsWith("Reading"))//Spectrum parser
                    value = "INFO: " + value;
                else if (value.Equals("Working  on the MS2"))//Y.A.D.A.
                    value = "INFO: Deconvoluting MS/MS spectra...";
                else if (value.Equals("Done with MS2 procedure."))//Y.A.D.A.
                    value = "INFO: MS/MS spectra have been deconvoluted successfully.";
                else if (value.Contains("The operation was canceled"))
                    value = "WARNING: The operation has been canceled.";
                else if (value.Contains("The query has been canceled"))
                    value = "WARNING: The operation has been canceled.";

                if (value.Contains("%"))
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (output != null && output.Items != null && output.Items.Count > 0 && output.Items[output.Items.Count - 1].ToString().Contains("%"))
                        output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                        {
                            if (output.Items.Count - 1 > 0)
                                output.Items.RemoveAt(output.Items.Count - 1);
                        }));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    // When character data is written, append it to the text box.
                    output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { output.Items.Add(value.ToString()); }));
                    output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                    {
                        if (output.Items.Count - 1 > 0)
                            output.SelectedIndex = output.Items.Count - 1;
                    }));
                    output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { output.ScrollIntoView(output.SelectedItem); }));
                }
                else
                {
                    // When character data is written, append it to the text box.
                    output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { output.Items.Add(value.ToString()); }));

                    if (String.IsNullOrEmpty(value) ||
                    value.Contains("lines") ||
                    value.Contains("} not found in") ||
                    value.Contains("~ not found in") ||
                    value.Contains("Scan ") ||          //Quantitation
                    value.Contains("parsed in") ||            //FastaReaser
                    value.Contains("spectra skipped (0 isotopic envelopes)") || //Determine precursor charge state
                    value.Contains("Plot time") || //SpectrumViewer2
                    value.Contains("Finished plotting") || //SpectrumViewer2
                    value.Contains("proteins with same locus"))//FastaReader
                        output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
                        {
                            if (output.Items.Count - 1 > 0)
                                output.Items.RemoveAt(output.Items.Count - 1);
                        }));
                    output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { output.SelectedIndex = output.Items.Count - 1; }));
                    output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate () { output.ScrollIntoView(output.SelectedItem); }));
                }
            }
            catch (Exception e) { }
            finally
            {
                bufferAccess.ReleaseMutex();
            }
        }

        public string GetString
        {
            get
            {
                string retVal = "";
                try
                {
                    bufferAccess.WaitOne();
                    retVal = output.Items[output.Items.Count - 1].ToString();
                    output.Items.Clear();
                }
                catch (Exception)
                {

                }
                finally
                {
                    bufferAccess.ReleaseMutex();
                }
                return retVal;
            }
        }

        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }
    }
}
