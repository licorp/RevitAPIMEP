using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;

namespace Quoc_MEP
{
    public partial class RotateWindow : Window
    {
        public double RotationAngle { get; private set; } = double.NaN;
        public bool ShouldProceed { get; private set; } = false;
        public bool IsProcessing { get; private set; } = false;
        private UIApplication _uiApp;

        // Event to notify when user wants to proceed with rotation
        public event EventHandler<RotationRequestEventArgs> RotationRequested;

        public RotateWindow(UIApplication uiApp)
        {
            _uiApp = uiApp;
            InitializeComponent();
            
            // Set Revit as owner window to keep form on top
            SetRevitAsOwner();
            
            // Load last used angle - KHÔNG RESET về 90
            double lastAngle = AngleMemory.GetLastAngle();
            if (lastAngle == 0.0)
            {
                // Chỉ set 90 nếu chưa có giá trị nào được lưu
                angleTextBox.Text = "90";
            }
            else
            {
                // Sử dụng giá trị đã lưu
                angleTextBox.Text = lastAngle.ToString("F2");
            }
            
            angleTextBox.Focus();
            angleTextBox.SelectAll();
        }

        /// <summary>
        /// Set Revit main window as owner to keep this dialog on top
        /// </summary>
        private void SetRevitAsOwner()
        {
            try
            {
                WindowInteropHelper helper = new WindowInteropHelper(this);
                helper.Owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                Trace.WriteLine("Set Revit as owner window successfully");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to set Revit as owner: {ex.Message}");
                // If fails, Topmost property will still keep it on top
            }
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            if (IsProcessing)
            {
                Trace.WriteLine("Already processing rotation request");
                return; // Prevent multiple clicks during processing
            }

            try
            {
                if (double.TryParse(angleTextBox.Text.Trim(), out double angle))
                {
                    RotationAngle = angle;
                    ShouldProceed = true;
                    IsProcessing = true;

                    // Hide form during processing
                    this.Hide();

                    Trace.WriteLine($"User confirmed rotation angle: {angle}° - hiding form for processing");
                    
                    // Fire event to notify parent about rotation request
                    var args = new RotationRequestEventArgs { Angle = angle };
                    RotationRequested?.Invoke(this, args);
                }
                else
                {
                    MessageBox.Show("Please enter a valid number for rotation angle.\nVui lòng nhập số hợp lệ cho góc xoay.", 
                                  "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    angleTextBox.Focus();
                    angleTextBox.SelectAll();
                    return;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error in BtnRun_Click: {ex.Message}");
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                IsProcessing = false;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ShouldProceed = false;
            Trace.WriteLine("User cancelled rotation input");
            this.Hide(); // Hide instead of close to allow reuse
        }

        public void ShowForReuse()
        {
            // Reset state for reuse
            IsProcessing = false;
            ShouldProceed = false;
            RotationAngle = double.NaN;
            
            // Load lại giá trị đã lưu
            double lastAngle = AngleMemory.GetLastAngle();
            if (lastAngle > 0)
            {
                angleTextBox.Text = lastAngle.ToString("F2");
            }
            
            // Simple Show - no dispatcher needed
            if (!this.IsVisible)
            {
                this.Show();
            }
            
            // Bring to front
            BringToFrontAndFocus();
            
            // Focus on angle input
            angleTextBox.Focus();
            angleTextBox.SelectAll();
            
            Trace.WriteLine("Form shown for reuse - angle value preserved");
        }

        /// <summary>
        /// Bring window to front and give it focus - improved method
        /// </summary>
        private void BringToFrontAndFocus()
        {
            try
            {
                // Restore if minimized
                if (this.WindowState == WindowState.Minimized)
                {
                    this.WindowState = WindowState.Normal;
                }
                
                // Activate and bring to front
                this.Activate();
                this.Topmost = true;
                this.Topmost = false; // Flash on top then return to normal
                this.Focus();
                
                Trace.WriteLine("Window brought to front successfully");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error bringing window to front: {ex.Message}");
            }
        }

        public void CompleteProcessing()
        {
            // Called when processing is complete
            IsProcessing = false;
            
            // KHÔNG RESET giá trị - giữ lại để người dùng có thể dùng lại
            RotationAngle = double.NaN;
            ShouldProceed = false;
            
            ShowForReuse(); // Show form again for reuse
            Trace.WriteLine("Processing completed - form ready for reuse with saved angle");
            
            // Call static completion method
            RotateElementsCommand.CompleteRotation();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure window is on top when first loaded
            BringToFrontAndFocus();
            angleTextBox.Focus();
            angleTextBox.SelectAll();
        }

        /// <summary>
        /// Override to ensure form stays visible when activated
        /// </summary>
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // Keep on top when activated
            this.Topmost = true;
        }

        /// <summary>
        /// Allow user to deactivate but stay visible
        /// </summary>
        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            // Still keep on top even when deactivated
            this.Topmost = true;
        }
    }

    // Event args for rotation request
    public class RotationRequestEventArgs : EventArgs
    {
        public double Angle { get; set; }
    }
}
