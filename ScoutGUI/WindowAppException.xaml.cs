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

namespace Scout
{
    /// <summary>
    /// Interaction logic for WindowAppException.xaml
    /// </summary>
    public partial class WindowAppException : Window
    {
        public Exception ThrownException { get; set; }

        public WindowAppException(Exception exception)
        {
            InitializeComponent();

            ThrownException = exception;

            TextBoxException.Text = $"{exception.Message}\n{exception.StackTrace}";
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ButtonCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TextBoxException.Text);
        }
    }
}
