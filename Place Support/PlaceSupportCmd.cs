using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quoc_MEP
{
    [Transaction(TransactionMode.Manual)]
    public class PlaceSupportCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            double distanceOffset = 1000 / 304.8;
            double distanceStart = 500 / 304.8;
            string familyName = "Pipe Support_V";
            string typeName = "Test";

            //pick ?ng nu?c
            Reference reference = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new PipeFilter());
            Pipe pipe = doc.GetElement(reference) as Pipe;

            //Pick m?t sàn
            Reference pickFace = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Face);
            Floor floor = doc.GetElement(pickFace) as Floor;
            Face face = floor.GetGeometryObjectFromReference(pickFace) as Face;


            //l?y thông tin c?a ?ng nu?c
            LocationCurve locationCurve = pipe.Location as LocationCurve;
            Line pipeLine = locationCurve.Curve as Line;
            double diameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
            ElementId levelId = pipe.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();
            Level level = doc.GetElement(levelId) as  Level;

            Line flatLine = FlatLine(pipeLine);
            double number = (flatLine.Length - distanceStart) / distanceOffset;
            int total = (int)Math.Truncate(number) + 1;

            //tính toán các thông tin
            XYZ point0 = FindPointOnLineFromStartPoint(flatLine, distanceStart);
            XYZ point1 = flatLine.GetEndPoint(1);
            Line newLine = Line.CreateBound(point0, point1);
            double radian = newLine.Direction.AngleTo(XYZ.BasisX);
            double degree = Math.Round(radian * 180 / Math.PI, 2);


            using (Transaction t = new Transaction(doc, " "))
            {
                t.Start();

                FamilySymbol symbol = GetFamilySymbol(doc, familyName, typeName);
                if (!symbol.IsActive) symbol.Activate();

                for (int i = 0; i < total; i++)
                {
                    XYZ point = FindPointOnLineFromStartPoint(newLine, i * distanceOffset);
                    FamilyInstance support = doc.Create.NewFamilyInstance(point, symbol, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                    XYZ pointOnFace = PointOnFace(point, face);
                    XYZ pointOnLine = PointOnLine(point, pipeLine);

                    if (pointOnLine != null)
                    {
                        support.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(pointOnLine.Z - diameter / 2);
                        support.LookupParameter("Pipe Diameter").Set(diameter);
                    }    
                    if (pointOnFace != null)
                    {
                        double height = pointOnFace.DistanceTo(pointOnLine) + diameter / 2;
                        support.LookupParameter("Height").Set(height);
                    }

                    //ROTARE
                    XYZ pointZ = point + new XYZ(0, 0, 1);
                    Line axis = Line.CreateBound(point, pointZ);

                    if (degree == 0 || degree == 180) //ông song song tr?c Z
                    {
                        (support.Location as LocationPoint).Rotate(axis, Math.PI/2);
                    }    
                    if (degree != 0 && degree != 180 && degree != 90) // ?ng không song song tr?c X và Y
                    {
                        (support.Location as LocationPoint).Rotate(axis, Math.PI / 2 - radian);
                    }    

                }


                t.Commit();
            }    

            



            return Result.Succeeded;
        }


        private Line FlatLine(Line line)
        {
            XYZ sp = line.GetEndPoint(0);
            XYZ ep = line.GetEndPoint(1);

            double val = Math.Round(sp.Z - ep.Z, 3);
            if (val == 0) return line;
            else
            {
                if (sp.Z > ep.Z)
                {
                    XYZ newPoint = new XYZ(ep.X, ep.Y, sp.Z);
                    return Line.CreateBound(sp, newPoint);
                }
                else
                {
                    XYZ newPoint = new XYZ(sp.X, sp.Y, ep.Z);
                    return Line.CreateBound(newPoint, ep);
                }
            }    
        }

        private XYZ FindPointOnLineFromStartPoint (Line line, double distance)
        {
            //get point
            XYZ A = line.GetEndPoint(0);
            XYZ B = line.GetEndPoint(1);
            XYZ AB = B - A;
            double tile = distance / AB.GetLength();

            double x = tile * AB.X + A.X;
            double y = tile * AB.Y + A.Y;
            double z = tile * AB.Z + A.Z;

            return new XYZ(x, y, z);
        }

        private FamilySymbol GetFamilySymbol(Document doc, string familyName, string typeName)
        {
            try
            {
                var listfamilySymbol = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_MechanicalEquipment)
                    .WhereElementIsElementType()
                    .Cast<FamilySymbol>()
                    .ToList();

                var symbol = listfamilySymbol.Find(x => x.FamilyName == familyName && x.Name == typeName);
                return symbol;
            }
            catch
            {
                return null;
            }
            
        }

        private XYZ PointOnFace (XYZ point, Face face)
        {
            XYZ poitZ = point + new XYZ(0, 0, 1000);
            Line lineZ = Line.CreateBound(point, poitZ);
            face.Intersect(lineZ, out IntersectionResultArray array);
            if (array != null && array.Size == 1)
            {
                return array.get_Item(0).XYZPoint;
            }
            return null;
        }

        private XYZ PointOnLine (XYZ point, Line pipeLine)
        {
            XYZ pointZ = point - new XYZ(0, 0, 1000);
            Line lineZ = Line.CreateBound(point, pointZ);
            pipeLine.Intersect(lineZ, out IntersectionResultArray array);
            if (array != null && array.Size == 1)
            {
                return array.get_Item(0).XYZPoint;
            }
            return null;
        }
    }
}
