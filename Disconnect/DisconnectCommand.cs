using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using q.Utils;

namespace Quoc_MEP
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class DisconnectCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Chọn element đầu tiên
                Reference reference1 = uidoc.Selection.PickObject(ObjectType.Element, 
                    new SelectionHelper.MEPFamilySelectionFilter(), 
                    "Chọn MEP element đầu tiên:");

                Element element1 = doc.GetElement(reference1);

                // Chọn element thứ hai
                Reference reference2 = uidoc.Selection.PickObject(ObjectType.Element, 
                    new SelectionHelper.MEPFamilySelectionFilter(), 
                    "Chọn MEP element thứ hai để ngắt kết nối với element đầu tiên:");

                Element element2 = doc.GetElement(reference2);

                // Kiểm tra xem hai element có được kết nối với nhau không
                if (!ConnectionHelper.AreElementsConnected(element1, element2))
                {
                    message = "Hai element này không được kết nối với nhau.";
                    return Result.Failed;
                }

                // Bắt đầu transaction
                using (Transaction trans = new Transaction(doc, "Ngắt kết nối hai MEP Element"))
                {
                    trans.Start();

                    // Tự động unpin cả hai element nếu cần
                    bool element1Unpinned = ConnectionHelper.UnpinElementIfNeeded(element1);
                    bool element2Unpinned = ConnectionHelper.UnpinElementIfNeeded(element2);

                    // Ngắt kết nối giữa hai element
                    bool disconnected = ConnectionHelper.DisconnectTwoElements(doc, element1, element2);

                    if (disconnected)
                    {
                        trans.Commit();
                        
                        return Result.Succeeded;
                    }
                    else
                    {
                        trans.RollBack();
                        message = "Không thể ngắt kết nối giữa hai element này.";
                        return Result.Failed;
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // Người dùng hủy chọn
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = $"Đã xảy ra lỗi: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}
