using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quoc_MEP.UpDownTool.Event
{
    public class DuctCutDownEvent : IExternalEventHandler
    {
        public MEPUpDownView window { get;set; }

        public void Execute(UIApplication app)
        {
            window.Hide();

            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            Reference r1 = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new DuctFilter(), "Pick first point");
            Reference r2 = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new DuctFilter(), "Pick second point");


            string option = window.Option;
            string angle = window.Angle;
            double offset = window.Offset / 304.8;

            switch(option)
            {
                case "Cut Up":
                    {
                        if (angle == "45°") DuctCutDowUtils.DuctCut45(doc, r1, r2, offset, true);
                        else DuctCutDowUtils.DutCut90(doc, r1, r2, offset, true);
                        break;
                    }
                case "Cut Down":
                    {
                        if (angle == "45°") DuctCutDowUtils.DuctCut45(doc, r1, r2, offset, false);
                        else DuctCutDowUtils.DutCut90(doc, r1, r2, offset, false);
                        break;
                    }
                case "Move Up":
                    {
                        if (angle == "45°") DuctCutDowUtils.DuctMove45(doc, r1, r2, offset, true);
                        else DuctCutDowUtils.DuctMove90(doc, r1, r2, offset, true);
                        break;
                    }
                default:
                    {
                        if (angle == "45°") DuctCutDowUtils.DuctMove45(doc, r1, r2, offset, false);
                        else DuctCutDowUtils.DuctMove90(doc, r1, r2, offset, false);
                        break;
                    }
            }

            window.Show();
        }

        public string GetName()
        {
            return "DuctCutEvent";
        }
    }
}
