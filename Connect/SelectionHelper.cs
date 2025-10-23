using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI.Selection;


namespace Quoc_MEP
{
    /// <summary>
    /// Helper class để xử lý việc chọn elements
    /// </summary>
    public static class SelectionHelper
    {
        /// <summary>
        /// Selection filter cho MEP family instances
        /// </summary>
        public class MEPFamilySelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                // Chấp nhận MEP curves (ducts, pipes, cable trays, conduits)
                if (elem is MEPCurve)
                    return true;

                // Chấp nhận MEP family instances (equipment, fixtures, fittings)
                if (elem is FamilyInstance familyInstance)
                {
                    // Kiểm tra xem có phải MEP family không
                    if (familyInstance.MEPModel != null)
                        return true;

                    // Kiểm tra category
                    var category = elem.Category;
                    if (category != null)
                    {
                        var categoryId = category.Id.IntegerValue;
                        
                        // MEP categories
                        if (categoryId == (int)BuiltInCategory.OST_MechanicalEquipment ||
                            categoryId == (int)BuiltInCategory.OST_DuctFitting ||
                            categoryId == (int)BuiltInCategory.OST_DuctAccessory ||
                            categoryId == (int)BuiltInCategory.OST_DuctTerminal ||
                            categoryId == (int)BuiltInCategory.OST_PipeFitting ||
                            categoryId == (int)BuiltInCategory.OST_PipeAccessory ||
                            categoryId == (int)BuiltInCategory.OST_PlumbingFixtures ||
                            categoryId == (int)BuiltInCategory.OST_ElectricalEquipment ||
                            categoryId == (int)BuiltInCategory.OST_ElectricalFixtures ||
                            categoryId == (int)BuiltInCategory.OST_LightingFixtures ||
                            categoryId == (int)BuiltInCategory.OST_CableTrayFitting ||
                            categoryId == (int)BuiltInCategory.OST_ConduitFitting)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra xem element có phải MEP element không
        /// </summary>
        /// <param name="element">Element cần kiểm tra</param>
        /// <returns>True nếu là MEP element</returns>
        public static bool IsMEPElement(Element element)
        {
            if (element == null) return false;

            // MEP Curves
            if (element is MEPCurve) return true;

            // MEP Family Instances
            if (element is FamilyInstance familyInstance)
            {
                if (familyInstance.MEPModel != null) return true;

                var category = element.Category;
                if (category != null)
                {
#if REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023
                    var categoryId = category.Id.IntegerValue;
                    
                    // Kiểm tra các category MEP
                    int[] mepCategories = {
                        (int)BuiltInCategory.OST_MechanicalEquipment,
                        (int)BuiltInCategory.OST_DuctFitting,
                        (int)BuiltInCategory.OST_DuctAccessory,
                        (int)BuiltInCategory.OST_DuctTerminal,
                        (int)BuiltInCategory.OST_PipeFitting,
                        (int)BuiltInCategory.OST_PipeAccessory,
                        (int)BuiltInCategory.OST_PlumbingFixtures,
                        (int)BuiltInCategory.OST_ElectricalEquipment,
                        (int)BuiltInCategory.OST_ElectricalFixtures,
                        (int)BuiltInCategory.OST_LightingFixtures,
                        (int)BuiltInCategory.OST_CableTrayFitting,
                        (int)BuiltInCategory.OST_ConduitFitting
                    };

                    return mepCategories.Contains(categoryId);
#else
                    var categoryId = category.Id.IntegerValue;
                    
                    // Kiểm tra các category MEP
                    long[] mepCategories = {
                        (long)BuiltInCategory.OST_MechanicalEquipment,
                        (long)BuiltInCategory.OST_DuctFitting,
                        (long)BuiltInCategory.OST_DuctAccessory,
                        (long)BuiltInCategory.OST_DuctTerminal,
                        (long)BuiltInCategory.OST_PipeFitting,
                        (long)BuiltInCategory.OST_PipeAccessory,
                        (long)BuiltInCategory.OST_PlumbingFixtures,
                        (long)BuiltInCategory.OST_ElectricalEquipment,
                        (long)BuiltInCategory.OST_ElectricalFixtures,
                        (long)BuiltInCategory.OST_LightingFixtures,
                        (long)BuiltInCategory.OST_CableTrayFitting,
                        (long)BuiltInCategory.OST_ConduitFitting
                    };

                    return mepCategories.Contains(categoryId);
#endif
                }
            }

            return false;
        }

        /// <summary>
        /// Lấy tất cả MEP elements trong document
        /// </summary>
        /// <param name="doc">Document</param>
        /// <returns>Collection của MEP elements</returns>
        public static IList<Element> GetAllMEPElements(Document doc)
        {
            var collector = new FilteredElementCollector(doc);
            
            // Lấy tất cả MEP curves
            var mepCurves = collector
                .OfClass(typeof(MEPCurve))
                .ToElements();

            // Lấy tất cả MEP family instances
            var familyInstances = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .ToElements()
                .Where(e => IsMEPElement(e));

            // Kết hợp lại
            var allMEPElements = new List<Element>();
            allMEPElements.AddRange(mepCurves);
            allMEPElements.AddRange(familyInstances);

            return allMEPElements;
        }

        /// <summary>
        /// Lấy tên hiển thị của MEP element
        /// </summary>
        /// <param name="element">MEP element</param>
        /// <returns>Tên hiển thị</returns>
        public static string GetMEPElementDisplayName(Element element)
        {
            if (element == null) return "Unknown";

            try
            {
                // Thử lấy tên family trước
                if (element is FamilyInstance familyInstance)
                {
                    return $"{familyInstance.Symbol.FamilyName} - {familyInstance.Symbol.Name}";
                }

                // Với MEP curves, lấy tên type
                if (element is MEPCurve mepCurve)
                {
                    return $"{mepCurve.MEPSystem?.Name ?? "System"} - {mepCurve.Name}";
                }

                // Fallback về tên element
                return element.Name ?? $"Element {element.Id}";
            }
            catch
            {
                return $"Element {element.Id}";
            }
        }
    }
}
