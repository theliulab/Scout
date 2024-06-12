using SpectrumWizard;
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
    /// Interaction logic for WindowAddNewModification.xaml
    /// </summary>
    public partial class WindowAddNewModification : Window
    {
        public bool Accepted { get; internal set; }
        public AminoacidMod Modification { get; set; }

        public WindowAddNewModification(AminoacidMod modification)
        {
            InitializeComponent();
            Modification = modification;
            LoadFields();
            TextName.IsReadOnly = true;
        }

        private void LoadFields()
        {
            TextName.Text = Modification.Name;
            TextTargetResidues.Text = Modification.TargetResidues;
            CheckIsCTerm.IsChecked = Modification.IsCTerm;
            CheckIsNTerm.IsChecked = Modification.IsNTerm;
            CheckIsVariable.IsChecked = Modification.IsVariable;
            TextMassShift.Text = Modification.MassShift.ToString();
        }

        public WindowAddNewModification()
        {
            InitializeComponent();
            TextName.IsReadOnly = false;
        }

        private bool CheckParams()
        {
            if (String.IsNullOrEmpty(TextName.Text))
            {
                System.Windows.Forms.MessageBox.Show("Empty 'Name' field. Please fix it!", "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return false;
            }
            else if (String.IsNullOrEmpty(TextMassShift.Text))
            {
                System.Windows.Forms.MessageBox.Show("Empty 'Mass Shift' field. Please fix it!", "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
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
                    Modification = new AminoacidMod()
                    {
                        Name = TextName.Text.Trim(),
                        TargetResidues = TextTargetResidues.Text.Trim(),
                        IsCTerm = (bool)CheckIsCTerm.IsChecked,
                        IsNTerm = (bool)CheckIsNTerm.IsChecked,
                        IsVariable = (bool)CheckIsVariable.IsChecked,
                        MassShift = double.Parse(TextMassShift.Text),
                    };
                    Accepted = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error building reagent!\nError: {ex.Message}", "Information", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
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
