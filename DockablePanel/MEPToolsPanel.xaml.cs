using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Quoc_MEP
{
    public partial class MEPToolsPanel : Page, IDockablePaneProvider
    {
        public static Guid PanelGuid = new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
        private static UIApplication _staticUIApp; // Static để giữ reference
        private UIApplication _uiapp;
        private static bool _traceListenerAdded = false;
        
        // External Events
        private ExternalEvent _changeLengthEvent;
        //private ExternalEvent _rotateEvent;
        private LengthChangeEventHandler _changeLengthHandler; // Dùng class từ ChangeLengthcmd.cs
        //private RotateEventHandler _rotateHandler;

        public MEPToolsPanel()
        {
            InitializeComponent();
            
            if (!_traceListenerAdded)
            {
                Trace.Listeners.Add(new DefaultTraceListener());
                _traceListenerAdded = true;
            }
            
            // Tạo Event Handler cho Change Length (dùng class có pick point logic)
            _changeLengthHandler = new LengthChangeEventHandler();
            _changeLengthEvent = ExternalEvent.Create(_changeLengthHandler);
            
            // TODO: Tạo RotateEventHandler khi cần
            //_rotateHandler = new RotateEventHandler();
            //_rotateEvent = ExternalEvent.Create(_rotateHandler);
            
            // Subscribe to Loaded event để lấy UIApplication khi Panel được show
            this.Loaded += OnPanelLoaded;
            
            MEPToolsPanelLogger.Info("MEPToolsPanel initialized");
        }
        
        private void OnPanelLoaded(object sender, RoutedEventArgs e)
        {
            // Try to get UIApplication from Revit context when panel is loaded
            if (_staticUIApp == null)
            {
                try
                {
                    // Get from Revit's UIApplication
                    var uiApp = Autodesk.Revit.ApplicationServices.Application.Create(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);
                    // This won't work directly, we need IExternalCommand context
                    MEPToolsPanelLogger.Warning("Panel loaded but UIApplication not set yet");
                }
                catch (Exception ex)
                {
                    MEPToolsPanelLogger.Warning($"Cannot get UIApplication on load: {ex.Message}");
                }
            }
        }

        public MEPToolsPanel(UIApplication uiapp) : this()
        {
            _uiapp = uiapp;
            _staticUIApp = uiapp; // Lưu vào static
            
            MEPToolsPanelLogger.Info("MEPToolsPanel initialized with UIApplication");
        }
        
        /// <summary>
        /// Public static method để set UIApplication sau khi Panel được tạo
        /// </summary>
        public static void SetUIApplication(UIApplication uiapp)
        {
            _staticUIApp = uiapp;
            MEPToolsPanelLogger.Info("Static UIApplication set");
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            MEPToolsPanelLogger.StartOperation("SetupDockablePane");
            
            data.FrameworkElement = this;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Tabbed,
                MinimumWidth = 300,
                MinimumHeight = 400
            };
            
            MEPToolsPanelLogger.Info("DockablePane configured");
            MEPToolsPanelLogger.EndOperation("SetupDockablePane");
        }

        private void BtnRunChangeLength_Click(object sender, RoutedEventArgs e)
        {
            MEPToolsPanelLogger.StartOperation("ChangeLength");
            
            try
            {
                // Lấy UIApplication từ nhiều nguồn (ưu tiên static > instance > RevitContext)
                UIApplication uiapp = _staticUIApp ?? _uiapp ?? RevitContext.UIApplication;
                
                // Nếu vẫn null, thử lấy từ AddInId
                if (uiapp == null)
                {
                    try
                    {
                        // Get UIApplication from Revit's context
                        var addinId = Autodesk.Windows.ComponentManager.Ribbon?.ActiveTab?.Id;
                        if (addinId != null)
                        {
                            // Try to get from external command data (workaround)
                            MEPToolsPanelLogger.Warning("UIApplication not set, trying to get from context");
                        }
                    }
                    catch { }
                }
                
                if (uiapp == null)
                {
                    MEPToolsPanelLogger.Error("UIApplication is null - Panel opened before initialization");
                    txtChangeLengthStatus.Text = "✗ Please click 'Show MEP Tools Panel' button first";
                    txtChangeLengthStatus.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }
                
                if (uiapp.ActiveUIDocument == null)
                {
                    MEPToolsPanelLogger.Error("No active Revit document");
                    txtChangeLengthStatus.Text = "✗ No active document";
                    txtChangeLengthStatus.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                if (!double.TryParse(txtChangeLengthValue.Text, out double lengthMm))
                {
                    MEPToolsPanelLogger.Error($"Invalid length: {txtChangeLengthValue.Text}");
                    txtChangeLengthStatus.Text = "✗ Invalid length value";
                    txtChangeLengthStatus.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                // Validate length range
                if (lengthMm < 1 || lengthMm > 100000)
                {
                    MEPToolsPanelLogger.Error($"Length out of range: {lengthMm}mm");
                    txtChangeLengthStatus.Text = "✗ Length must be 1-100,000mm";
                    txtChangeLengthStatus.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                // Use ExternalEvent với pick point interaction
                MEPToolsPanelLogger.Info($"Triggering pick point interaction with length: {lengthMm}mm");
                
                _changeLengthHandler.LengthMm = lengthMm;
                _changeLengthHandler.UIDoc = uiapp.ActiveUIDocument;
                _changeLengthHandler.ParentWindow = null; // Panel không cần window reference
                
                // Set callback để update status khi hoàn thành
                _changeLengthHandler.OnCompleted = (success, message) =>
                {
                    // Update UI trên Dispatcher thread
                    Dispatcher.Invoke(() =>
                    {
                        txtChangeLengthStatus.Text = message;
                        txtChangeLengthStatus.Foreground = success 
                            ? System.Windows.Media.Brushes.Green 
                            : System.Windows.Media.Brushes.Orange;
                        
                        MEPToolsPanelLogger.Info($"Operation completed: {message}");
                    });
                };
                
                // Raise event để trigger pick point
                _changeLengthEvent.Raise();
                
                txtChangeLengthStatus.Text = "⏳ Pick point on pipe to extend...";
                txtChangeLengthStatus.Foreground = System.Windows.Media.Brushes.Blue;
                
                MEPToolsPanelLogger.Info("ExternalEvent raised for pick point interaction");
                MEPToolsPanelLogger.EndOperation("ChangeLength");
            }
            catch (Exception ex)
            {
                MEPToolsPanelLogger.Error("Change Length failed", ex);
                txtChangeLengthStatus.Text = $"✗ Error: {ex.Message}";
                txtChangeLengthStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void BtnRunRotate_Click(object sender, RoutedEventArgs e)
        {
            MEPToolsPanelLogger.StartOperation("Rotate");
            
            try
            {
                UIApplication uiapp = _staticUIApp ?? _uiapp;
                
                if (uiapp == null)
                {
                    MEPToolsPanelLogger.Error("UIApplication is null");
                    txtRotateStatus.Text = "✗ Panel not initialized";
                    txtRotateStatus.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }
                
                if (uiapp.ActiveUIDocument == null)
                {
                    MEPToolsPanelLogger.Error("No active Revit document");
                    txtRotateStatus.Text = "✗ No active document";
                    txtRotateStatus.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                if (!double.TryParse(txtRotateAngleValue.Text, out double angleDegrees))
                {
                    MEPToolsPanelLogger.Error($"Invalid angle: {txtRotateAngleValue.Text}");
                    txtRotateStatus.Text = "✗ Invalid angle value";
                    txtRotateStatus.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                var selectedIds = uiapp.ActiveUIDocument.Selection.GetElementIds();
                if (selectedIds.Count == 0)
                {
                    MEPToolsPanelLogger.Warning("No selection");
                    txtRotateStatus.Text = "⚠ Please select elements first";
                    txtRotateStatus.Foreground = System.Windows.Media.Brushes.Orange;
                    return;
                }

                // Set data vào Bridge để command nhận
                PanelDataBridge.SetRotateData(angleDegrees);
                
                // Post command "Rotate"
                RevitCommandId commandId = RevitCommandId.LookupCommandId("CustomCtrl_%CustomCtrl_%Quoc_MEP%Quoc_MEP%Rotate");
                if (commandId != null)
                {
                    uiapp.PostCommand(commandId);
                    MEPToolsPanelLogger.Info($"Posted Rotate command with {angleDegrees}°");
                    
                    txtRotateStatus.Text = $"✓ Command posted for {selectedIds.Count} element(s)";
                    txtRotateStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    MEPToolsPanelLogger.Error("Cannot find Rotate command ID");
                    txtRotateStatus.Text = "✗ Command not found";
                    txtRotateStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
                
                MEPToolsPanelLogger.EndOperation("Rotate");
            }
            catch (Exception ex)
            {
                MEPToolsPanelLogger.Error("Rotate failed", ex);
                txtRotateStatus.Text = $"✗ Error: {ex.Message}";
                txtRotateStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
    }
}
