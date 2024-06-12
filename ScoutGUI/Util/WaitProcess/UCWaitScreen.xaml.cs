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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Scout.Util.WaitProcess
{
    /// <summary>
    /// Interaction logic for UCWaitScreen.xaml
    /// </summary>
    public partial class UCWaitScreen : UserControl
    {
        public UCWaitScreen()
        {
            InitializeComponent();
        }
        public void LoadMsgs(string primaryMessage, string secondaryMessage)
        {
            MyTextBlock.Text = primaryMessage;
            MyTextBlockSecondary.Text = secondaryMessage;
        }
        public UCWaitScreen(string primaryMessage, string secondaryMessage)
        {
            InitializeComponent();
            MyTextBlock.Text = primaryMessage;
            MyTextBlockSecondary.Text = secondaryMessage;
        }
    }
}
