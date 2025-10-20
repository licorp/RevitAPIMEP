using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Quoc_MEP.PlaceFamily;
using Autodesk.Revit.UI.Selection;




namespace Quoc_MEP
{
    [Transaction(TransactionMode.Manual)]
    public class PlaceFamilyCmd : IExternalCommand
    {
        [Obsolete]
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            ImportInstance fileCad = null;
            FamilyInstance instance = null;

            try
            {
                //pick file cad
                Reference r1 = uidoc.Selection.PickObject(ObjectType.Element, new CadFilter(), "Pick file cad");
                fileCad = doc.GetElement(r1) as ImportInstance;

                //pick family
                Reference r2 = uidoc.Selection.PickObject(ObjectType.Element, new InstanceFilter(), "Pick a element");
                instance = doc.GetElement(r2) as FamilyInstance;
            }
            catch { }

            if (fileCad != null && instance != null)
            {
                CADLinkType cadLinkType = doc.GetElement(fileCad.GetTypeId()) as CADLinkType;
                string fileName = cadLinkType.Name;

                var listBlocks = PlaceFamilyUtils.GetListBlockCad(cadLinkType);
                var listFamily = PlaceFamilyUtils.GetListFamily(doc, instance);

                PlaceFamilyWindow window = new PlaceFamilyWindow(doc, instance, fileName, listBlocks, listFamily);
                window.ShowDialog();

                if(window.DialogResult == true)
                {
                    string cadBlock = window.BlockName;
                    string familyName =window.FamilyName;
                    string typeName = window.TypeName;
                    string levelName = window.LevelName;
                    double distance = window.Distance / 304.8;


                    Level level = PlaceFamilyUtils.GetLevelByName(doc, levelName);
                    FamilySymbol symbol = PlaceFamilyUtils.GetFamilySymbolByName(doc, instance, familyName, typeName);
                    List<GeometryInstance> listBlockCad = PlaceFamilyUtils.GetListBlockCadByName(cadLinkType, cadBlock);
                    Transform transform = fileCad.GetTransform();


                    using (ProgressBarView bv = new ProgressBarView("Process",listBlockCad.Count))
                    {
                        bv.Show();

                        using (Transaction t = new Transaction(doc, "Place Family"))
                        {
                            t.Start();

                            foreach (GeometryInstance block in listBlockCad)
                            {
                                XYZ cadLocation = block.Transform.Origin;
                                XYZ revitLoction = transform.OfPoint(cadLocation);

                                FamilyInstance familyInstance = doc.Create.NewFamilyInstance(revitLoction, symbol, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                                familyInstance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(distance);

                                if (bv.Update()) break;
                            }

                            t.Commit();
                        }
                    }

                   

                    

                    //doc.Create.NewFamilyInstance()

                }


            }

            
            




            return Result.Succeeded;

        }

    }
}
