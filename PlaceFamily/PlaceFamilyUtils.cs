using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quoc_MEP.PlaceFamily
{
    public class PlaceFamilyUtils
    {

        [Obsolete]
        public static List<string> GetListBlockCad(CADLinkType cadLinkType)
        {
            List<string> listName = new List<string>();
            string fileName = $"{cadLinkType.Name}.";
            GeometryElement geometryElement = cadLinkType.get_Geometry(new Options());
            foreach (GeometryObject geometryOb in geometryElement)
            {
                if (geometryOb != null && geometryOb is GeometryInstance)
                {
                    GeometryInstance geometryInstance = (GeometryInstance)geometryOb;
                    string name = geometryInstance.Symbol.Name.Replace(fileName, "");
                    listName.Add(name);
                }
            }

            listName.Sort();
            return listName.Distinct().ToList();
        }

        public static List<string> GetListFamily(Document doc, FamilyInstance instance)
        {
            var listFamily = new List<string>();

            BuiltInCategory builtIn = (BuiltInCategory)instance.Category.Id.IntegerValue;

            var listType = new FilteredElementCollector(doc)
                            .OfCategory(builtIn)
                            .WhereElementIsElementType()
                            .Cast<FamilySymbol>()
                            .ToList();
            foreach (FamilySymbol familySymbol in listType)
            {
                string familyName = familySymbol.FamilyName;
                if (!listFamily.Contains(familyName))
                {
                    listFamily.Add(familyName);
                }
            }

            return listFamily;
        }

        public static List<string> GetListTypeByFamilyName(Document doc, FamilyInstance instance, string familyName)
        {
            var listTypeName = new List<string>();

            BuiltInCategory builtIn = (BuiltInCategory)instance.Category.Id.IntegerValue;

            var listType = new FilteredElementCollector(doc)
                            .OfCategory(builtIn)
                            .WhereElementIsElementType()
                            .Cast<FamilySymbol>()
                            .ToList();

            foreach (FamilySymbol familySymbol in listType)
            {
                if (familySymbol.FamilyName == familyName)
                {
                    listTypeName.Add(familySymbol.Name);
                }

            }

            return listTypeName;
        }

        public static List<string> GetListLevelInModel(Document doc)
        {
            var listLevel = new FilteredElementCollector(doc)
                            .OfCategory(BuiltInCategory.OST_Levels)
                            .WhereElementIsNotElementType()
                            .Cast<Level>()
                            .OrderBy(x => x.Elevation)
                            .ToList();

            return listLevel.Select(x => x.Name).ToList();
        }

        public static Level GetLevelByName(Document doc, string levelName)
        {
            var listLevel = new FilteredElementCollector(doc)
                            .OfCategory(BuiltInCategory.OST_Levels)
                            .WhereElementIsNotElementType()
                            .Cast<Level>()
                            .ToList();

            return listLevel.Find(x => x.Name == levelName);
        }

        public static FamilySymbol GetFamilySymbolByName (Document doc, FamilyInstance instance, string familyName, string typeName)
        {
            FamilySymbol familySymbol = null;

            BuiltInCategory builtIn = (BuiltInCategory)instance.Category.Id.IntegerValue;
            var listType = new FilteredElementCollector(doc)
                        .OfCategory(builtIn)
                        .WhereElementIsElementType()
                        .Cast<FamilySymbol>()
                        .ToList();

            foreach(FamilySymbol symbol in listType)
            {
                if (symbol.FamilyName == familyName && symbol.Name == typeName)
                {
                    familySymbol = symbol;
                    break;
                }
            }
            return familySymbol;
        }

        [Obsolete]
        public static List<GeometryInstance> GetListBlockCadByName(CADLinkType cadLinkType, string blockName)
        {
            var listBlocks = new List<GeometryInstance>();
            string fileName = $"{cadLinkType.Name}.";

            GeometryElement geoEle = cadLinkType.get_Geometry(new Options());
            foreach(GeometryObject geoOb in geoEle)
            {
                if (geoOb != null && geoOb is GeometryInstance)
                {
                    GeometryInstance geoIns = (GeometryInstance)geoOb;
                    string name = geoIns.Symbol.Name.Replace(fileName, "");
                    if (name == blockName)
                    {
                        listBlocks.Add(geoIns);
                    }
                }
            }
            return listBlocks;
        }

    }
}
