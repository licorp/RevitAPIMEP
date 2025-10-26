using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Quoc_MEP
{
    /// <summary>
    /// Event Handler để xử lý copy parameters trong ExternalEvent
    /// </summary>
    public class TransDataParaEventHandler : IExternalEventHandler
    {
        private CopyParametersRequestEventArgs _request;
        private TransDataParaWindow _window;

        public void SetRequest(CopyParametersRequestEventArgs request, TransDataParaWindow window)
        {
            _request = request;
            _window = window;
        }

        public void Execute(UIApplication uiApp)
        {
            try
            {
                if (_request == null || _window == null)
                {
                    Logger.Warning("TransDataParaEventHandler: No request or window");
                    return;
                }

                Document doc = uiApp.ActiveUIDocument.Document;

                Logger.StartOperation("Copy Parameter Data");
                Logger.Info($"═══════════════════════════════════════");
                Logger.Info($"COPY CONFIGURATION:");
                Logger.Info($"  Source Group: {_request.SourceGroupName}");
                Logger.Info($"  Source Parameter: {_request.SourceParameterName}");
                Logger.Info($"  ↓");
                Logger.Info($"  Target Group: {_request.TargetGroupName}");
                Logger.Info($"  Target Parameter: {_request.TargetParameterName}");
                Logger.Info($"═══════════════════════════════════════");

                // Get elements by categories
                List<Element> elements = new List<Element>();

                if (_request.SelectedCategories.Count > 0)
                {
                    Logger.Info($"Selected Categories: {string.Join(", ", _request.SelectedCategories)}");
                    
                    foreach (string catName in _request.SelectedCategories)
                    {
                        var categoryElements = new FilteredElementCollector(doc)
                            .WhereElementIsNotElementType()
                            .Where(elem => elem.Category != null && elem.Category.Name == catName)
                            .ToList();
                        elements.AddRange(categoryElements);
                    }
                    
                    Logger.Info($"Found {elements.Count} elements in selected categories");
                }
                else
                {
                    elements = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .ToElements()
                        .ToList();
                    Logger.Info($"Processing ALL elements: {elements.Count}");
                }

                int totalElements = elements.Count;
                int successCount = 0;
                int skippedCount = 0;
                int errorCount = 0;

                Logger.Info($"Overwrite Mode: {(_request.OverwriteExisting ? "YES - Overwrite all" : "NO - Only empty parameters")}");

                using (Transaction trans = new Transaction(doc, "Copy Parameter Data"))
                {
                    trans.Start();

                    foreach (Element elem in elements)
                    {
                        try
                        {
                            // Get parameters by group
                            Parameter sourceParam = _window.GetParameterFromElementByGroup(
                                elem, _request.SourceParameterName, _request.SourceGroupName);
                            
                            Parameter targetParam = _window.GetParameterFromElementByGroup(
                                elem, _request.TargetParameterName, _request.TargetGroupName);

                            if (sourceParam != null && targetParam != null && !targetParam.IsReadOnly)
                            {
                                // CHECK: Nếu Source parameter RỖNG → BỎ QUA, không copy
                                if (_window.IsParameterEmpty(sourceParam))
                                {
                                    skippedCount++;
                                    return; // return trong lambda = continue trong foreach
                                }
                                
                                string sourceValue = _window.GetParameterValueString(sourceParam);
                                string targetValueBefore = _window.GetParameterValueString(targetParam);
                                
                                bool isInstanceParam = _window.IsInstanceParameter(targetParam);
                                bool shouldCopy = _request.OverwriteExisting || _window.IsParameterEmpty(targetParam);

                                if (shouldCopy)
                                {
                                    if (sourceParam.StorageType == targetParam.StorageType)
                                    {
                                        bool copySuccess = _window.CopyParameterValue(sourceParam, targetParam);
                                        if (copySuccess)
                                        {
                                            successCount++;
                                        }
                                    }
                                    else
                                    {
                                        skippedCount++;
                                    }
                                }
                                else
                                {
                                    skippedCount++;
                                }
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            Logger.Error($"  ID {elem.Id.IntegerValue}: Error - {ex.Message}");
                        }
                    }

                    trans.Commit();
                }

                Logger.Info($"=== RESULTS ===");
                Logger.Info($"Total Elements: {totalElements}");
                Logger.Info($"Successfully Copied: {successCount}");
                Logger.Info($"Skipped: {skippedCount}");
                Logger.Info($"Errors: {errorCount}");
                Logger.EndOperation("Copy Parameter Data");

                // Show result on UI thread
                _window.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"Copy thành công!\n\n" +
                        $"Tổng elements: {totalElements}\n" +
                        $"Đã copy: {successCount} elements\n" +
                        $"Bỏ qua (đã có giá trị): {skippedCount} elements\n" +
                        $"Lỗi: {errorCount} elements\n\n" +
                        $"Xem DebugView để theo dõi chi tiết!",
                        "Kết quả",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);

                    // Show form again for reuse
                    _window.ShowForReuse();
                });

            }
            catch (Exception ex)
            {
                Logger.Error("TransDataParaEventHandler.Execute failed", ex);
                
                _window?.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"Lỗi: {ex.Message}",
                        "Lỗi",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    
                    _window.ShowForReuse();
                });
            }
        }

        public string GetName()
        {
            return "TransDataParaEventHandler";
        }
    }

    /// <summary>
    /// Event args cho copy parameters request
    /// </summary>
    public class CopyParametersRequestEventArgs : EventArgs
    {
        public string SourceGroupName { get; set; }
        public string SourceParameterName { get; set; }
        public string TargetGroupName { get; set; }
        public string TargetParameterName { get; set; }
        public List<string> SelectedCategories { get; set; }
        public bool OverwriteExisting { get; set; }
    }
}
