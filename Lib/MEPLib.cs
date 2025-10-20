using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Electrical;

namespace Quoc_MEP.Lib
{
    public class MEPLib
    {
        //convert bitmap to bimapimage
        public static BitmapImage Convert(Bitmap bimap)
        {
            MemoryStream memory = new MemoryStream();
            bimap.Save(memory, ImageFormat.Png);
            memory.Position = 0;
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }


        public static XYZ LineIntersectPlane (Line line, Plane plane)
        {
            XYZ normal = plane.Normal;
            XYZ origin = plane.Origin;

            XYZ sp = line.GetEndPoint(0);
            XYZ ep = line.GetEndPoint(1);

            XYZ normalize = (ep - sp).Normalize();

            double distance = (normal.DotProduct(origin) - normal.DotProduct(sp)) / normal.DotProduct(normalize);

            XYZ intersectPoint = sp + distance * normalize;

            return intersectPoint;
        }

        public static XYZ FindPointOnLineFromStartPoint(Line line, double distance)
        {
            XYZ A = line.GetEndPoint(0);
            XYZ B = line.GetEndPoint(1);
            XYZ AB = A - B;

            double tile = distance / AB.GetLength();

            double x = tile * AB.X + A.X;
            double y = tile * AB.Y + A.Y;
            double z = tile * AB.Z + A.Z;

            return new XYZ(x, y, z);
        }

        public static XYZ FindPointOnLineFromEndPoint(Line line, double distance)
        {
            XYZ A = line.GetEndPoint(0);
            XYZ B = line.GetEndPoint(1);
            XYZ AB = A - B;

            double tile = distance / AB.GetLength();

            double x = tile * AB.X + B.X;
            double y = tile * AB.Y + B.Y;
            double z = tile * AB.Z + B.Z;

            return new XYZ(x, y, z);
        }


        public static double ToRadian (double degree)
        {
            return degree * Math.PI / 180;
        }


        public static void CreateElbowFiting(Document doc, Duct duct1, Duct duct2)
        {
            ConnectorManager cM1 = duct1.ConnectorManager;
            ConnectorManager cM2 = duct2.ConnectorManager;

            ConnectorSet cS1 = cM1.Connectors;
            ConnectorSet cS2 = cM2.Connectors;

            List<Connector> list = new List<Connector>();

            //tìm 2 connector trùng nhau
            foreach (Connector c1 in cS1)
            {
                foreach (Connector c2 in cS2)
                {
                    XYZ o1 = c1.Origin;
                    XYZ o2 = c2.Origin;
                    double kc = Math.Round(o1.DistanceTo(o2), 3);
                    if (kc == 0)
                    {
                        list.Add(c1);
                        list.Add(c2);
                        break;
                    }
                }
            }

            try
            {
                doc.Create.NewElbowFitting(list[0], list[1]);
            }
            catch { }


        }


        public static Solid GetMEPSolid(Element element)
        {

            Options options = new Options()
            {
                ComputeReferences = true,
                DetailLevel = ViewDetailLevel.Medium,
            };

            GeometryElement geometryElement = element.get_Geometry(options);
            foreach (GeometryObject geoOb in geometryElement)
            {
                if (geoOb is Solid solid) return solid;
            }

            return null;
        }


        public static bool ChekSolid(Element ele1, Element ele2)
        {

            Solid solid1 = GetMEPSolid(ele1);
            Solid solid2 = GetMEPSolid(ele2);

            Solid intersectSolid = BooleanOperationsUtils.ExecuteBooleanOperation(solid1, solid2, BooleanOperationsType.Intersect);

            if (intersectSolid != null && intersectSolid.Volume > 0) return true;

            return false;
        }

        public static void DeleteElement(Document doc, IList<ElementId> ids, double length, out List<ElementId> result)
        {
            result = new List<ElementId>();

            foreach (ElementId id in ids)
            {
                LocationCurve locationCurve = null;

                Element element = doc.GetElement(id);

                if (element is Duct duct) locationCurve = duct.Location as LocationCurve;
                if (element is Pipe pipe) locationCurve = pipe.Location as LocationCurve;
                if (element is CableTray cableTray) locationCurve = cableTray.Location as LocationCurve;

                double curveLength = locationCurve.Curve.Length;
                double val = Math.Round(curveLength - length, 3);

                if (val == 0) doc.Delete(id);
                else result.Add(id);
            }
        }

    }
}
