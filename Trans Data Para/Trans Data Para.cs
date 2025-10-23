using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

namespace Quoc_MEP
{
    public class Trans_Data_Para : IExternalApplication
    {
        private static string AddInPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private const string PanelName = "MEP Tools";
        private const string ButtonName = "Copy Parameters";

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Tạo Ribbon Panel
                RibbonPanel panel = null;
                try
                {
                    panel = application.GetRibbonPanels("Addins").FirstOrDefault(p => p.Name == PanelName);
                }
                catch { }

                if (panel == null)
                {
                    panel = application.CreateRibbonPanel(PanelName);
                }

                // Tạo Push Button Command
                string thisAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                PushButtonData pbd = new PushButtonData(
                    "CopyParametersBtn",
                    ButtonName,
                    thisAssemblyPath,
                    "Quoc_MEP.CopyParametersCommand");

                PushButton pb = panel.AddItem(pbd) as PushButton;
                pb.ToolTip = "Copy dữ liệu giữa các Parameters";

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Lỗi", $"Lỗi khởi tạo Add-in: {ex.Message}");
                return Result.Failed;
            }
        }
    }

    /// <summary>
    /// External Command để mở cửa sổ copy parameters
    /// </summary>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class CopyParametersCommand : IExternalCommand
    {
        private static TransDataParaWindow _window;
        private static ExternalEvent _externalEvent;
        private static TransDataParaEventHandler _eventHandler;
        private static readonly object _windowLock = new object();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                
                // Initialize external event on first run
                if (_externalEvent == null)
                {
                    _eventHandler = new TransDataParaEventHandler();
                    _externalEvent = ExternalEvent.Create(_eventHandler);
                }

                // Show reusable form
                ShowReusableForm(uiApp);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private static void ShowReusableForm(UIApplication uiApp)
        {
            lock (_windowLock)
            {
                if (_window == null || !_window.IsLoaded)
                {
                    // Create new form
                    _window = new TransDataParaWindow(uiApp);
                    
                    // Subscribe to copy request event
                    _window.CopyRequested += (sender, e) =>
                    {
                        _eventHandler.SetRequest(e, _window);
                        _externalEvent.Raise();
                    };
                    
                    // Handle window closing
                    _window.Closed += (sender, e) =>
                    {
                        _window = null;
                    };
                    
                    // Show form modeless (non-blocking)
                    _window.Show();
                }
                else
                {
                    // Reuse existing form
                    _window.ShowForReuse();
                }
            }
        }
    }
}
