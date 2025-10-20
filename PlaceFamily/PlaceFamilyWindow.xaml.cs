using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Document = Autodesk.Revit.DB.Document;

namespace Quoc_MEP.PlaceFamily
{
    /// <summary>
    /// Interaction logic for PlaceFamilyWindow.xaml
    /// </summary>
    public partial class PlaceFamilyWindow : Window
    {
        private Document doc;
        private FamilyInstance instance;

        public string BlockName;
        public string FamilyName;
        public string TypeName;
        public string LevelName;
        public double Distance;

        public PlaceFamilyWindow(Document doc, FamilyInstance instance,  string fileCadName, List<string> listBlocks, List<string> listFamily)
        {
            InitializeComponent();
            this.doc = doc;
            this.instance = instance;



            tbFileCad.Text = fileCadName;
            //set list cad blocks
            cbbCadBlock.ItemsSource = listBlocks;
            cbbCadBlock.SelectedIndex = 0;

            //set list family
            cbbFamily.ItemsSource = listFamily;
            cbbFamily.SelectedIndex = 0;

            //set list Levels
            cbbLevel.ItemsSource = PlaceFamilyUtils.GetListLevelInModel(doc);
            cbbLevel.SelectedIndex = 0;

            tbDistance.Text = "0";
        }

        private void cbbFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string familyName = cbbFamily.SelectedValue.ToString();
            var listType = PlaceFamilyUtils.GetListTypeByFamilyName(doc, instance, familyName);
            cbbTypeName.ItemsSource = listType;
            cbbTypeName.SelectedIndex = 0;

        }

        private void btOk_Click(object sender, RoutedEventArgs e)
        {
            BlockName = cbbCadBlock.SelectedValue.ToString();
            FamilyName = cbbFamily.SelectedValue.ToString();
            TypeName = cbbTypeName.SelectedValue.ToString();
            LevelName = cbbLevel.SelectedValue.ToString();


            //check number
            bool isNumber = double.TryParse(tbDistance.Text, out double value);
            if (isNumber) Distance = value;
            else
            {
                MessageBox.Show("Enter an number", "Message");
                tbDistance.Text = "0";
            }

            DialogResult = true;
        }




        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
