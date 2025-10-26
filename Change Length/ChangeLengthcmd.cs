using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace Quoc_MEP
{
    /// <summary>
    /// Lệnh thay đổi chiều dài ống/ống gió
    /// Change pipe/duct length command
    /// Version: 1.0
    /// Author: Quoc.Nguyen
    /// Date: 19.04.2025
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ChangeLengthcmd : IExternalCommand
    {
        // Hằng số chuyển đổi / Conversion constants
        private const double MM_TO_FEET = 304.8;
        private const double MIN_LENGTH_MM = 1;
        private const double MAX_LENGTH_MM = 100000;

        private static PipeLengthWindow _window;
        private static ExternalEvent _lengthChangeEvent;
        private static LengthChangeEventHandler _eventHandler;
        private static bool _traceListenerAdded = false;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Thêm OutputDebugString listener để xuất ra DebugView
            if (!_traceListenerAdded)
            {
                Trace.Listeners.Add(new DefaultTraceListener());
                _traceListenerAdded = true;
            }

            ChangeLengthLogger.StartOperation("Execute");
            
            try
            {
                // Get UIDocument và Document
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;
                UIApplication uiApp = commandData.Application;

                ChangeLengthLogger.Info($"UIApp: {uiApp != null}, UIDoc: {uidoc != null}, Doc: {doc != null}");

                // ===== CHECK: Nếu gọi từ Panel =====
                if (PanelDataBridge.IsCalledFromPanel && PanelDataBridge.ChangeLengthValue.HasValue)
                {
                    ChangeLengthLogger.Info($"Called from Panel with length: {PanelDataBridge.ChangeLengthValue.Value} mm");
                    
                    // Thực thi trực tiếp KHÔNG hiển thị form
                    double lengthMm = PanelDataBridge.ChangeLengthValue.Value;
                    var selectedIds = uidoc.Selection.GetElementIds();
                    
                    if (selectedIds.Count == 0)
                    {
                        TaskDialog.Show("Warning", "Please select pipe or duct elements!");
                        PanelDataBridge.Reset();
                        return Result.Cancelled;
                    }
                    
                    // Thực thi change length
                    double lengthFeet = lengthMm / 304.8;
                    int successCount = 0;
                    int skipCount = 0;
                    
                    using (Transaction trans = new Transaction(doc, "Change Length from Panel"))
                    {
                        trans.Start();
                        
                        foreach (ElementId id in selectedIds)
                        {
                            Element elem = doc.GetElement(id);
                            
                            if (elem is Autodesk.Revit.DB.Plumbing.Pipe pipe)
                            {
                                Parameter lengthParam = pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                                if (lengthParam != null && !lengthParam.IsReadOnly)
                                {
                                    lengthParam.Set(lengthFeet);
                                    successCount++;
                                }
                                else skipCount++;
                            }
                            else if (elem is Autodesk.Revit.DB.Mechanical.Duct duct)
                            {
                                Parameter lengthParam = duct.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                                if (lengthParam != null && !lengthParam.IsReadOnly)
                                {
                                    lengthParam.Set(lengthFeet);
                                    successCount++;
                                }
                                else skipCount++;
                            }
                            else skipCount++;
                        }
                        
                        trans.Commit();
                    }
                    
                    ChangeLengthLogger.Info($"Panel execution: {successCount} success, {skipCount} skipped");
                    
                    string msg = $"Changed {successCount} element(s) to {lengthMm}mm";
                    if (skipCount > 0) msg += $"\n{skipCount} skipped.";
                    TaskDialog.Show("Success", msg);
                    
                    // Reset Bridge
                    PanelDataBridge.Reset();
                    
                    ChangeLengthLogger.EndOperation("Execute");
                    return Result.Succeeded;
                }
                
                // ===== Gọi từ Ribbon: Hiển thị Form =====
                // Khởi tạo window và event handler nếu chưa có (singleton pattern)
                // Initialize window and event handler if not exists (singleton pattern)
                if (_window == null)
                {
                    ChangeLengthLogger.Info("Creating new PipeLengthWindow and ExternalEvent");
                    _eventHandler = new LengthChangeEventHandler();
                    _lengthChangeEvent = ExternalEvent.Create(_eventHandler);
                    _window = new PipeLengthWindow(uiApp);
                    
                    // Subscribe to length change event
                    _window.LengthChangeRequested += (sender, e) =>
                    {
                        ChangeLengthLogger.Info($"LengthChangeRequested: {e.Length} mm");
                        _eventHandler.LengthMm = e.Length;
                        _eventHandler.UIDoc = uidoc;
                        _eventHandler.ParentWindow = _window; // Pass window reference
                        _lengthChangeEvent.Raise();
                    };
                }
                else
                {
                    // Reset state khi mở lại window
                    ChangeLengthLogger.Info("Resetting window state for reuse");
                    _window.ResetState();
                }

                // Hiển thị window (không block)
                // Show window (non-blocking)
                ChangeLengthLogger.Info("Showing PipeLengthWindow");
                _window.Show();
                _window.Activate();

                ChangeLengthLogger.EndOperation("Execute");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                PanelDataBridge.Reset(); // Đảm bảo reset khi có lỗi
                return Result.Failed;
            }
        }

        /// <summary>
        /// Lấy ConnectorSet từ Pipe hoặc Duct
        /// Get ConnectorSet from Pipe or Duct
        /// </summary>
        private ConnectorSet GetConnectors(Element element)
        {
            if (element is Pipe pipe)
            {
                return pipe.ConnectorManager?.Connectors;
            }
            else if (element is Duct duct)
            {
                return duct.ConnectorManager?.Connectors;
            }
            return null;
        }

        /// <summary>
        /// Tìm connector gần nhất với điểm cho trước
        /// Find closest connector to given point
        /// </summary>
        private Connector FindClosestConnector(ConnectorSet connectors, XYZ targetPoint)
        {
            if (connectors == null) return null;

            Connector closestConnector = null;
            double minDistance = double.MaxValue;

            foreach (Connector conn in connectors)
            {
                double distance = conn.Origin.DistanceTo(targetPoint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestConnector = conn;
                }
            }

            return closestConnector;
        }

        /// <summary>
        /// Thu thập tất cả các element kết nối với connector
        /// Collect all elements connected to connector
        /// </summary>
        private List<Element> CollectConnectedElements(Connector connector, HashSet<int> processedIds = null)
        {
            if (processedIds == null)
            {
                processedIds = new HashSet<int>();
            }

            List<Element> result = new List<Element>();
            int ownerId = connector.Owner.Id.IntegerValue;

            if (processedIds.Contains(ownerId))
            {
                return result;
            }

            processedIds.Add(ownerId);

            foreach (Connector refConnector in connector.AllRefs)
            {
                Element refOwner = refConnector.Owner;
                int refOwnerId = refOwner.Id.IntegerValue;

                if (refOwnerId == ownerId || processedIds.Contains(refOwnerId))
                {
                    continue;
                }

                result.Add(refOwner);
                processedIds.Add(refOwnerId);

                // Nếu là fitting, tiếp tục thu thập các element kết nối
                // If it's a fitting, continue collecting connected elements
                if (!(refOwner is Pipe) && !(refOwner is Duct))
                {
                    ConnectorSet refConnectors = GetConnectors(refOwner);
                    if (refConnectors != null)
                    {
                        foreach (Connector otherConn in refConnectors)
                        {
                            if (otherConn.Id != refConnector.Id)
                            {
                                result.AddRange(CollectConnectedElements(otherConn, processedIds));
                            }
                        }
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    /// External Event Handler để thực hiện thay đổi chiều dài
    /// External Event Handler to perform length change
    /// </summary>
    public class LengthChangeEventHandler : IExternalEventHandler
    {
        public UIDocument UIDoc { get; set; }
        public double LengthMm { get; set; }
        public PipeLengthWindow ParentWindow { get; set; }
        
        /// <summary>
        /// Callback được gọi khi operation hoàn thành (success hoặc cancelled)
        /// Parameters: (bool success, string message)
        /// </summary>
        public Action<bool, string> OnCompleted { get; set; }

        private const double MM_TO_FEET = 304.8;

        public void Execute(UIApplication app)
        {
            ChangeLengthLogger.StartOperation($"EventHandler.Execute - Length: {LengthMm}mm");

            if (UIDoc == null || UIDoc.Document == null)
            {
                ChangeLengthLogger.Error("UIDoc or Document is null");
                MessageBox.Show("Lỗi: Document không hợp lệ\nError: Invalid Document", "Lỗi / Error");
                return;
            }

            Document doc = UIDoc.Document;
            double lengthFt = LengthMm / MM_TO_FEET;

            try
            {
                // Lấy tất cả pipes và ducts trong view
                ChangeLengthLogger.Info("Collecting all pipes and ducts in view");
                FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id);
                
                List<Element> allPipes = collector.OfClass(typeof(Pipe)).ToList();
                List<Element> allDucts = collector.OfClass(typeof(Duct)).ToList();
                List<Element> allElements = new List<Element>();
                allElements.AddRange(allPipes);
                allElements.AddRange(allDucts);
                
                ChangeLengthLogger.Info($"Found {allPipes.Count} pipes and {allDucts.Count} ducts");

                if (allElements.Count == 0)
                {
                    ChangeLengthLogger.Warning("No pipes or ducts found in active view");
                    TaskDialog.Show("Thông báo / Notice", 
                        "Không tìm thấy ống hoặc ống gió trong view.\nNo pipes or ducts found in view.");
                    return;
                }

                // Pick point gần ống - ĐIỂM NÀY SẼ LÀ HƯỚNG DI CHUYỂN (END POINT MỚI)
                ChangeLengthLogger.Info("Prompting user to pick point near pipe");
                XYZ pickPoint = UIDoc.Selection.PickPoint("Click vào ống tại hướng muốn kéo dài (điểm này là end point mới) / Click on pipe direction to extend (this will be the new end point)");
                ChangeLengthLogger.Info($"Picked point: {pickPoint}");

                // Tìm ống gần nhất và connector gần nhất
                Element nearestElement = null;
                Connector nearestConnector = null;
                double minDistance = double.MaxValue;

                foreach (Element elem in allElements)
                {
                    Connector closestConn = GetNearestConnector(elem, pickPoint);
                    if (closestConn != null)
                    {
                        // Chiếu pickPoint lên cùng độ cao với connector
                        XYZ projectedPoint = new XYZ(pickPoint.X, pickPoint.Y, closestConn.Origin.Z);
                        double distance = projectedPoint.DistanceTo(closestConn.Origin);
                        
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestElement = elem;
                            nearestConnector = closestConn;
                        }
                    }
                }

                if (nearestElement == null || nearestConnector == null)
                {
                    ChangeLengthLogger.Error("Could not find nearest pipe/duct or connector");
                    TaskDialog.Show("Lỗi / Error", 
                        "Không tìm thấy ống gần điểm pick.\nCould not find pipe near picked point.");
                    return;
                }

                ChangeLengthLogger.Info($"Nearest element: {nearestElement.Name} (ID: {nearestElement.Id})");
                ChangeLengthLogger.Info($"Nearest connector at: {nearestConnector.Origin}");

                // Lấy LocationCurve
                LocationCurve locationCurve = nearestElement.Location as LocationCurve;
                if (locationCurve == null || !(locationCurve.Curve is Line))
                {
                    ChangeLengthLogger.Error("Element does not have valid LocationCurve or not a straight line");
                    TaskDialog.Show("Lỗi / Error",
                        "Chỉ hỗ trợ đường thẳng.\nOnly straight lines are supported.");
                    return;
                }

                Line originalLine = locationCurve.Curve as Line;
                XYZ startPoint = originalLine.GetEndPoint(0);
                XYZ endPoint = originalLine.GetEndPoint(1);
                XYZ direction = (endPoint - startPoint).Normalize();

                ChangeLengthLogger.Info($"Original line: Start={startPoint}, End={endPoint}");

                // Xác định điểm di chuyển dựa trên connector gần pickPoint nhất
                // The connector nearest to pickPoint will be the moving direction
                bool moveStartPoint = startPoint.DistanceTo(nearestConnector.Origin) < endPoint.DistanceTo(nearestConnector.Origin);
                
                ChangeLengthLogger.Info($"Move decision: moveStartPoint={moveStartPoint}");

                // Start Transaction
                ChangeLengthLogger.Info("Starting transaction");
                using (Transaction trans = new Transaction(doc, "Thay đổi chiều dài ống / Change Pipe Length"))
                {
                    trans.Start();

                    try
                    {
                        XYZ newStartPoint, newEndPoint;
                        XYZ oldMovingPoint, newMovingPoint;

                        if (moveStartPoint)
                        {
                            // Di chuyển start, cố định end
                            newEndPoint = endPoint;
                            newStartPoint = newEndPoint - direction * lengthFt;
                            oldMovingPoint = startPoint;
                            newMovingPoint = newStartPoint;
                        }
                        else
                        {
                            // Di chuyển end, cố định start
                            newStartPoint = startPoint;
                            newEndPoint = newStartPoint + direction * lengthFt;
                            oldMovingPoint = endPoint;
                            newMovingPoint = newEndPoint;
                        }

                        ChangeLengthLogger.Info($"New line: Start={newStartPoint}, End={newEndPoint}");

                        // Lấy connector tại điểm di chuyển trước khi thay đổi
                        ConnectorSet connectors = null;
                        if (nearestElement is Pipe pipe)
                        {
                            connectors = pipe.ConnectorManager?.Connectors;
                        }
                        else if (nearestElement is Duct duct)
                        {
                            connectors = duct.ConnectorManager?.Connectors;
                        }

                        Connector movingConnector = null;
                        if (connectors != null)
                        {
                            foreach (Connector conn in connectors)
                            {
                                if (conn.Origin.DistanceTo(oldMovingPoint) < 0.01) // tolerance
                                {
                                    movingConnector = conn;
                                    break;
                                }
                            }
                        }

                        // Thu thập elements kết nối với điểm di chuyển
                        List<ElementId> connectedElementIds = new List<ElementId>();
                        if (movingConnector != null && movingConnector.IsConnected)
                        {
                            ChangeLengthLogger.Info("Collecting connected elements");
                            foreach (Connector refConn in movingConnector.AllRefs)
                            {
                                if (refConn.Owner.Id != nearestElement.Id)
                                {
                                    connectedElementIds.Add(refConn.Owner.Id);
                                    ChangeLengthLogger.Info($"Found connected element: {refConn.Owner.Name} (ID: {refConn.Owner.Id})");
                                }
                            }
                        }

                        // Thay đổi chiều dài ống
                        Line newLine = Line.CreateBound(newStartPoint, newEndPoint);
                        locationCurve.Curve = newLine;
                        ChangeLengthLogger.Info("Pipe length changed");

                        // Di chuyển các elements kết nối
                        if (connectedElementIds.Count > 0)
                        {
                            XYZ translationVector = newMovingPoint - oldMovingPoint;
                            ChangeLengthLogger.Info($"Moving {connectedElementIds.Count} connected elements by vector: {translationVector}");
                            ElementTransformUtils.MoveElements(doc, connectedElementIds, translationVector);
                        }

                        trans.Commit();
                        ChangeLengthLogger.Info("Transaction committed successfully");
                        
                        // Notify success
                        OnCompleted?.Invoke(true, $"✓ Changed length to {LengthMm}mm successfully!");


                    }
                    catch (Exception ex)
                    {
                        ChangeLengthLogger.Error("Error in transaction", ex);
                        trans.RollBack();
                        OnCompleted?.Invoke(false, $"✗ Transaction failed: {ex.Message}");
                        throw;
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                ChangeLengthLogger.Info("User cancelled pick operation");
                OnCompleted?.Invoke(false, "⚠ Operation cancelled by user");
            }
            catch (Exception ex)
            {
                ChangeLengthLogger.Error("Unexpected error", ex);
                MessageBox.Show(
                    $"Lỗi: {ex.Message}\nError: {ex.Message}",
                    "Lỗi / Error");
                OnCompleted?.Invoke(false, $"✗ Error: {ex.Message}");
            }
            finally
            {
                // Reset window state và show lại để có thể sử dụng lại
                if (ParentWindow != null)
                {
                    ParentWindow.Dispatcher.Invoke(() =>
                    {
                        ParentWindow.ResetState();
                        ParentWindow.Show();
                        ParentWindow.Activate();
                        ChangeLengthLogger.Info("Window reset and shown for next use");
                    });
                }
                
                ChangeLengthLogger.EndOperation("EventHandler.Execute");
            }
        }

        /// <summary>
        /// Tìm connector gần nhất đến điểm pickpoint
        /// Find nearest connector to pickpoint
        /// </summary>
        private Connector GetNearestConnector(Element element, XYZ pickPoint)
        {
            ConnectorSet connectors = null;

            if (element is Pipe pipe)
            {
                connectors = pipe.ConnectorManager?.Connectors;
            }
            else if (element is Duct duct)
            {
                connectors = duct.ConnectorManager?.Connectors;
            }

            if (connectors == null || connectors.Size == 0)
            {
                return null;
            }

            Connector nearestConnector = null;
            double minDistance = double.MaxValue;

            foreach (Connector conn in connectors)
            {
                double distance = conn.Origin.DistanceTo(pickPoint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestConnector = conn;
                }
            }

            return nearestConnector;
        }

        public string GetName()
        {
            return "LengthChangeEventHandler";
        }
    }

    /// <summary>
    /// Filter để chỉ cho phép chọn Pipe hoặc Duct
    /// Filter to only allow selecting Pipe or Duct
    /// </summary>
    public class PipeOrDuctSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Pipe || elem is Duct;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
