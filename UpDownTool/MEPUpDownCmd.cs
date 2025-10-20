using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Quoc_MEP.UpDownTool;

namespace Quoc_MEP
{
    [Transaction(TransactionMode.Manual)]
    public class MEPUpDownCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var window = new MEPUpDownView();
            window.Show();
            return Result.Succeeded;
        }
    }
}
