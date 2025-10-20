using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace Quoc_MEP
{

    public class FloorFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            BuiltInCategory builtInCategory = (BuiltInCategory)element.Category.Id.IntegerValue;
            if (builtInCategory == BuiltInCategory.OST_Floors)
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference refer, XYZ point)
        {
            return false;
        }
    }

    public class AirTermialFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            BuiltInCategory builtInCategory = (BuiltInCategory)element.Category.Id.IntegerValue;
            if (builtInCategory == BuiltInCategory.OST_DuctTerminal)
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference refer, XYZ point)
        {
            return false;
        }
    }


    public class GridFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            BuiltInCategory builtInCategory = (BuiltInCategory)element.Category.Id.IntegerValue;
            if (builtInCategory == BuiltInCategory.OST_Grids)
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference refer, XYZ point)
        {
            return false;
        }
    }


    public class DuctFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            BuiltInCategory builtInCategory = (BuiltInCategory)element.Category.Id.IntegerValue;
            if (builtInCategory == BuiltInCategory.OST_DuctCurves)
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference refer, XYZ point)
        {
            return false;
        }
    }

    public class PipeFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            BuiltInCategory builtInCategory = (BuiltInCategory)element.Category.Id.IntegerValue;
            if (builtInCategory == BuiltInCategory.OST_PipeCurves)
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference refer, XYZ point)
        {
            return false;
        }
    }

    public class CadFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            if (element is ImportInstance)
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference refer, XYZ point)
        {
            return false;
        }
    }

    public class InstanceFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            if (element is FamilyInstance)
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference refer, XYZ point)
        {
            return false;
        }
    }
}
