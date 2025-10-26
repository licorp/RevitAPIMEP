using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ricaun.Revit.UI.StatusBar;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Quoc_MEP
{
    /// <summary>
    /// Command để demo các tính năng của ricaun.Revit.UI.StatusBar
    /// Hiển thị progress bar trên StatusBar của Revit
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class StatusBarDemoCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Demo 1: Simple progress with number of iterations
                DemoSimpleProgress();

                // Demo 2: Progress with elements collection
                DemoElementsProgress(doc);

                // Demo 3: Manual progress control
                DemoManualProgress(doc);

                // Demo 4: Show Balloon notification
                // BalloonUtils.Show("StatusBar Demo completed!", "Quoc_MEP Tools"); // Requires additional package

                TaskDialog.Show("Success", "Đã hoàn thành demo StatusBar!\nCheck StatusBar ở dưới màn hình Revit.");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Error", $"Lỗi: {ex.Message}");
                return Result.Failed;
            }
        }

        /// <summary>
        /// Demo 1: Simple progress với số lần lặp
        /// </summary>
        private void DemoSimpleProgress()
        {
            int repeat = 50;
            RevitProgressBarUtils.Run("Demo Simple Progress", repeat, (i) =>
            {
                // Simulate work
                System.Threading.Thread.Sleep(20);
                System.Diagnostics.Trace.WriteLine($"Processing iteration {i}");
            });
        }

        /// <summary>
        /// Demo 2: Progress với collection của elements
        /// </summary>
        private void DemoElementsProgress(Document doc)
        {
            // Lấy tất cả các walls trong document
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            List<Element> walls = collector
                .OfClass(typeof(Wall))
                .WhereElementIsNotElementType()
                .Take(20) // Giới hạn 20 elements để demo
                .ToList();

            if (walls.Count > 0)
            {
                RevitProgressBarUtils.Run("Processing Walls", walls, (wall) =>
                {
                    // Simulate processing
                    System.Threading.Thread.Sleep(50);
                    string wallName = wall.Name ?? "Unnamed Wall";
                    System.Diagnostics.Trace.WriteLine($"Processing: {wallName} - Id: {wall.Id}");
                });
            }
        }

        /// <summary>
        /// Demo 3: Manual control progress bar
        /// </summary>
        private void DemoManualProgress(Document doc)
        {
            // Lấy một số elements để process
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            List<Element> elements = collector
                .OfCategory(BuiltInCategory.OST_MEPSpaces)
                .WhereElementIsNotElementType()
                .Take(10)
                .ToList();

            // Nếu không có spaces, lấy bất kỳ element nào
            if (elements.Count == 0)
            {
                elements = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .Take(10)
                    .ToList();
            }

            // Sử dụng using để tự động dispose progress bar
            using (var progressBar = new RevitProgressBar())
            {
                progressBar.SetCurrentOperation("Manual Progress Demo");
                
                foreach (var element in elements)
                {
                    // Increment progress
                    progressBar.Increment();
                    
                    // Do some work
                    System.Threading.Thread.Sleep(100);
                    System.Diagnostics.Trace.WriteLine($"Manual processing: {element.Id}");
                }
            }
        }

        /// <summary>
        /// Demo 4: Progress bar trong một vòng lặp phức tạp hơn
        /// </summary>
        private void DemoComplexProgress(Document doc)
        {
            using (var progressBar = new RevitProgressBar())
            {
                // Stage 1: Collect elements
                progressBar.SetCurrentOperation("Stage 1: Collecting Elements");
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                var elements = collector.WhereElementIsNotElementType().ToList();
                
                for (int i = 0; i < 20; i++)
                {
                    progressBar.Increment();
                    System.Threading.Thread.Sleep(30);
                }

                // Stage 2: Process elements
                progressBar.SetCurrentOperation("Stage 2: Processing Elements");
                foreach (var element in elements.Take(30))
                {
                    progressBar.Increment();
                    System.Threading.Thread.Sleep(20);
                }

                // Stage 3: Finalize
                progressBar.SetCurrentOperation("Stage 3: Finalizing");
                for (int i = 0; i < 10; i++)
                {
                    progressBar.Increment();
                    System.Threading.Thread.Sleep(50);
                }
            }
        }
    }
}
