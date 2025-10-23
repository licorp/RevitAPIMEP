using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Quoc_MEP
{
    public static class SelectionHelper
    {
        /// <summary>
        /// Cho phép user chọn một ống trong model
        /// </summary>
        /// <param name="uiDoc">UIDocument hiện tại</param>
        /// <returns>Pipe đã chọn hoặc null nếu không chọn được</returns>
        public static Pipe SelectPipe(UIDocument uiDoc)
        {
            Logger.LogMethodEntry(nameof(SelectPipe));
            
            try
            {
                Logger.LogDebug("Creating pipe selection filter");
                // Tạo filter để chỉ cho phép chọn Pipe
                PipeSelectionFilter pipeFilter = new PipeSelectionFilter();
                
                Logger.LogDebug("Waiting for user to select pipe");
                // Cho phép user chọn một element
                Reference pickedRef = uiDoc.Selection.PickObject(ObjectType.Element, pipeFilter, "Chọn ống cần cập nhật endpoint");
                
                if (pickedRef != null)
                {
                    Element selectedElement = uiDoc.Document.GetElement(pickedRef);
                    Pipe pipe = selectedElement as Pipe;
                    Logger.LogInfo($"User selected pipe: ID={pipe?.Id}, Name={pipe?.Name}");
                    Logger.LogMethodExit(nameof(SelectPipe), pipe?.Id);
                    return pipe;
                }
                
                Logger.LogWarning("No pipe reference returned from selection");
                Logger.LogMethodExit(nameof(SelectPipe), "null");
                return null;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException ex)
            {
                Logger.LogWarning($"User cancelled pipe selection: {ex.Message}");
                // User đã hủy selection
                Logger.LogMethodExit(nameof(SelectPipe), "cancelled");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error in SelectPipe method");
                Logger.LogMethodExit(nameof(SelectPipe), "error");
                return null;
            }
        }

        /// <summary>
        /// Cho phép user chọn một đối tượng MEP (pipe, fitting, equipment) có connector
        /// </summary>
        /// <param name="uiDoc">UIDocument hiện tại</param>
        /// <returns>Element đã chọn hoặc null nếu không chọn được</returns>
        public static Element SelectMEPElement(UIDocument uiDoc)
        {
            Logger.LogMethodEntry(nameof(SelectMEPElement));
            
            try
            {
                Logger.LogDebug("Creating MEP element selection filter");
                // Tạo filter để chỉ cho phép chọn MEP elements có connector
                MEPElementSelectionFilter mepFilter = new MEPElementSelectionFilter();
                
                Logger.LogDebug("Waiting for user to select MEP element");
                // Cho phép user chọn một element
                Reference pickedRef = uiDoc.Selection.PickObject(ObjectType.Element, mepFilter, "Chọn đối tượng có connector để làm endpoint");
                
                if (pickedRef != null)
                {
                    Element selectedElement = uiDoc.Document.GetElement(pickedRef);
                    Logger.LogInfo($"User selected MEP element: ID={selectedElement?.Id}, Name={selectedElement?.Name}, Category={selectedElement?.Category?.Name}");
                    Logger.LogMethodExit(nameof(SelectMEPElement), selectedElement?.Id);
                    return selectedElement;
                }
                
                Logger.LogWarning("No MEP element reference returned from selection");
                Logger.LogMethodExit(nameof(SelectMEPElement), "null");
                return null;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException ex)
            {
                Logger.LogWarning($"User cancelled MEP element selection: {ex.Message}");
                // User đã hủy selection
                Logger.LogMethodExit(nameof(SelectMEPElement), "cancelled");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error in SelectMEPElement method");
                Logger.LogMethodExit(nameof(SelectMEPElement), "error");
                return null;
            }
        }
    }

    /// <summary>
    /// Selection filter để chỉ cho phép chọn Pipe
    /// </summary>
    public class PipeSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Pipe;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }

    /// <summary>
    /// Selection filter để chỉ cho phép chọn các MEP elements có connector
    /// </summary>
    public class MEPElementSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            // Cho phép chọn các loại element sau:
            // - Pipe
            // - FamilyInstance (fittings, equipment)
            // - Duct
            // - FlexPipe, FlexDuct
            
            if (elem is Pipe ||
                elem is Duct ||
                elem is FlexPipe ||
                elem is FlexDuct)
            {
                return true;
            }

            // Kiểm tra FamilyInstance có phải là MEP element có connector không
            if (elem is FamilyInstance familyInstance)
            {
                // Kiểm tra category
                Category category = familyInstance.Category;
                if (category != null)
                {
                    BuiltInCategory builtInCategory = (BuiltInCategory)category.Id.IntegerValue;
                    
                    // Các category MEP thường có connector
                    return builtInCategory == BuiltInCategory.OST_PipeFitting ||
                           builtInCategory == BuiltInCategory.OST_PipeAccessory ||
                           builtInCategory == BuiltInCategory.OST_PlumbingFixtures ||
                           builtInCategory == BuiltInCategory.OST_MechanicalEquipment ||
                           builtInCategory == BuiltInCategory.OST_DuctFitting ||
                           builtInCategory == BuiltInCategory.OST_DuctAccessory ||
                           builtInCategory == BuiltInCategory.OST_DuctTerminal;
                }
            }

            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}