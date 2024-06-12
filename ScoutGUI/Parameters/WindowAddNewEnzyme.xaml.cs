using Digestor;
using SpectrumWizard.Predictors.CleavableXL;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Scout.Parameters
{
    /// <summary>
    /// Interaction logic for WindowAddNewEnzyme.xaml
    /// </summary>
    public partial class WindowAddNewEnzyme : Window
    {
        public bool Accepted { get; private set; }
        public Enzyme Enzyme { get; private set; }

        public WindowAddNewEnzyme()
        {
            InitializeComponent();
        }

        private bool CheckParams()
        {
            if (String.IsNullOrEmpty(TextName.Text))
            {
                System.Windows.Forms.MessageBox.Show("Empty 'Name' field. Please fix it!", "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            else if (String.IsNullOrEmpty(TextCleavagesAA.Text))
            {
                System.Windows.Forms.MessageBox.Show("Empty 'Sites' field. Please fix it!", "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckParams())
                {
                    Enzyme = new Enzyme(
                        TextName.Text,
                        (bool)CheckIsCTerm.IsChecked,
                        TextCleavagesAA.Text,
                        TextNonCleavagesAA.Text);
                    this.Close();
                    Accepted = true;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error building enzyme!\nError: {ex.Message}", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Text_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[a-zA-Z]"))
                e.Handled = true;
        }
    }
}
