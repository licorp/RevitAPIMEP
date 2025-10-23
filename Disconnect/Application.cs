using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace Quoc_MEP
{
    /// <summary>
    /// Lớp Application chính cho MEP Connector add-in
    /// </summary>
    public class Application : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // DEBUGGER: Attach debugger khi khởi động
                #if DEBUG
                System.Diagnostics.Debugger.Launch(); // Sẽ mở dialog để attach debugger
                #endif
                
                // Log để debug UI structure
                LogUIStructure();
                
                // Tạo ribbon tab mới cho MEP Connector
                string tabName = "Licorp";
                
                // Kiểm tra xem tab đã tồn tại chưa
                try
                {
                    application.CreateRibbonTab(tabName);
                }
                catch
                {
                    // Tab có thể đã tồn tại, tiếp tục
                }

                // Tạo ribbon panel, tránh trùng tên panel
                RibbonPanel panel = null;
                foreach (RibbonPanel pnl in application.GetRibbonPanels(tabName))
                {
                    if (pnl.Name == "Modify")
                    {
                        panel = pnl;
                        break;
                    }
                }
                if (panel == null)
                {
                    panel = application.CreateRibbonPanel(tabName, "Modify");
                }
                // DEBUG: Breakpoint ở đây để inspect panel
                System.Diagnostics.Debug.WriteLine($"Panel created: {panel.Name}");
                System.Diagnostics.Debug.WriteLine($"Panel Enabled: {panel.Enabled}");
                DebugUIElement(panel, "RibbonPanel");

                // Tạo SplitButton cho các chức năng kết nối
                SplitButtonData splitButtonData = new SplitButtonData("ConnectSplitButton", "Kết Nối");
                SplitButton splitButton = panel.AddItem(splitButtonData) as SplitButton;
                
                // DEBUG: Breakpoint ở đây để inspect splitButton
                System.Diagnostics.Debug.WriteLine($"SplitButton created: {splitButton.Name}");
                DebugUIElement(splitButton, "SplitButton");

                // Nút chính - Move Connect (chức năng mặc định)
                PushButtonData moveConnectData = new PushButtonData(
                    "MoveConnect",
                    "Kết Nối Hai Đối Tượng",
                    Assembly.GetExecutingAssembly().Location,
                    "MEPConnector.Commands.MoveConnectCommand");

                PushButton moveConnectButton = splitButton.AddPushButton(moveConnectData);
                moveConnectButton.ToolTip = "Di chuyển và kết nối các MEP family";
                moveConnectButton.LongDescription = "Click vào một MEP family đích, sau đó click vào MEP family muốn di chuyển. " +
                    "Family thứ hai sẽ được di chuyển để kết nối với family đầu tiên.";

                // Nút Move Connect Align
                PushButtonData moveConnectAlignData = new PushButtonData(
                    "MoveConnectAlign",
                    "Kết Nối Move Hai Đối Tượng Lại",
                    Assembly.GetExecutingAssembly().Location,
                    "MEPConnector.Commands.MoveConnectAlignCommand");

                PushButton moveConnectAlignButton = splitButton.AddPushButton(moveConnectAlignData);
                moveConnectAlignButton.ToolTip = "Di chuyển, căn chỉnh và kết nối các MEP family";
                moveConnectAlignButton.LongDescription = "Click vào một MEP family đích, sau đó click vào MEP family muốn di chuyển. " +
                    "Family thứ hai sẽ được di chuyển và căn chỉnh để kết nối hoàn hảo với family đầu tiên.";

                // Nút Disconnect thứ tự 1
                PushButtonData disconnectData = new PushButtonData(
                    "Disconnect",
                    "Disconnect",
                    Assembly.GetExecutingAssembly().Location,
                    "MEPConnector.Commands.DisconnectCommand");

                PushButton disconnectButton = splitButton.AddPushButton(disconnectData);
                disconnectButton.ToolTip = "Ngắt kết nối giữa hai MEP family";
                disconnectButton.LongDescription = "Click vào MEP family đầu tiên, sau đó click vào MEP family thứ hai để ngắt kết nối giữa chúng.";

                // Nút Aline Đối Tượng 
                PushButtonData alineData = new PushButtonData(
                    "Aline",
                    "Kết Nối Aline Đối Tượng",
                    Assembly.GetExecutingAssembly().Location,
                    "MEPConnector.Commands.MoveConnectAlignCommand"); // Tạm thời dùng chung command

                PushButton alineButton = splitButton.AddPushButton(alineData);
                alineButton.ToolTip = "Căn chỉnh và kết nối các MEP family";
                alineButton.LongDescription = "Căn chỉnh và kết nối các MEP family với độ chính xác cao.";

                // Đặt nút mặc định cho SplitButton
                splitButton.CurrentButton = moveConnectButton;


                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Lỗi khi load MEP Connector: " + ex.Message + "\n\n" + ex.StackTrace);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            // Cleanup nếu cần thiết
            return Result.Succeeded;
        }

        /// <summary>
        /// Method để log structure của UI - giúp debug và preview
        /// </summary>
        private void LogUIStructure()
        {
            string uiInfo = @"
=== UI STRUCTURE PREVIEW ===
Tab: Licorp
Panel: Modify

SplitButton: [Kết Nối] (Dropdown)
├── Kết Nối Hai Đối Tượng (Default)
├── Kết Nối Move Hai Đối Tượng Lại  
├── Disconnect
└── Kết Nối Aline Đối Tượng

Individual Buttons:
├── Nâng hạ cao độ
├── Copy/Move  
├── Tăng giảm chiều dài
├── Aline Đối Tương
├── Aline Hanger
└── Up&Down

=== END PREVIEW ===
            ";
            
            System.Diagnostics.Debug.WriteLine(uiInfo);
            // Ghi vào file log để dễ đọc
            try
            {
                string logPath = @"C:\temp\MEP_UI_Debug.txt";
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
                System.IO.File.WriteAllText(logPath, uiInfo + Environment.NewLine + DateTime.Now.ToString());
            }
            catch { /* Ignore file write errors */ }
        }

        /// <summary>
        /// Debug helper để inspect UI elements
        /// </summary>
        private void DebugUIElement(object uiElement, string elementName)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"=== DEBUGGING {elementName} ===");
            
            if (uiElement is RibbonPanel panel)
            {
                System.Diagnostics.Debug.WriteLine($"Panel.Name: {panel.Name}");
                System.Diagnostics.Debug.WriteLine($"Panel.Enabled: {panel.Enabled}");
                System.Diagnostics.Debug.WriteLine($"Panel.Visible: {panel.Visible}");
            }
            else if (uiElement is SplitButton splitBtn)
            {
                System.Diagnostics.Debug.WriteLine($"SplitButton.Name: {splitBtn.Name}");
                System.Diagnostics.Debug.WriteLine($"SplitButton.ItemText: {splitBtn.ItemText}");
                System.Diagnostics.Debug.WriteLine($"SplitButton.Enabled: {splitBtn.Enabled}");
            }
            else if (uiElement is PushButton pushBtn)
            {
                System.Diagnostics.Debug.WriteLine($"PushButton.Name: {pushBtn.Name}");
                System.Diagnostics.Debug.WriteLine($"PushButton.ItemText: {pushBtn.ItemText}");
                System.Diagnostics.Debug.WriteLine($"PushButton.ToolTip: {pushBtn.ToolTip}");
                System.Diagnostics.Debug.WriteLine($"PushButton.Enabled: {pushBtn.Enabled}");
            }
            
            System.Diagnostics.Debug.WriteLine($"=== END DEBUG {elementName} ===");
            #endif
        }
    }
}
