using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using System.Data;
using System.IO;
using ExcelDataReader;
using Excel = Microsoft.Office.Interop.Excel;
using Window = System.Windows.Window;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;

namespace Quoc_MEP
{
    /// <summary>
    /// Interaction logic for SheetFromExcelView.xaml
    /// </summary>
    public partial class SheetFromExcelView : Window
    {
        private Document doc;
        public SheetFromExcelView(Document doc)
        {
            InitializeComponent();
            this.doc = doc;
            cbb_TitleBlocks.ItemsSource = GetListTitleBlocks();
            cbb_TitleBlocks.SelectedIndex = 0;
        }

        private void bt_Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Select Excel File";
            dialog.Filter = "Excel Files| *xls; *xlsx; *xlsm";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tb_FilePath.Text = dialog.FileName;
            }
            else tb_FilePath.Text = "";
        }

        private List<string> GetListTitleBlocks()
        {
            var collector = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_TitleBlocks)
                        .WhereElementIsElementType()
                        .Cast<FamilySymbol>()
                        .ToList();

            var listNames = (collector.Select(x => $"{x.FamilyName}: {x.Name}")).ToList();
            listNames.Sort();
            return listNames;

        }
        private ElementId GetBlockId (string blockName)
        {
            var collector = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_TitleBlocks)
                        .WhereElementIsElementType()
                        .Cast<FamilySymbol>()
                        .ToList();
            var id = collector.Find(x => $"{x.FamilyName}: {x.Name}" == blockName).Id;
            return id;
        }

        private void bt_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void bt_Ok_Click(object sender, RoutedEventArgs e)
        {
            string blockName = cbb_TitleBlocks.SelectedValue.ToString();
            ElementId blockId = GetBlockId(blockName);
           
            string filePath = tb_FilePath.Text;
            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("Ch?n file excel ho?c copy/paste du?ng d?n!", "Message");
            }
            else
            {
                if (filePath.Contains("\"")) filePath = filePath.Replace("\"", "");

                System.Data.DataTable excelData = new System.Data.DataTable();
                using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(fileStream))
                    {
                        var data = reader.AsDataSet();
                        if (data != null) excelData = data.Tables[0];
                    }
                }
                CloseExcelFile(filePath);
                CreateSheet(excelData, blockId);

            }    
        }

        private void CloseExcelFile(string filePath)
        {
            Excel.Application app = new Excel.Application();
            if (app != null)
            {
                foreach(Workbook workbook in app.Workbooks)
                {
                    if(workbook.FullName == filePath)
                    {
                        workbook.Close(false);
                        break;
                    }
                }
                Marshal.ReleaseComObject(app);
            }
        }

        private void CreateSheet (System.Data.DataTable excelData, ElementId blockId)
        {
            List<ViewSheet> sheetList = new List<ViewSheet>();
            using (var t = new Transaction(doc, " "))
            {
                t.Start();

                int rowCount = excelData.Rows.Count;
                int colCount = excelData.Columns.Count;
                for (int i = 1; i < rowCount; i++)
                {
                    ViewSheet vs = ViewSheet.Create(doc, blockId);
                    sheetList.Add(vs);

                    //set parameters
                    for (int j = 0; j < colCount; j++)
                    {
                        string header = excelData.Columns[j].ColumnName;
                        string parameterName = excelData.Rows[0][header].ToString();
                        string parameterValue = excelData.Rows[i][header].ToString();
                        SetParameters(vs, parameterName, parameterValue);
                    }

                }
                t.Commit();
            }
            Close();
            MessageBox.Show($"{sheetList.Count} sheets created!", "Message");
        }

        private void SetParameters(ViewSheet vs, string parameterName, string parameterValue)
        {
            try
            {
                Autodesk.Revit.DB.Parameter p = vs.LookupParameter(parameterName);
                if (p != null && !p.IsReadOnly)
                {
                    var paraType = p.StorageType;
                    if (paraType == StorageType.Integer)
                    {
                        p.Set(int.Parse(parameterValue));
                    }
                    else if (paraType == StorageType.Double)
                    {
                        p.Set(double.Parse(parameterValue));
                    }
                    else if (paraType == StorageType.String)
                    {
                        p.Set(parameterValue);
                    }
                }

            }
            catch { }
           
        }
    }
}
