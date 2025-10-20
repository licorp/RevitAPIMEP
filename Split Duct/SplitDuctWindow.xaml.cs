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

namespace Quoc_MEP.SplitDuct
{
    /// <summary>
    /// Interaction logic for SplitDuctWindow.xaml
    /// </summary>
    public partial class SplitDuctWindow : Window
    {
        public string SplitOption { get; set; }
        public double Distance { get; set; }
        public SplitDuctWindow()
        {
            InitializeComponent();

            //combobox
            List<string> options = new List<string>() { "Split Duct From Start Point", "Split Duct From End Point" };
            cbbOptions.ItemsSource = options;
            cbbOptions.SelectedIndex = 0;

            //textbox
            tbDistance.Text = "1120";

        }

        private void btOk_Click(object sender, RoutedEventArgs e)
        {
            SplitOption = cbbOptions.SelectedValue.ToString();
            string value = tbDistance.Text;

            bool check = double.TryParse(value, out double giatri);
            if (check)
            {
                Distance = giatri;
            }
            else
            {
                MessageBox.Show("Enter a number!", "Message");
            }

            DialogResult = true;
        }

        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
