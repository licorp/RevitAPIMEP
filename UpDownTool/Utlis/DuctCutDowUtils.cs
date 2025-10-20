
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Quoc_MEP.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quoc_MEP
{
    public class DuctCutDowUtils
    {

        public static void DuctCut45 (Document doc, Reference r1, Reference r2, double offset, bool isTop)
        {

            XYZ pick1 = r1.GlobalPoint;
            XYZ pick2 = r2.GlobalPoint;

            Duct duct = doc.GetElement(r1) as Duct;
            LocationCurve locationCurve = duct.Location as LocationCurve;
            Line locationLine = locationCurve.Curve as Line;
            XYZ direction = locationLine.Direction;

            Plane plane1 = Plane.CreateByNormalAndOrigin(direction, pick1);
            Plane plane2 = Plane.CreateByNormalAndOrigin(direction, pick2);

            XYZ gd1 = MEPLib.LineIntersectPlane(locationLine, plane1);
            XYZ gd2 = MEPLib.LineIntersectPlane(locationLine, plane2);


            double dis = offset / Math.Tan(MEPLib.ToRadian(45));
            Line line = Line.CreateBound(gd1, gd2);

            XYZ p1 = MEPLib.FindPointOnLineFromStartPoint(line, dis);
            XYZ p2 = MEPLib.FindPointOnLineFromEndPoint(line, -dis);

            double length = p1.DistanceTo(p2);
            IList<ElementId> ids = new List<ElementId>();

            using (Transaction t = new Transaction(doc, " "))
            {
                t.Start();

                ElementId id1 = MechanicalUtils.BreakCurve(doc, duct.Id, p1);
                ids.Add(id1);

                ElementId id2 = null;
                ElementId id3 = null;

                try
                {
                    id2 = MechanicalUtils.BreakCurve(doc, duct.Id, p2);
                }
                catch { }
                try
                {
                    id3 = MechanicalUtils.BreakCurve(doc, id1, p2);
                }
                catch { }

                if (id2 != null) ids.Add(id2);
                if (id3 != null) ids.Add(id3);
                ids.Add(duct.Id);


                MEPLib.DeleteElement(doc, ids, length, out List<ElementId> newIds);

                XYZ ngd1;
                XYZ ngd2;
                if (isTop)
                {
                    ngd1 = new XYZ(gd1.X, gd1.Y, gd1.Z + offset);
                    ngd2 = new XYZ(gd2.X, gd2.Y, gd2.Z + offset);
                }
                else
                {
                    ngd1 = new XYZ(gd1.X, gd1.Y, gd1.Z - offset);
                    ngd2 = new XYZ(gd2.X, gd2.Y, gd2.Z - offset);
                }

                ElementId ductTypeId = duct.GetTypeId();
                ElementId levelId = duct.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();
                ElementId systemId = duct.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM).AsElementId();

                Duct ductTop = Duct.Create(doc, systemId, ductTypeId, levelId, ngd1, ngd2);

                Duct ductLeft = Duct.Create(doc, systemId, ductTypeId, levelId, p1, ngd1);

                Duct ductRight = Duct.Create(doc, systemId, ductTypeId, levelId, p2, ngd2);

                MEPLib.CreateElbowFiting(doc, ductTop, ductLeft);
                MEPLib.CreateElbowFiting(doc, ductTop, ductRight);

                Duct d1 = doc.GetElement(newIds[0]) as Duct;
                Duct d2 = doc.GetElement(newIds[1]) as Duct;

                bool isIntersection = MEPLib.ChekSolid(d1, ductLeft);

                if (isIntersection)
                {
                    MEPLib.CreateElbowFiting(doc, d1, ductLeft);
                    MEPLib.CreateElbowFiting(doc, d2, ductRight);
                }
                else
                {
                    MEPLib.CreateElbowFiting(doc, d2, ductLeft);
                    MEPLib.CreateElbowFiting(doc, d1, ductRight);
                }

                t.Commit();
            }
        }

        public static void DutCut90(Document doc, Reference r1, Reference r2, double offset, bool isTop)
        {

            XYZ pick1 = r1.GlobalPoint;
            XYZ pick2 = r2.GlobalPoint;

            Duct duct = doc.GetElement(r1) as Duct;
            LocationCurve locationCurve = duct.Location as LocationCurve;
            Line locationLine = locationCurve.Curve as Line;
            XYZ direction = locationLine.Direction;

            Plane plane1 = Plane.CreateByNormalAndOrigin(direction, pick1);
            Plane plane2 = Plane.CreateByNormalAndOrigin(direction, pick2);

            XYZ gd1 = MEPLib.LineIntersectPlane(locationLine, plane1);
            XYZ gd2 = MEPLib.LineIntersectPlane(locationLine, plane2);


            //double dis = offset / Math.Tan(MEPLib.ToRadian(45));
            double dis = 0;
            Line line = Line.CreateBound(gd1, gd2);

            XYZ p1 = MEPLib.FindPointOnLineFromStartPoint(line, dis);
            XYZ p2 = MEPLib.FindPointOnLineFromEndPoint(line, -dis);

            double length = p1.DistanceTo(p2);
            IList<ElementId> ids = new List<ElementId>();

            using (Transaction t = new Transaction(doc, " "))
            {
                t.Start();

                ElementId id1 = MechanicalUtils.BreakCurve(doc, duct.Id, p1);
                ids.Add(id1);

                ElementId id2 = null;
                ElementId id3 = null;

                try
                {
                    id2 = MechanicalUtils.BreakCurve(doc, duct.Id, p2);
                }
                catch { }
                try
                {
                    id3 = MechanicalUtils.BreakCurve(doc, id1, p2);
                }
                catch { }

                if (id2 != null) ids.Add(id2);
                if (id3 != null) ids.Add(id3);
                ids.Add(duct.Id);


                MEPLib.DeleteElement(doc, ids, length, out List<ElementId> newIds);


                XYZ ngd1;
                XYZ ngd2;
                if (isTop)
                {
                    ngd1 = new XYZ(gd1.X, gd1.Y, gd1.Z + offset);
                    ngd2 = new XYZ(gd2.X, gd2.Y, gd2.Z + offset);
                }
                else
                {
                    ngd1 = new XYZ(gd1.X, gd1.Y, gd1.Z - offset);
                    ngd2 = new XYZ(gd2.X, gd2.Y, gd2.Z - offset);
                }

                

                ElementId ductTypeId = duct.GetTypeId();
                ElementId levelId = duct.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();
                ElementId systemId = duct.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM).AsElementId();

                Duct ductBottom = Duct.Create(doc, systemId, ductTypeId, levelId, ngd1, ngd2);

                Duct ductLeft = Duct.Create(doc, systemId, ductTypeId, levelId, p1, ngd1);

                Duct ductRight = Duct.Create(doc, systemId, ductTypeId, levelId, p2, ngd2);

                MEPLib.CreateElbowFiting(doc, ductBottom, ductLeft);
                MEPLib.CreateElbowFiting(doc, ductBottom, ductRight);

                Duct d1 = doc.GetElement(newIds[0]) as Duct;
                Duct d2 = doc.GetElement(newIds[1]) as Duct;

                bool isIntersection = MEPLib.ChekSolid(d1, ductLeft);

                if (isIntersection)
                {
                    MEPLib.CreateElbowFiting(doc, d1, ductLeft);
                    MEPLib.CreateElbowFiting(doc, d2, ductRight);
                }
                else
                {
                    MEPLib.CreateElbowFiting(doc, d2, ductLeft);
                    MEPLib.CreateElbowFiting(doc, d1, ductRight);
                }

                t.Commit();
            }
        }

        public static void DuctMove45(Document doc, Reference r1, Reference r2, double offset, bool isTop)
        {

            XYZ pick1 = r1.GlobalPoint;
            XYZ pick2 = r2.GlobalPoint;

            Duct duct = doc.GetElement(r1) as Duct;
            LocationCurve locationCurve = duct.Location as LocationCurve;
            Line locationLine = locationCurve.Curve as Line;
            XYZ direction = locationLine.Direction;

            Plane plane1 = Plane.CreateByNormalAndOrigin(direction, pick1);
            Plane plane2 = Plane.CreateByNormalAndOrigin(direction, pick2);

            XYZ gd1 = MEPLib.LineIntersectPlane(locationLine, plane1);
            XYZ gd2 = MEPLib.LineIntersectPlane(locationLine, plane2);


            //double dis = offset / Math.Tan(MEPLib.ToRadian(45));
            double dis = 0;
            Line line = Line.CreateBound(gd1, gd2);

            XYZ p1 = MEPLib.FindPointOnLineFromStartPoint(line, dis);
            XYZ p2 = MEPLib.FindPointOnLineFromEndPoint(line, -dis);

            double length = p1.DistanceTo(p2);
            IList<ElementId> ids = new List<ElementId>();

            using (Transaction t = new Transaction(doc, " "))
            {
                t.Start();

                ElementId id1 = MechanicalUtils.BreakCurve(doc, duct.Id, p1);
                ids.Add(id1);

                ElementId id2 = null;
                ElementId id3 = null;

                try
                {
                    id2 = MechanicalUtils.BreakCurve(doc, duct.Id, p2);
                }
                catch { }
                try
                {
                    id3 = MechanicalUtils.BreakCurve(doc, id1, p2);
                }
                catch { }

                if (id2 != null) ids.Add(id2);
                if (id3 != null) ids.Add(id3);
                ids.Add(duct.Id);


                MEPLib.DeleteElement(doc, ids, length, out List<ElementId> newIds);


                XYZ ngd1;
                XYZ ngd2;
                if (isTop)
                {
                    ngd1 = new XYZ(gd1.X, gd1.Y, gd1.Z + offset);
                    ngd2 = new XYZ(gd2.X, gd2.Y, gd2.Z + offset);
                }
                else
                {
                    ngd1 = new XYZ(gd1.X, gd1.Y, gd1.Z - offset);
                    ngd2 = new XYZ(gd2.X, gd2.Y, gd2.Z - offset);
                }



                ElementId ductTypeId = duct.GetTypeId();
                ElementId levelId = duct.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();
                ElementId systemId = duct.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM).AsElementId();

                Duct ductBottom = Duct.Create(doc, systemId, ductTypeId, levelId, ngd1, ngd2);

                Duct ductLeft = Duct.Create(doc, systemId, ductTypeId, levelId, p1, ngd1);

                Duct ductRight = Duct.Create(doc, systemId, ductTypeId, levelId, p2, ngd2);

                MEPLib.CreateElbowFiting(doc, ductBottom, ductLeft);
                MEPLib.CreateElbowFiting(doc, ductBottom, ductRight);

                Duct d1 = doc.GetElement(newIds[0]) as Duct;
                Duct d2 = doc.GetElement(newIds[1]) as Duct;

                bool isIntersection = MEPLib.ChekSolid(d1, ductLeft);

                if (isIntersection)
                {
                    MEPLib.CreateElbowFiting(doc, d1, ductLeft);
                    MEPLib.CreateElbowFiting(doc, d2, ductRight);
                }
                else
                {
                    MEPLib.CreateElbowFiting(doc, d2, ductLeft);
                    MEPLib.CreateElbowFiting(doc, d1, ductRight);
                }

                t.Commit();
            }
        }

        public static void DuctMove90(Document doc, Reference r1, Reference r2, double offset, bool isTop)
        {

            XYZ pick1 = r1.GlobalPoint;
            XYZ pick2 = r2.GlobalPoint;

            Duct duct = doc.GetElement(r1) as Duct;
            LocationCurve locationCurve = duct.Location as LocationCurve;
            Line locationLine = locationCurve.Curve as Line;
            XYZ direction = locationLine.Direction;

            Plane plane1 = Plane.CreateByNormalAndOrigin(direction, pick1);
            Plane plane2 = Plane.CreateByNormalAndOrigin(direction, pick2);

            XYZ gd1 = MEPLib.LineIntersectPlane(locationLine, plane1);
            XYZ gd2 = MEPLib.LineIntersectPlane(locationLine, plane2);


            //double dis = offset / Math.Tan(MEPLib.ToRadian(45));
            double dis = 0;
            Line line = Line.CreateBound(gd1, gd2);

            XYZ p1 = MEPLib.FindPointOnLineFromStartPoint(line, dis);
            XYZ p2 = MEPLib.FindPointOnLineFromEndPoint(line, -dis);

            double length = p1.DistanceTo(p2);
            IList<ElementId> ids = new List<ElementId>();

            using (Transaction t = new Transaction(doc, " "))
            {
                t.Start();

                ElementId id1 = MechanicalUtils.BreakCurve(doc, duct.Id, p1);
                ids.Add(id1);

                ElementId id2 = null;
                ElementId id3 = null;

                try
                {
                    id2 = MechanicalUtils.BreakCurve(doc, duct.Id, p2);
                }
                catch { }
                try
                {
                    id3 = MechanicalUtils.BreakCurve(doc, id1, p2);
                }
                catch { }

                if (id2 != null) ids.Add(id2);
                if (id3 != null) ids.Add(id3);
                ids.Add(duct.Id);


                MEPLib.DeleteElement(doc, ids, length, out List<ElementId> newIds);


                XYZ ngd1;
                XYZ ngd2;
                if (isTop)
                {
                    ngd1 = new XYZ(gd1.X, gd1.Y, gd1.Z + offset);
                    ngd2 = new XYZ(gd2.X, gd2.Y, gd2.Z + offset);
                }
                else
                {
                    ngd1 = new XYZ(gd1.X, gd1.Y, gd1.Z - offset);
                    ngd2 = new XYZ(gd2.X, gd2.Y, gd2.Z - offset);
                }



                ElementId ductTypeId = duct.GetTypeId();
                ElementId levelId = duct.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();
                ElementId systemId = duct.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM).AsElementId();

                Duct ductBottom = Duct.Create(doc, systemId, ductTypeId, levelId, ngd1, ngd2);

                Duct ductLeft = Duct.Create(doc, systemId, ductTypeId, levelId, p1, ngd1);

                Duct ductRight = Duct.Create(doc, systemId, ductTypeId, levelId, p2, ngd2);

                MEPLib.CreateElbowFiting(doc, ductBottom, ductLeft);
                MEPLib.CreateElbowFiting(doc, ductBottom, ductRight);

                Duct d1 = doc.GetElement(newIds[0]) as Duct;
                Duct d2 = doc.GetElement(newIds[1]) as Duct;

                bool isIntersection = MEPLib.ChekSolid(d1, ductLeft);

                if (isIntersection)
                {
                    MEPLib.CreateElbowFiting(doc, d1, ductLeft);
                    MEPLib.CreateElbowFiting(doc, d2, ductRight);
                }
                else
                {
                    MEPLib.CreateElbowFiting(doc, d2, ductLeft);
                    MEPLib.CreateElbowFiting(doc, d1, ductRight);
                }

                t.Commit();
            }
        }
    }
}
