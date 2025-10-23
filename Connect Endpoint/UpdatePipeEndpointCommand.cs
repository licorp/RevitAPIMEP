using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace Quoc_MEP
{
    [Transaction(TransactionMode.Manual)]
    public class UpdatePipeEndpointCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Logger.LogMethodEntry(nameof(Execute));
            Logger.LogRevitEnvironment();
            
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            Logger.LogInfo($"Document: {doc.Title}, PathName: {doc.PathName}");

            try
            {
                // Bước 1: Chọn ống cần cập nhật endpoint
                Logger.LogInfo("Starting pipe selection");
                
                Pipe selectedPipe = SelectionHelper.SelectPipe(uiDoc);
                if (selectedPipe == null)
                {
                    Logger.LogWarning("No pipe selected by user");
                    return Result.Cancelled;
                }

                Logger.LogInfo($"Selected pipe: Name={selectedPipe.Name}, ID={selectedPipe.Id}");

                // Bước 2: Chọn đối tượng có connector để làm endpoint mới
                Logger.LogInfo("Starting target element selection");
                
                Element targetElement = SelectionHelper.SelectMEPElement(uiDoc);
                if (targetElement == null)
                {
                    Logger.LogWarning("No target element selected by user");
                    return Result.Cancelled;
                }

                Logger.LogInfo($"Selected target element: Name={targetElement.Name}, ID={targetElement.Id}, Category={targetElement.Category?.Name}");

                // Bước 3: Xử lý theo loại target và logic connector
                Connector targetConnector = null;
                List<Element> elementsToDelete = new List<Element>();
                List<Element> elementsToConnectAfter = new List<Element>();
                
                if (targetElement is Pipe targetPipe)
                {
                    Logger.LogInfo("Target is a pipe - using CONNECTED connector for position, then connect to fitting and delete pipe");
                    
                    // Lấy CONNECTED connector để làm target position
                    targetConnector = ConnectorHelper.GetConnectedConnector(targetPipe);
                    
                    // Tìm các fitting đã kết nối với pipe này để connect sau khi xóa
                    var allConnectedElements = PipeHelper.GetConnectedElements(targetPipe);
                    elementsToConnectAfter = allConnectedElements.Where(e => e.Id != selectedPipe.Id).ToList();
                    
                    Logger.LogInfo($"Found {allConnectedElements.Count} total connected elements, {elementsToConnectAfter.Count} to connect after deletion");
                    
                    // Đánh dấu pipe để xóa SAU KHI đã connect với fitting
                    elementsToDelete.Add(targetPipe);
                    Logger.LogInfo($"Marked target pipe for deletion AFTER fitting connection: {targetPipe.Name} (ID: {targetPipe.Id})");
                }
                else
                {
                    // Target là fitting - lấy NOT CONNECTED connector
                    Logger.LogInfo("Target is a fitting - using UNCONNECTED connector for endpoint");
                    targetConnector = ConnectorHelper.GetUnconnectedConnector(targetElement);
                }
                
                if (targetConnector == null)
                {
                    Logger.LogError("No suitable connector found on target element");
                    return Result.Failed;
                }

                Logger.LogInfo($"Target connector found: {ConnectorHelper.GetConnectorInfo(targetConnector)}");

                // Bước 4: Cập nhật endpoint và kết nối
                Logger.LogInfo("Starting pipe endpoint update transaction");
                using (Transaction trans = new Transaction(doc, "Cập nhật Pipe Endpoint"))
                {
                    trans.Start();
                    Logger.LogDebug("Transaction started");

                    // BƯỚC 3: Update endpoint của pipe A đến target position
                    bool success = PipeHelper.UpdatePipeEndpoint(selectedPipe, targetConnector);
                    
                    if (success)
                    {
                        Logger.LogInfo("Pipe endpoint updated successfully");
                        
                        // BƯỚC 4: Nếu target là pipe, xử lý kết nối và xóa
                        if (elementsToDelete.Count > 0)
                        {
                            if (elementsToConnectAfter.Count > 0)
                            {
                                Logger.LogInfo("Found fittings to connect - connecting before deletion");
                                
                                // Kết nối với fitting sử dụng ConnectTo để tạo kết nối thực sự
                                Element fittingToConnect = elementsToConnectAfter.FirstOrDefault(e => !(e is Pipe)) ?? elementsToConnectAfter[0];
                                Logger.LogInfo($"Connecting to fitting: {fittingToConnect.Name} (ID: {fittingToConnect.Id})");
                                
                                Connector fittingConnector = ConnectorHelper.GetAvailableConnector(fittingToConnect);
                                if (fittingConnector != null)
                                {
                                    // Lấy connector gần nhất của pipe A để kết nối với fitting
                                    Connector pipeConnector = PipeHelper.GetNearestConnector(selectedPipe, fittingConnector);
                                    if (pipeConnector != null)
                                    {
                                        Logger.LogInfo("Attempting direct connection using ConnectTo");
                                        bool connectSuccess = ConnectorHelper.ConnectConnectors(pipeConnector, fittingConnector);
                                        Logger.LogInfo($"Direct connection to fitting result: {connectSuccess}");
                                    }
                                    else
                                    {
                                        Logger.LogWarning("Could not find suitable pipe connector for fitting connection");
                                    }
                                }
                            }
                            else
                            {
                                Logger.LogInfo("No fittings to connect - pipe B only connected to pipe A, proceeding with deletion");
                            }
                            
                            // Xóa target pipe B trong mọi trường hợp
                            foreach (Element elementToDelete in elementsToDelete)
                            {
                                Logger.LogInfo($"Deleting target pipe: {elementToDelete.Name} (ID: {elementToDelete.Id})");
                                doc.Delete(elementToDelete.Id);
                            }
                            Logger.LogInfo("Target pipe deletion completed");
                        }
                        
                        trans.Commit();
                        Logger.LogInfo("Pipe endpoint updated successfully, transaction committed");
                        Logger.LogMethodExit(nameof(Execute), Result.Succeeded);
                        return Result.Succeeded;
                    }
                    else
                    {
                        trans.RollBack();
                        Logger.LogError("Failed to update pipe endpoint, transaction rolled back");
                        Logger.LogMethodExit(nameof(Execute), Result.Failed);
                        return Result.Failed;
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException ex)
            {
                Logger.LogWarning($"Operation cancelled by user: {ex.Message}");
                Logger.LogMethodExit(nameof(Execute), Result.Cancelled);
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Unexpected error in Execute method");
                Logger.LogMethodExit(nameof(Execute), Result.Failed);
                return Result.Failed;
            }
            finally
            {
                // Cleanup old logs
                Logger.CleanupOldLogs();
            }
        }
    }
}