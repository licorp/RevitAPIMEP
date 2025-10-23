using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;


namespace Quoc_MEP
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MoveConnectCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Bước 1: Chọn MEP family đích (destination)
                Reference destRef = uidoc.Selection.PickObject(ObjectType.Element, 
                    new SelectionHelper.MEPFamilySelectionFilter(), 
                    "Chọn MEP family đích");

                if (destRef == null)
                {
                    message = "Không chọn được MEP family đích";
                    return Result.Cancelled;
                }

                Element destElement = doc.GetElement(destRef);

                // Bước 2: Chọn MEP family nguồn (source - sẽ được di chuyển)
                Reference sourceRef = uidoc.Selection.PickObject(ObjectType.Element, 
                    new SelectionHelper.MEPFamilySelectionFilter(), 
                    "Chọn MEP family muốn di chuyển");

                if (sourceRef == null)
                {
                    message = "Không chọn được MEP family nguồn";
                    return Result.Cancelled;
                }

                Element sourceElement = doc.GetElement(sourceRef);

                // Kiểm tra nếu chọn cùng một element
                if (destElement.Id == sourceElement.Id)
                {
                    message = "Không thể kết nối element với chính nó!";
                    return Result.Failed;
                }

                // Bước 3: Thực hiện kết nối
                using (Transaction trans = new Transaction(doc, "Move Connect MEP"))
                {
                    trans.Start();

                    try
                    {
                        // Unpin các element nếu cần
                        ConnectionHelper.UnpinElementIfPinned(doc, sourceElement);
                        ConnectionHelper.UnpinElementIfPinned(doc, destElement);

                        // Thực hiện move và connect
                        bool success = ConnectionHelper.MoveAndConnect(doc, sourceElement, destElement);

                        if (success)
                        {
                            trans.Commit();
                            return Result.Succeeded;
                        }
                        else
                        {
                            trans.RollBack();
                            message = "Không thể kết nối các MEP family. Vui lòng kiểm tra lại.";
                            return Result.Failed;
                        }
                    }
                    catch (Exception ex)
                    {
                        trans.RollBack();
                        message = $"Lỗi khi thực hiện kết nối: {ex.Message}";
                        return Result.Failed;
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                message = "Thao tác bị hủy bỏ";
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = $"Lỗi không mong muốn: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}
