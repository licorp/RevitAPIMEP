using Autodesk.Revit.UI;
using Quoc_MEP.Lib;
using Quoc_MEP.UpDownTool.Event;
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

namespace Quoc_MEP.UpDownTool
{
    /// <summary>
    /// Interaction logic for MEPUpDownView.xaml
    /// </summary>
    public partial class MEPUpDownView : Window
    {
        private ExternalEvent DuctCutDownEvent;
        private ExternalEvent PipeCutDownEvent;
        private ExternalEvent CableTrayCutDownEvent;

        public MEPUpDownView()
        {
            InitializeComponent();

            //combobox Options
            var options = new List<string>() { "Cut Up", "Cut Down", "Move Up", "Move Down" };
            cbbOption.ItemsSource = options;
            cbbOption.SelectedIndex = 0;

            //combobox Angle
            var angles = new List<string>() { "45°", "90°" };
            cbbAngle.ItemsSource = angles;
            cbbAngle.SelectedIndex = 0;

            tbOffset.Text = "1000";


            //event
            DuctCutDownEvent ductCutEvent = new DuctCutDownEvent() { window = this };
            DuctCutDownEvent = ExternalEvent.Create(ductCutEvent);



        }


        private void BindingImage(string option, string angle)
        {

            string findOption = $"{option}-{angle}";

            List<BitmapImage> images = new List<BitmapImage>()
            {
                MEPLib.Convert(Properties.Resources.CutElbowUp45Img),
                MEPLib.Convert(Properties.Resources.CutElbowUp90Img),
                MEPLib.Convert(Properties.Resources.CutElbowDown45Img),
                MEPLib.Convert(Properties.Resources.CutElbowDown90Img),

                MEPLib.Convert(Properties.Resources.MoveDownElbow45Img),
                MEPLib.Convert(Properties.Resources.MoveDownElbow90Img),
                MEPLib.Convert(Properties.Resources.MoveUpElbow45Img),
                MEPLib.Convert(Properties.Resources.MoveUpElbow90Img),

            };

            List<PictureItem> pictureItems = new List<PictureItem>();

            int i = 0;
            foreach(string op in cbbOption.Items)
            {
                foreach(string goc in cbbAngle.Items)
                {
                    string name = $"{op}-{goc}";
                    PictureItem pictureItem = new PictureItem(images[i], name);
                    pictureItems.Add(pictureItem);
                    i++;
                }
            }


            PictureItem item = pictureItems.Find(x => x.Option == findOption);
            image.Source = item.Image;

        }

        private void cbbOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string option = cbbOption.SelectedValue.ToString();
                string angle = cbbAngle.SelectedValue.ToString();
                BindingImage(option, angle);
            }
            catch { }
            

        }

        private void cbbAngle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string option = cbbOption.SelectedValue.ToString();
                string angle = cbbAngle.SelectedValue.ToString();
                BindingImage(option, angle);
            }
            catch { }
        }



        public string Option;
        public string Angle;
        public double Offset;


        private void btOk_Click(object sender, RoutedEventArgs e)
        {
            Option =cbbOption.SelectedValue.ToString();
            Angle = cbbAngle.SelectedValue.ToString();

            bool isNumber = double.TryParse(tbOffset.Text, out double number);  
            if (!isNumber)
            {
                MessageBox.Show("Enter a number!");
                tbOffset.Text = "1000";
            }
            else
            {
                Offset = number;

                TabItem tabItem = tabControl.SelectedItem as TabItem;

                if (tabItem.Header.ToString() == "Duct") DuctCutDownEvent.Raise();
                else if (tabItem.Header.ToString() == "Pipe") PipeCutDownEvent.Raise();
                else CableTrayCutDownEvent.Raise();
            }

            
            
        }

        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
