using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Mechanical;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB.Visual;
using System.Windows.Documents;
using Quoc_MEP.SplitDuct;


namespace Quoc_MEP
{
    [Transaction(TransactionMode.Manual)]
    public class SplitDuctCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;


            IList<Reference> listOb = uidoc.Selection.PickObjects(ObjectType.Element, new DuctFilter(), "Pick Ducts");

            //Reference reference = uidoc.Selection.PickObject(ObjectType.Element, new DuctFilter());
            //Duct originDuct = doc.GetElement(reference) as Duct;

            SplitDuctWindow window = new SplitDuctWindow();
            window.ShowDialog();

            if(window.DialogResult == true)
            {
                string option = window.SplitOption;
                double distance = window.Distance / 304.8;

                //MessageBox.Show(distance.ToString());


                

                using (Transaction t = new Transaction(doc, "abc"))
                {
                    t.Start();

                    foreach(Reference reference in listOb)
                    {
                        Duct originDuct = doc.GetElement(reference) as Duct;
                        if (option.Contains("Start"))
                        {
                            SplitDuctFromStartPoint(doc, originDuct, distance);
                        }
                        else
                        {
                            SplitDuctFromEndPoint(doc, originDuct, distance);
                        }
                    }

                    MessageBox.Show("Complete!", "Message");
                    

                    t.Commit();
                }
            }

            


      






            return Result.Succeeded;
        }


        private XYZ FindPointOnLineFromStartPoint(Line line, double distance)
        {
            XYZ A = line.GetEndPoint(0);
            XYZ B = line.GetEndPoint(1);
            XYZ BA = B - A;

            double tile = distance / BA.GetLength();

            double x = tile * BA.X + A.X;
            double y = tile * BA.Y + A.Y;
            double z = tile * BA.Z + A.Z;

            return new XYZ(x, y, z);
        }

        private XYZ FindPointOnLineFromEndPoint (Line line, double distance)
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

        private void SplitDuctFromStartPoint(Document doc, Duct originDuct, double distance)
        {
            LocationCurve locationCurve = originDuct.Location as LocationCurve;
            Line locationLine = locationCurve.Curve as Line;
            double number = Math.Round(locationLine.Length / distance, 0);
            int total = int.Parse(number.ToString());

            Line line = locationLine;
            List<ElementId> listId = new List<ElementId>();
            for (int i = 0; i < total; i++)
            {
                try
                {
                    //ng?t ?ng gi�
                    XYZ p = FindPointOnLineFromStartPoint(line, distance);
                    ElementId id = MechanicalUtils.BreakCurve(doc, originDuct.Id, p);
                    if (id != null)
                    {
                        listId.Add(id);
                    }
                    
                }
                catch { }
            }

            listId.Add(originDuct.Id);
            CreateDuctFittings(doc, listId);
        }

        private void SplitDuctFromEndPoint(Document doc, Duct originDuct, double distance)
        {
            LocationCurve locationCurve = originDuct.Location as LocationCurve;
            Line locationLine = locationCurve.Curve as Line;
            double number = Math.Round(locationLine.Length / distance, 0);
            int total = int.Parse(number.ToString());

            Line line = locationLine;
            List<ElementId> listId = new List<ElementId>();
            listId.Add(originDuct.Id);

            for (int i = 0; i < total; i++)
            {
                try
                {
                    //ng?t ?ng gi�
                    XYZ p = FindPointOnLineFromEndPoint(line, distance);
                    ElementId id = MechanicalUtils.BreakCurve(doc, originDuct.Id, p);
                    if(id != null)
                    {
                        listId.Add(id);
                    }

                    //reset data
                    originDuct = doc.GetElement(id) as Duct;
                    LocationCurve lc = originDuct.Location as LocationCurve;
                    line = lc.Curve as Line;
                }
                catch { }
            }

            CreateDuctFittings(doc, listId);


        }

        private void CreateUnionFiting (Document doc, Duct duct1, Duct duct2)
        {
            ConnectorManager cM1 = duct1.ConnectorManager;
            ConnectorManager cM2 = duct2.ConnectorManager;

            ConnectorSet cS1 = cM1.Connectors;
            ConnectorSet cS2 = cM2.Connectors;

            List<Connector> list = new List<Connector>();

            //t�m 2 connector tr�ng nhau
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
                doc.Create.NewUnionFitting(list[0], list[1]);
            }
            catch { }
            

        }

        private void CreateDuctFittings(Document doc, List<ElementId> listIds)
        {
            Duct duct0 = doc.GetElement(listIds[0]) as Duct;
            if (duct0 != null)
            {
                for (int i = 1; i < listIds.Count; i++)
                {
                    Duct duct_i = doc.GetElement(listIds[i]) as Duct;
                    if (duct_i != null)
                    {
                        CreateUnionFiting(doc, duct0, duct_i);
                        duct0 = duct_i;
                    }
                    
                }
            }
        }

    }
}
