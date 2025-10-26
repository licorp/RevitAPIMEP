using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Threading;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Quoc_MEP
{
    /// <summary>
    /// External Command class for the Rotate Elements functionality
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RotateElementsCommand : IExternalCommand
    {
        private static RotateWindow _rotateWindow;
        private static ExternalEvent _rotationEvent;
        private static ImprovedRotationEventHandler _rotationHandler;
        private static readonly object _windowLock = new object();
        private static bool _isProcessing = false;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                Trace.WriteLine("=== RotateElementsCommand Started ===");

                // ===== CHECK: Nếu gọi từ Panel =====
                if (PanelDataBridge.IsCalledFromPanel && PanelDataBridge.RotateAngleValue.HasValue)
                {
                    Trace.WriteLine($"Called from Panel with angle: {PanelDataBridge.RotateAngleValue.Value}°");
                    
                    // Thực thi trực tiếp KHÔNG hiển thị form
                    double angleDegrees = PanelDataBridge.RotateAngleValue.Value;
                    UIDocument uidoc = uiApp.ActiveUIDocument;
                    Document doc = uidoc.Document;
                    var selectedIds = uidoc.Selection.GetElementIds();
                    
                    if (selectedIds.Count == 0)
                    {
                        TaskDialog.Show("Warning", "Please select elements to rotate!");
                        PanelDataBridge.Reset();
                        return Result.Cancelled;
                    }
                    
                    // Thực thi rotate
                    double angleRadians = angleDegrees * Math.PI / 180.0;
                    int successCount = 0;
                    int skipCount = 0;
                    
                    using (Transaction trans = new Transaction(doc, "Rotate from Panel"))
                    {
                        trans.Start();
                        
                        foreach (ElementId id in selectedIds)
                        {
                            Element elem = doc.GetElement(id);
                            Location location = elem?.Location;
                            
                            if (location is LocationPoint locPoint)
                            {
                                XYZ point = locPoint.Point;
                                Line axis = Line.CreateBound(point, point + XYZ.BasisZ);
                                locPoint.Rotate(axis, angleRadians);
                                successCount++;
                            }
                            else if (location is LocationCurve locCurve)
                            {
                                Curve curve = locCurve.Curve;
                                XYZ midPoint = (curve.GetEndPoint(0) + curve.GetEndPoint(1)) / 2.0;
                                Line axis = Line.CreateBound(midPoint, midPoint + XYZ.BasisZ);
                                locCurve.Rotate(axis, angleRadians);
                                successCount++;
                            }
                            else skipCount++;
                        }
                        
                        trans.Commit();
                    }
                    
                    Trace.WriteLine($"Panel execution: {successCount} success, {skipCount} skipped");
                    
                    string msg = $"Rotated {successCount} element(s) by {angleDegrees}°";
                    if (skipCount > 0) msg += $"\n{skipCount} skipped.";
                    TaskDialog.Show("Success", msg);
                    
                    // Reset Bridge
                    PanelDataBridge.Reset();
                    
                    return Result.Succeeded;
                }

                // ===== Gọi từ Ribbon: Hiển thị Form =====
                // Initialize external event on first run
                if (_rotationEvent == null)
                {
                    _rotationHandler = new ImprovedRotationEventHandler();
                    _rotationEvent = ExternalEvent.Create(_rotationHandler);
                }

                // Show reusable form
                ShowReusableForm(uiApp);

                Trace.WriteLine("=== RotateElementsCommand Form Shown ===");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error in RotateElementsCommand: {ex.Message}");
                message = ex.Message;
                PanelDataBridge.Reset(); // Đảm bảo reset khi có lỗi
                return Result.Failed;
            }
        }

        private static void ShowReusableForm(UIApplication uiApp)
        {
            lock (_windowLock)
            {
                if (_rotateWindow == null || !_rotateWindow.IsLoaded)
                {
                    // Create new form directly in UI thread - NO STA Thread needed
                    _rotateWindow = new RotateWindow(uiApp);
                    
                    // Subscribe to rotation request
                    _rotateWindow.RotationRequested += (sender, e) =>
                    {
                        if (!_isProcessing)
                        {
                            _isProcessing = true;
                            _rotationHandler.SetRequest(e.Angle, _rotateWindow);
                            _rotationEvent.Raise();
                        }
                    };
                    
                    // Handle window closing
                    _rotateWindow.Closed += (sender, e) =>
                    {
                        _rotateWindow = null;
                    };
                    
                    // Show form modeless (non-blocking)
                    _rotateWindow.Show();
                }
                else
                {
                    // Reuse existing form
                    _rotateWindow.ShowForReuse();
                }
            }
        }

        public static void CompleteRotation()
        {
            _isProcessing = false;
        }

        private Element SelectRotationAxis(UIDocument uidoc)
        {
            try
            {
                // T?o selection filter ch? cho ph�p ch?n pipes v� pipe fittings
                ISelectionFilter filter = new PipeAndFittingSelectionFilter();
                
                // Y�u c?u user ch?n element l�m tr?c xoay
                Reference reference = uidoc.Selection.PickObject(ObjectType.Element, filter, 
                    "Select a pipe or fitting as rotation axis / Ch?n ?ng ho?c ph? ki?n l�m tr?c xoay:");
                
                if (reference != null)
                {
                    Element element = uidoc.Document.GetElement(reference);
                    Trace.WriteLine($"Selected rotation axis element: {element.Id}, Category: {element.Category?.Name}");
                    return element;
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                Trace.WriteLine("User cancelled rotation axis selection");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error in SelectRotationAxis: {ex.Message}");
            }
            
            return null;
        }

        private Line GetRotationAxisFromElement(Element element)
        {
            try
            {
                // L?y location curve c?a element (cho pipes)
                if (element.Location is LocationCurve locationCurve)
                {
                    Curve curve = locationCurve.Curve;
                    if (curve is Line line)
                    {
                        return line;
                    }
                    else
                    {
                        // N?u kh�ng ph?i line, t?o line t? start v� end point
                        XYZ startPoint = curve.GetEndPoint(0);
                        XYZ endPoint = curve.GetEndPoint(1);
                        return Line.CreateBound(startPoint, endPoint);
                    }
                }
                // N?u l� fitting ho?c kh�ng c� LocationCurve, t?o tr?c vertical qua center
                else if (element.Location is LocationPoint locationPoint)
                {
                    XYZ center = locationPoint.Point;
                    XYZ axisDirection = XYZ.BasisZ; // Tr?c Z (vertical)
                    XYZ startPoint = center - axisDirection * 10; // 10 feet xu?ng
                    XYZ endPoint = center + axisDirection * 10;   // 10 feet l�n
                    return Line.CreateBound(startPoint, endPoint);
                }
                
                // Backup: d�ng bounding box center v?i tr?c Z
                BoundingBoxXYZ bbox = element.get_BoundingBox(null);
                if (bbox != null)
                {
                    XYZ center = (bbox.Min + bbox.Max) / 2;
                    XYZ axisDirection = XYZ.BasisZ;
                    XYZ startPoint = center - axisDirection * 10;
                    XYZ endPoint = center + axisDirection * 10;
                    return Line.CreateBound(startPoint, endPoint);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error in GetRotationAxisFromElement: {ex.Message}");
            }
            
            return null;
        }

        private void RotateElementAroundAxis(Document doc, Element element, Line rotationAxis, double angleInRadians)
        {
            try
            {
                // Th?c hi?n xoay element quanh tr?c d� ch?n
                ElementTransformUtils.RotateElement(doc, element.Id, rotationAxis, angleInRadians);
                
                Trace.WriteLine($"Rotated element {element.Id} by {angleInRadians} radians around custom axis");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to rotate element {element.Id}: {ex.Message}");
            }
        }

        // Selection Filter class d? ch? cho ph�p ch?n pipes v� fittings
        public class PipeAndFittingSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                // Cho ph�p pipes, pipe fittings, ducts, duct fittings
                if (elem.Category != null)
                {
                    string categoryName = elem.Category.Name;
                    return categoryName == "Pipes" || 
                           categoryName == "Pipe Fittings" || 
                           categoryName == "Ducts" || 
                           categoryName == "Duct Fittings" ||
                           categoryName == "Pipe Accessories" ||
                           categoryName == "Duct Accessories";
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }
    }

    // External Event Handler for thread-safe rotation processing
    public class RotationEventHandler : IExternalEventHandler
    {
        private double _rotationAngle;
        private RotateWindow _window;

        public void SetRequest(double angle, RotateWindow window)
        {
            _rotationAngle = angle;
            _window = window;
        }

        public void Execute(UIApplication uiapp)
        {
            try
            {
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Check selected elements
                ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
                
                if (selectedIds.Count == 0)
                {
                    TaskDialog.Show("Warning", "Please select elements to rotate first.\nVui l�ng ch?n c�c d?i tu?ng c?n xoay tru?c.");
                    _window?.Dispatcher.Invoke(() => _window.CompleteProcessing());
                    return;
                }

                Trace.WriteLine($"Selected {selectedIds.Count} elements to rotate");

                // Select rotation axis
                Element rotationAxisElement = SelectRotationAxis(uidoc);
                if (rotationAxisElement == null)
                {
                    TaskDialog.Show("Warning", "Please select a pipe or fitting as rotation axis.\nVui l�ng ch?n ?ng ho?c ph? ki?n l�m tr?c xoay.");
                    _window?.Dispatcher.Invoke(() => _window.CompleteProcessing());
                    return;
                }

                // Get rotation axis
                Line rotationAxis = GetRotationAxisFromElement(rotationAxisElement);
                if (rotationAxis == null)
                {
                    TaskDialog.Show("Error", "Unable to determine rotation axis from selected element.\nKh�ng th? x�c d?nh tr?c xoay t? element d� ch?n.");
                    _window?.Dispatcher.Invoke(() => _window.CompleteProcessing());
                    return;
                }

                Trace.WriteLine($"Rotation axis determined from element {rotationAxisElement.Id}");

                // Convert angle to radians
                double angleInRadians = _rotationAngle * Math.PI / 180.0;

                // Perform rotation
                using (Transaction trans = new Transaction(doc, "Rotate Elements"))
                {
                    trans.Start();
                    
                    foreach (ElementId elementId in selectedIds)
                    {
                        Element element = doc.GetElement(elementId);
                        if (element != null && element.Id != rotationAxisElement.Id)
                        {
                            RotateElementAroundAxis(doc, element, rotationAxis, angleInRadians);
                        }
                    }
                    
                    trans.Commit();
                }

                // Confirm rotation direction
                bool isCorrectDirection = ConfirmRotationDirectionStatic(selectedIds.Count, _rotationAngle);
                
                if (!isCorrectDirection)
                {
                    using (Transaction reverseTransac = new Transaction(doc, "Reverse Rotation Direction"))
                    {
                        reverseTransac.Start();
                        
                        foreach (ElementId elementId in selectedIds)
                        {
                            Element element = doc.GetElement(elementId);
                            if (element != null && element.Id != rotationAxisElement.Id)
                            {
                                RotateElementAroundAxis(doc, element, rotationAxis, -2 * angleInRadians);
                            }
                        }
                        
                        reverseTransac.Commit();
                    }
                    
                    Trace.WriteLine($"User chose to reverse rotation. Applied additional {-2 * _rotationAngle}� rotation");
                }

                Trace.WriteLine("=== Rotation Process Completed Successfully ===");
                
                // Clear selection and history to prevent "Elements need to be disconnected" warning
                try
                {
                    // Clear current selection
                    UIDocument uiDocument = new UIDocument(doc);
                    uiDocument.Selection.SetElementIds(new List<ElementId>());
                    
                    // Clear any temporary graphics or highlights
                    uiDocument.RefreshActiveView();
                    
                    Trace.WriteLine("Cleared selection and refreshed view to prevent disconnection warnings");
                }
                catch (Exception clearEx)
                {
                    Trace.WriteLine($"Warning: Could not clear selection - {clearEx.Message}");
                }
                
                // Complete and show form for reuse
                _window?.Dispatcher.Invoke(() => _window.CompleteProcessing());
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error in RotationEventHandler: {ex.Message}");
                TaskDialog.Show("Error", $"Error during rotation: {ex.Message}");
                _window?.Dispatcher.Invoke(() => _window.CompleteProcessing());
            }
        }

        public string GetName()
        {
            return "RotationEventHandler";
        }

        private Element SelectRotationAxis(UIDocument uidoc)
        {
            try
            {
                Reference reference = uidoc.Selection.PickObject(ObjectType.Element, 
                    new RotateElementsCommand.PipeAndFittingSelectionFilter(), 
                    "Select a pipe or fitting as rotation axis / Ch?n ?ng ho?c ph? ki?n l�m tr?c xoay");
                
                return uidoc.Document.GetElement(reference.ElementId);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error in SelectRotationAxis: {ex.Message}");
                return null;
            }
        }

        private Line GetRotationAxisFromElement(Element element)
        {
            try
            {
                if (element.Location is LocationCurve locationCurve)
                {
                    return locationCurve.Curve as Line;
                }
                
                Trace.WriteLine($"Element {element.Id} does not have a LocationCurve");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error in GetRotationAxisFromElement: {ex.Message}");
            }
            
            return null;
        }

        private void RotateElementAroundAxis(Document doc, Element element, Line rotationAxis, double angleInRadians)
        {
            try
            {
                ElementTransformUtils.RotateElement(doc, element.Id, rotationAxis, angleInRadians);
                Trace.WriteLine($"Rotated element {element.Id} by {angleInRadians} radians around custom axis");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to rotate element {element.Id}: {ex.Message}");
            }
        }

        private bool ConfirmRotationDirection(int elementCount, double angle)
        {
            string message = $"Rotated {elementCount} elements by {angle}�.\nDi chuy?n c� d�ng hu?ng kh�ng?\nIs the rotation direction correct?";
            
            TaskDialogResult result = TaskDialog.Show("Confirm Rotation Direction", 
                message, 
                TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);
            
            return result == TaskDialogResult.Yes;
        }

        private static bool ConfirmRotationDirectionStatic(int elementCount, double angle)
        {
            // Static method for use in ExternalEvent handler
            TaskDialog dialog = new TaskDialog("Rotation Confirmation");
            dialog.MainInstruction = $"Rotated {elementCount} elements by {angle}�";
            dialog.MainContent = "Di chuy?n c� d�ng hu?ng kh�ng?\nIs the rotation direction correct?";
            dialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;
            dialog.DefaultButton = TaskDialogResult.Yes;
            
            TaskDialogResult result = dialog.Show();
            
            Trace.WriteLine($"User confirmation result: {result}");
            return result == TaskDialogResult.Yes;
        }
    }
}
