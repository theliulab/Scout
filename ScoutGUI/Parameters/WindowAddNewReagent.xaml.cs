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
    /// Interaction logic for WindowAddNewReagent.xaml
    /// </summary>
    public partial class WindowAddNewReagent : Window
    {
        public bool Accepted { get; private set; }
        public CleaveReagent Reagent { get; private set; }

        public WindowAddNewReagent()
        {
            InitializeComponent();
        }

        private void TextMass_TextChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TryUpdateMassShift();
        }

        private void TryUpdateMassShift()
        {
            try
            {
                if (TextLightMass == null ||
                    TextHeavyMass == null ||
                    TextMassShift == null ||
                    String.IsNullOrEmpty(TextLightMass.Text) ||
                    String.IsNullOrEmpty(TextHeavyMass.Text)) return;

                double light = double.Parse(TextLightMass.Text);
                double heavy = double.Parse(TextHeavyMass.Text);

                TextMassShift.Text = (heavy - light).ToString();
            }
            catch { }
        }

        private bool CheckParams()
        {
            if (String.IsNullOrEmpty(TextName.Text))
            {
                System.Windows.Forms.MessageBox.Show("Empty 'Name' field. Please fix it!", "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            else if (String.IsNullOrEmpty(TextLightMass.Text))
            {
                System.Windows.Forms.MessageBox.Show("Empty 'Light Fragment Mass' field. Please fix it!", "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            else if (String.IsNullOrEmpty(TextHeavyMass.Text))
            {
                System.Windows.Forms.MessageBox.Show("Empty 'Heavy Fragment Mass' field. Please fix it!", "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            else if (String.IsNullOrEmpty(TextFullMass.Text))
            {
                System.Windows.Forms.MessageBox.Show("Empty 'Full Mass' field. Please fix it!", "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            else if (String.IsNullOrEmpty(TextTargetResidues.Text))
            {
                System.Windows.Forms.MessageBox.Show("Empty 'Target Residues' field. Please fix it!", "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
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
                    Reagent = new CleaveReagent(
                    TextName.Text,
                    "Light",
                    double.Parse(TextLightMass.Text),
                    "Heavy",
                    double.Parse(TextHeavyMass.Text),
                    "Full",
                    double.Parse(TextFullMass.Text),
                    TextTargetResidues.Text,
                    (bool)CheckIsNTerm.IsChecked
                    );
                    Accepted = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error building reagent!\nError: {ex.Message}", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TextTargetResidues_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[a-zA-Z]"))
                e.Handled = true;
        }
    }
}
