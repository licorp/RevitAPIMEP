using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Quoc_MEP.Lib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Quoc_MEP
{
    [Transaction(TransactionMode.Manual)]
    public class DrawPipe : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            //get UIdocument, document
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            //select pipes
            IList<Element> listPipes = uidoc.Selection.PickElementsByRectangle(new PipeFilter(), "Select Pipes");

            IList<Element> selectedPipes = new List<Element>();
            if (listPipes.Count > 0)
            {
                selectedPipes = listPipes.Where(ele => IsNoneSlope(ele as Pipe)).ToList();

                if (selectedPipes.Count > 0)
                {
                    while (true)
                    {
                        try
                        {
                            XYZ pickPoint = uidoc.Selection.PickPoint();

                            bool isPointInside = IsPointInside(selectedPipes, pickPoint);

                            if (isPointInside) //option 1 : extend pipes
                            {

                                using (Transaction t = new Transaction(doc, " "))
                                {
                                    t.Start();

                                    var listXYZ = GetListPoint(selectedPipes, pickPoint);
                                    for (int i = 0; i < selectedPipes.Count; i++)
                                    {
                                        var pipe = selectedPipes[i] as Pipe;
                                        XYZ pointToExtend = listXYZ[i];
                                        ExtendPipes(pipe, pointToExtend);
                                    }

                                    t.Commit();
                                }

                            }
                            else //option 2: create new pipes
                            {

                                //tìm di?m g?n nh?t d?n v? trí pickpoint
                                XYZ nearestPoint = GetNearestPoint(selectedPipes, pickPoint, out Pipe nearestPipe);

                                //taoj line t? di?m pickpoint d?n di?m g?n nh?t
                                XYZ newPoint = new XYZ(pickPoint.X, pickPoint.Y, nearestPoint.Z);
                                Line originalLine = Line.CreateBound(nearestPoint, newPoint);


                                //l?y danh sách kho?ng cách gi?a các ?ng
                                List<double> listDistance = GetListDistance(selectedPipes, newPoint);

                                //t?o danh sách các lines du?c offset
                                List<Line> listLine1 = new List<Line>();
                                List<Line> listLine2 = new List<Line>();


                                foreach (double value in listDistance)
                                {
                                    Line offsetLine1 = originalLine.CreateOffset(value, XYZ.BasisZ) as Line;
                                    Line offsetLine2 = originalLine.CreateOffset(-value, XYZ.BasisZ) as Line;

                                    listLine1.Add(offsetLine1);
                                    listLine2.Add(offsetLine2);
                                }

                                //l?c ra danh sách các line c?n tìm
                                List<Line> listLineFinal = new List<Line>();
                                Line line1 = listLine1[0];
                                bool isIntersect = IsLineIntersection(nearestPipe, line1);

                                if (isIntersect) listLineFinal = listLine2;
                                else listLineFinal = listLine1;
                                listLineFinal.Insert(0, originalLine);

                                //s?p x?p l?i list pipe
                                var listPipeSorted = SortedListPipe(selectedPipes, pickPoint);

                                //t?o list d? luu tr? pipe m?i du?c t?o ra
                                IList<Element> newList = new List<Element>();
                                using (Transaction t = new Transaction(doc, " "))
                                {
                                    t.Start();

                                    for (int i = 0; i < listPipeSorted.Count; i++)
                                    {
                                        Pipe pipe_i = listPipeSorted[i];
                                        Line line_i = listLineFinal[i];
                                        //doc.Create.NewDetailCurve(doc.ActiveView, line_i);

                                        //m? r?ng pipe
                                        XYZ intersectPoint = PipeIntersectionLine(pipe_i, line_i);
                                        ExtendPipes(pipe_i, intersectPoint);

                                        //L?y thông tin pipe
                                        var systemTypeId = pipe_i.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId();
                                        var levelId = pipe_i.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();
                                        var diameter = pipe_i.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
                                        var typeId = pipe_i.GetTypeId();

                                        XYZ ep = line_i.GetEndPoint(1);
                                        Pipe newPipe = Pipe.Create(doc, systemTypeId, typeId, levelId, intersectPoint, ep);
                                        newPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diameter);
                                        CreateElbowFiting(doc, newPipe, pipe_i);
                                        newList.Add(newPipe);

                                    }

                                   

                                    t.Commit();
                                }

                                selectedPipes.Clear();
                                selectedPipes = new List<Element>(newList);
                            }
                        }
                        catch
                        {
                            break;
                        }
                    }
                   
                }    
            }

            



            return Result.Succeeded;
        }

        //ki?m tra ?ng có d? d?c hay không?
        private bool IsNoneSlope(Pipe pipe)
        {
            double slope = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_SLOPE).AsDouble();
            if (slope == 0) return true;
            return false;
        }




        //m? r?ng du?ng th?ng theo 2 hu?ng v?i 1 kho?ng cách c? d?nh
        private Line CreateExtendLine (Line line, double distance)
        {
            XYZ sp = line.GetEndPoint(0);
            XYZ ep = line.GetEndPoint(1);

            //l?y vector normalize
            XYZ normalize = (ep - sp).Normalize();


            //tính toán t?a d? di?m m? r?ng
            XYZ nsp = sp - normalize * distance;
            XYZ nep = ep + normalize * distance;

            Line newLine = Line.CreateBound(nsp, nep);
            return newLine;
        }

        private List<XYZ> GetListPoint(IList<Element> listPipes, XYZ pickPoint)
        {
            //t?o m?t ph?ng
            Pipe pipe0 = listPipes[0] as Pipe;
            LocationCurve lc = pipe0.Location as LocationCurve;
            Line line0 = lc.Curve as Line;
            Plane plane = Plane.CreateByNormalAndOrigin(line0.Direction, pickPoint);

            var listXYZ = new List<XYZ>();
            foreach (Element element in listPipes)
            {
                Pipe pipe = element as Pipe;
                LocationCurve locationCurve = pipe.Location as LocationCurve;
                Line line = locationCurve.Curve as Line;

                Line newLine = CreateExtendLine(line, 500);
                XYZ intersectPoint = MEPLib.LineIntersectPlane(newLine, plane);
                listXYZ.Add(intersectPoint);
            }
            return listXYZ;
        }


        //Tính kho?ng cách l?n nh?t gi?a các ?ng
        private double GetMaxDistance(IList<Element> listPipes, XYZ pickPoint)
        {
            var listXYZ = GetListPoint(listPipes, pickPoint);
            var listDistance = new List<double>();

            for(int i = 0; i <listXYZ.Count; i++)
            {
                XYZ p1 = listXYZ[i];

                foreach(XYZ p2 in listXYZ)
                {
                    double distance = p2.DistanceTo(p1);
                    listDistance.Add(distance);
                }
            }

            return listDistance.Max();
        }

        //Ki?m tra di?m pickpoint có n?m gi?a các ?ng hay không?
        private bool IsPointInside (IList<Element> listPipes, XYZ pickPoint)
        {
            var max = GetMaxDistance(listPipes, pickPoint);
            var listXYZ = GetListPoint(listPipes, pickPoint);


            //tìm t?a d? 2 di?m ngoài cùng
            XYZ point1 = new XYZ();
            XYZ point2 = new XYZ();

            for(int i = 0; i < listXYZ.Count;i++)
            {
                XYZ p1 = listXYZ[i];

                foreach(XYZ p2 in listXYZ)
                {
                    double distance = p2.DistanceTo(p1);
                    double value = Math.Round(max - distance, 0);

                    if (value == 0)
                    {
                        point1 = p1;
                        point2 = p2;
                        break;
                    }    
                }    
            }


            //quy v? cùng 1 m?t ph?ng
            XYZ pZ = new XYZ(pickPoint.X, pickPoint.Y, point1.Z);
            double maxDistance = pZ.DistanceTo(point1) + pZ.DistanceTo(point2);

            //so sánh hi?u s? 2 giá tr?
            if (Math.Round(maxDistance - max, 0) == 0) return true;
            return false;

        }


        //m? r?ng pipe d?n 1 di?m
        private void ExtendPipes (Pipe pipe, XYZ pointToExtend)
        {

            LocationCurve lc = pipe.Location as LocationCurve;
            Line line = lc.Curve as Line;
            XYZ sp = line.GetEndPoint(0);
            XYZ ep = line.GetEndPoint(1);

            XYZ dir1 = line.Direction;
            XYZ dir2 = pointToExtend - sp;
            double radian = dir1.AngleTo(dir2);
            double degree = Math.Round(radian * 180 / Math.PI, 3);


            double dis1 = sp.DistanceTo(pointToExtend);
            double dis2 = ep.DistanceTo(pointToExtend);

            if (degree == 180)
            {
                if (dis1 > dis2) line = Line.CreateBound(pointToExtend, sp);
                else line = Line.CreateBound(pointToExtend, ep);
            }
            if (degree == 0)
            {
                if (dis1 > dis2) line = Line.CreateBound(sp, pointToExtend);
                else line = Line.CreateBound(ep, pointToExtend);
            }

            (pipe.Location as LocationCurve).Curve = line;
        }

        //tìm di?m g?n nh?t d?n di?m pickpoint
        private XYZ GetNearestPoint(IList<Element> listPipes, XYZ pickPoint, out Pipe nearestPipe)
        {
            nearestPipe = null;
            var listDistance = new List<double>();
            foreach(Element ele in listPipes)
            {
                Pipe pipe = ele as Pipe;
                Connector cn = GetNearestConnector(pipe, pickPoint);
                XYZ pZ = new XYZ(pickPoint.X, pickPoint.Y, cn.Origin.Z);
                double distance = pZ.DistanceTo(cn.Origin);
                listDistance.Add(distance);
            }    

            double min = listDistance.Min();
            foreach(Element ele in listPipes)
            {
                Pipe pipe = ele as Pipe;
                Connector cn = GetNearestConnector(pipe, pickPoint);
                XYZ pZ = new XYZ(pickPoint.X, pickPoint.Y, cn.Origin.Z);
                double distance = pZ.DistanceTo(cn.Origin);

                double hs = Math.Round(distance-min, 2);
                if (hs == 0)
                {
                    nearestPipe = pipe;
                    return cn.Origin;
                }
            }

            return null;

        }

        //tìm connector g?n nh?t d?n di?m pickpoint
        private Connector GetNearestConnector(Pipe pipe, XYZ pickPoint)
        {
            ConnectorManager cm = pipe.ConnectorManager;
            ConnectorSet cs = cm.Connectors;

            var listDistance = new List<double>();
            foreach(Connector cn in cs)
            {
                XYZ origin = cn.Origin;
                double distance = origin.DistanceTo(pickPoint);
                listDistance.Add(distance);
            }

            double min = listDistance.Min();

            foreach(Connector cn in cs)
            {
                XYZ origin = cn.Origin;
                double distance = origin.DistanceTo(pickPoint);
                double hs = Math.Round(distance - min, 3);
                if (hs == 0) return cn;
            }

            return null;
        }

        //tính kho?ng cách t? ?ng d?n point
        private double DistancePipeToPoint(Pipe pipe, XYZ point)
        {
            LocationCurve locationCurve = pipe.Location as LocationCurve;
            Line line = locationCurve.Curve as Line;
            Line extendLine = CreateExtendLine(line, 500);
            return extendLine.Distance(point);
        }

        //tìm danh sách kho?ng cách gi?a các ?ng
        private List<double> GetListDistance(IList<Element> listPipes, XYZ pickPoint)
        {
            XYZ nearestPoint = GetNearestPoint(listPipes, pickPoint, out Pipe nearestPipe);

            var listDistance = (from ele in listPipes select DistancePipeToPoint(ele as Pipe, nearestPoint))
                        .OrderBy(dis => dis)
                        .ToList();

            listDistance.RemoveAt(0);


            return listDistance;
        }

        //ki?m tra pipe giao nhau v?i line
        private bool IsLineIntersection(Pipe pipe, Line line)
        {
            Line extendLine = CreateExtendLine(line, 500);
            double z = extendLine.GetEndPoint(0).Z;

            LocationCurve locationCurve = pipe.Location as LocationCurve;
            Line pipeLine = locationCurve.Curve as Line;
            XYZ sp = pipeLine.GetEndPoint(0);
            XYZ ep = pipeLine.GetEndPoint(1);

            XYZ nsp = new XYZ(sp.X, sp.Y, z);
            XYZ nep = new XYZ(ep.X, ep.Y, z);
            Line newLine = Line.CreateBound(nsp, nep);

            extendLine.Intersect(newLine, out IntersectionResultArray array);
            if (array != null && array.Size == 1) return true;
            return false;

        }

        //s?p s?p l?i v? trí các ?ng theo th? t? min -> max
        private List<Pipe> SortedListPipe (IList<Element> listPipes, XYZ pickPoint)
        {
            List<double> listDistance = GetListDistance(listPipes, pickPoint);
            XYZ nearestPoint = GetNearestPoint(listPipes, pickPoint, out Pipe nearestPipe);

            var listnewPipe =new List<Pipe>();
            foreach(double value in listDistance)
            {
                foreach(Element ele in listPipes)
                {
                    double distance = DistancePipeToPoint(ele as Pipe, nearestPoint);
                    double hs = Math.Round(distance - value, 3);
                    if (hs == 0)
                    {
                        listnewPipe.Add(ele as Pipe);
                        break;
                    }    
                }    
            }    
            listnewPipe.Insert(0, nearestPipe);
            return listnewPipe;
        }

        //tìm di?m giao nhau gi?a Pipe và Line
        private XYZ PipeIntersectionLine (Pipe pipe, Line line)
        {

            LocationCurve locationCurve = pipe.Location as LocationCurve;
            Line pipeLine = locationCurve.Curve as Line;
            Line pipeLineExtend = CreateExtendLine(pipeLine, 500);
            double z = pipeLineExtend.GetEndPoint(0).Z;


            Line extendLine = CreateExtendLine(line, 500);
            XYZ sp = extendLine.GetEndPoint(0);
            XYZ ep = extendLine.GetEndPoint(1);
            XYZ nsp = new XYZ(sp.X, sp.Y, z);
            XYZ nep = new XYZ(ep.X, ep.Y, z);

            Line newLine = Line.CreateBound(nep, nsp);
            newLine.Intersect(pipeLineExtend, out IntersectionResultArray resultArray);

            if (resultArray != null && resultArray.Size == 1) return resultArray.get_Item(0).XYZPoint;
            return null;

        }

        private void CreateElbowFiting(Document doc, Pipe pipe1, Pipe pipe2)
        {
            ConnectorManager cM1 = pipe1.ConnectorManager;
            ConnectorManager cM2 = pipe2.ConnectorManager;

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

    }
    
}
