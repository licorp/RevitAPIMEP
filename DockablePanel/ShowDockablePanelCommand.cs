using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Quoc_MEP
{
    /// <summary>
    /// Command để show/hide MEP Tools Dockable Panel
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ShowDockablePanelCommand : IExternalCommand
    {
        private static bool _isPanelRegistered = false;
        private static MEPToolsPanel _panelInstance = null;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                
                // LUÔN set UIApplication vào cả static property và RevitContext
                MEPToolsPanel.SetUIApplication(uiapp);
                RevitContext.UIApplication = uiapp;
                
                // Nếu panel chưa được register (khi load bằng Addin Manager)
                if (!_isPanelRegistered)
                {
                    try
                    {
                        _panelInstance = new MEPToolsPanel(uiapp);
                        uiapp.RegisterDockablePane(
                            new DockablePaneId(MEPToolsPanel.PanelGuid),
                            "MEP Tools Panel",
                            _panelInstance
                        );
                        _isPanelRegistered = true;
                        MEPToolsPanelLogger.Info("DockablePanel registered via ShowDockablePanelCommand");
                    }
                    catch (Exception ex)
                    {
                        // Panel có thể đã được register rồi
                        MEPToolsPanelLogger.Warning($"Cannot register panel: {ex.Message}");
                        _isPanelRegistered = true;
                    }
                }
                
                // Lấy dockable pane
                var dockablePaneId = new DockablePaneId(MEPToolsPanel.PanelGuid);
                var dockablePane = uiapp.GetDockablePane(dockablePaneId);

                if (dockablePane != null)
                {
                    // Toggle show/hide
                    if (dockablePane.IsShown())
                    {
                        dockablePane.Hide();
                    }
                    else
                    {
                        dockablePane.Show();
                    }
                    
                    // Panel đã được khởi tạo với UIApplication trong constructor
                    // Không cần update lại
                    MEPToolsPanelLogger.Info("Panel shown successfully");
                }
                else
                {
                    TaskDialog.Show("Error", "MEP Tools Panel not found!\nPlease restart Revit or try loading again.");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                MEPToolsPanelLogger.Error("ShowDockablePanelCommand failed", ex);
                TaskDialog.Show("Error", $"Cannot show/hide dockable panel:\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
