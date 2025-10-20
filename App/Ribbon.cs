using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Quoc_MEP
{
    [Transaction(TransactionMode.Manual)]
    class Ribbon : IExternalApplication
    {
        private readonly string nameSpace = "Quoc_MEP.";
        private readonly string tabName = "Quoc_MEP";
        private readonly string path = Assembly.GetExecutingAssembly().Location;

        private BitmapImage Convert(Bitmap bimapImage)
        {
            MemoryStream memory = new MemoryStream();
            bimapImage.Save(memory, ImageFormat.Png);
            memory.Position = 0;
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }



        private void SetPull_Image(PulldownButton pull, Bitmap imageSource)
        {
            pull.LargeImage = Convert(imageSource);
        }

        private void SetPush_Image(PushButton push, Bitmap imageSource)
        {
            push.LargeImage = Convert(imageSource);
        }

        private void MyPush(RibbonPanel panel, PushButtonData data, Bitmap bitmap, string description)
        {
            PushButton push = panel.AddItem(data) as PushButton;
            push.ToolTip = description;
            SetPush_Image(push, bitmap);
        }


        private void MyPull(RibbonPanel panel, PulldownButtonData data, Bitmap bitmap, List<PushButtonData> list, string des, List<string> listdes)
        {
            PulldownButton pulldown = panel.AddItem(data) as PulldownButton;
            for (int i = 0; i < list.Count; i++)
            {
                PushButtonData pushdata = list[i];
                PushButton bt = pulldown.AddPushButton(pushdata);
                if (listdes.Count > 0)
                {
                    bt.ToolTip = listdes[i];
                }
            }
            SetPull_Image(pulldown, bitmap);
            pulldown.ToolTip = des;
        }


        private void Modify_Tool (RibbonPanel panel)
        {

            //place_Family
            PushButtonData place_Family = new PushButtonData("place_Family", "Place Family", path, nameSpace + "PlaceFamilyCmd");
            MyPush(panel, place_Family, Properties.Resources.place, "Place family from location of block in file link CAD.");

            //draw_Pipe
            PushButtonData draw_Pipe = new PushButtonData("draw_Pipe", "Draw Pipe", path, nameSpace + "DrawPipe");
            MyPush(panel, draw_Pipe, Properties.Resources.pipe, "Create and align multiple pipes.");


            //split_Duct
            PushButtonData split_Duct = new PushButtonData("split_Duct", "Split Duct", path, nameSpace + "SplitDuctCmd");
            MyPush(panel, split_Duct, Properties.Resources.split, "Divide the selected section of the duct into multiple duct sections of equal length.");

            //mep_UpDown
            PushButtonData mep_UpDown = new PushButtonData("mep_UpDown", "MEP Up Down", path, nameSpace + "MEPUpDownCmd");
            MyPush(panel, mep_UpDown, Properties.Resources.up, "A tool that supports resolving clashes within the MEP system.");

            //Rotate
            PushButtonData Rotate = new PushButtonData("Rotate", "Rotate Element", path, nameSpace + "RotateElementsCommand");
            MyPush(panel, Rotate, Properties.Resources.rotate, "Rotate Element with Angle");


        }

        private void Data_Tool(RibbonPanel panel)
        {
            //create_Sheet
            PushButtonData create_Sheet = new PushButtonData("create_Sheet", "Create " + '\n' + "Sheets", path, nameSpace + "SheetFromExcelCmd");
            MyPush(panel, create_Sheet, Properties.Resources.sheet, "Create sheets from Excel data.");

            //export_Schedule
            PushButtonData export_Schedule = new PushButtonData("export_Schedule", "Export " + '\n' + "Schedule", path, nameSpace + "SheetFromExcelCmd");
            MyPush(panel, export_Schedule, Properties.Resources.excel, "Export Schedule to Excel.");
        }

        private void Annotation_Tool(RibbonPanel panel)
        {
            //create_Sheet
            PushButtonData create_Sheet = new PushButtonData("create_Sheet", "Create " + '\n' + "Sheets", path, nameSpace + "SheetFromExcelCmd");
            MyPush(panel, create_Sheet, Properties.Resources.sheet, "Create sheets from Excel data.");

            //export_Schedule
            PushButtonData export_Schedule = new PushButtonData("export_Schedule", "Export " + '\n' + "Schedule", path, nameSpace + "SheetFromExcelCmd");
            MyPush(panel, export_Schedule, Properties.Resources.excel, "Export Schedule to Excel.");
        }
        private void Export_Tool(RibbonPanel panel)
        {
            //create_Sheet
            PushButtonData create_Sheet = new PushButtonData("create_Sheet", "Create " + '\n' + "Sheets", path, nameSpace + "SheetFromExcelCmd");
            MyPush(panel, create_Sheet, Properties.Resources.sheet, "Create sheets from Excel data.");

            //export_Schedule
            PushButtonData export_Schedule = new PushButtonData("export_Schedule", "Export " + '\n' + "Schedule", path, nameSpace + "SheetFromExcelCmd");
            MyPush(panel, export_Schedule, Properties.Resources.excel, "Export Schedule to Excel.");
        }

        public Result OnStartup(UIControlledApplication application)
        {

            //t?o tab c� t�n l� "RevitAPI_ARC_2023"
            application.CreateRibbonTab(tabName);

            //t?o panel
            RibbonPanel ModifyPanel = application.CreateRibbonPanel(tabName, "Modify");
            RibbonPanel dataPanel = application.CreateRibbonPanel(tabName, "Data");
            RibbonPanel AnnotationPanel = application.CreateRibbonPanel(tabName, "Annotation");
            RibbonPanel ExportPanel = application.CreateRibbonPanel(tabName, "Export");

            Modify_Tool(ModifyPanel);
            Data_Tool(dataPanel);
            Annotation_Tool(AnnotationPanel);
            Export_Tool(ExportPanel);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
