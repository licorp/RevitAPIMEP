using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;

namespace Quoc_MEP
{
    /// <summary>
    /// Cửa sổ WPF để nhập chiều dài mới cho ống/ống gió (có thể tái sử dụng)
    /// WPF window to enter new length for pipe/duct (reusable)
    /// </summary>
    public partial class PipeLengthWindow : Window
    {
        /// <summary>
        /// Giá trị chiều dài được nhập (đơn vị: mm)
        /// Entered length value (unit: mm)
        /// </summary>
        public double LengthValue { get; private set; } = 0;

        /// <summary>
        /// Cho biết user có muốn tiếp tục thực hiện không
        /// Indicates if user wants to proceed
        /// </summary>
        public bool ShouldProceed { get; private set; } = false;

        /// <summary>
        /// Đang xử lý request
        /// Currently processing request
        /// </summary>
        public bool IsProcessing { get; private set; } = false;

        private UIApplication _uiApp;

        // Event để thông báo khi user muốn thực hiện thay đổi chiều dài
        // Event to notify when user wants to change length
        public event EventHandler<LengthChangeRequestEventArgs> LengthChangeRequested;

        public PipeLengthWindow(UIApplication uiApp)
        {
            _uiApp = uiApp;
            InitializeComponent();
            
            // Set Revit as owner window to keep form on top
            SetRevitAsOwner();

            // Focus vào ô nhập liệu khi mở cửa sổ
            // Focus on input field when window opens
            lengthTextBox.Focus();
            lengthTextBox.SelectAll();

            // Cho phép nhấn Enter để chạy lệnh
            // Allow pressing Enter to run command
            lengthTextBox.KeyDown += (sender, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    BtnOK_Click(sender, e);
                }
            };
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

        /// <summary>
        /// Reset window state để tái sử dụng
        /// Reset window state for reuse
        /// </summary>
        public void ResetState()
        {
            ShouldProceed = false;
            IsProcessing = false;
            lengthTextBox.Focus();
            lengthTextBox.SelectAll();
        }

        /// <summary>
        /// Xử lý sự kiện khi nhấn nút OK
        /// Handle OK button click event
        /// </summary>
        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            if (IsProcessing)
            {
                Trace.WriteLine("Already processing length change request");
                return; // Prevent multiple clicks during processing
            }

            try
            {
                string inputText = lengthTextBox.Text.Trim();

                if (string.IsNullOrEmpty(inputText))
                {
                    MessageBox.Show(
                        "Vui lòng nhập giá trị chiều dài.\nPlease enter a length value.",
                        "Lỗi / Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Tính toán biểu thức (hỗ trợ +, -, *, /)
                // Calculate expression (supports +, -, *, /)
                double result = EvaluateExpression(inputText);

                // Cập nhật giá trị đã tính toán vào TextBox
                // Update calculated value to TextBox
                lengthTextBox.Text = result.ToString("0.##");

                // Kiểm tra giá trị hợp lệ
                // Validate value
                if (result < 1 || result > 100000)
                {
                    MessageBox.Show(
                        "Chiều dài phải từ 1mm đến 100,000mm.\nLength must be between 1mm and 100,000mm.",
                        "Lỗi / Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                LengthValue = result;
                ShouldProceed = true;
                IsProcessing = true;

                // Hide form during processing (thay vì Close)
                this.Hide();

                Trace.WriteLine($"Length change requested: {LengthValue} mm");

                // Raise event để thông báo có request mới
                // Raise event to notify new request
                LengthChangeRequested?.Invoke(this, new LengthChangeRequestEventArgs 
                { 
                    Length = LengthValue 
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi: {ex.Message}\nError: {ex.Message}",
                    "Lỗi / Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                IsProcessing = false;
            }
        }

        /// <summary>
        /// Xử lý sự kiện khi nhấn nút Cancel
        /// Handle Cancel button click event
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ShouldProceed = false;
            this.Hide(); // Hide thay vì Close để có thể show lại
            Trace.WriteLine("Length change cancelled by user");
        }

        /// <summary>
        /// Xử lý khi window đóng
        /// Handle window closing
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Không cho đóng window, chỉ hide
            // Don't close window, just hide it
            e.Cancel = true;
            this.Hide();
            Trace.WriteLine("Window close prevented, hiding instead");
        }

        /// <summary>
        /// Hiển thị window và reset state
        /// Show window and reset state
        /// </summary>
        public new void Show()
        {
            ResetState();
            base.Show();
            lengthTextBox.Focus();
        }

        /// <summary>
        /// Tính toán biểu thức toán học đơn giản
        /// Calculate simple math expression
        /// Hỗ trợ: +, -, *, / (không hỗ trợ dấu ngoặc)
        /// Supports: +, -, *, / (no parentheses support)
        /// </summary>
        private double EvaluateExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentException("Vui lòng nhập giá trị / Please enter a value");
            }

            // Loại bỏ khoảng trắng và thay thế dấu phẩy bằng dấu chấm
            // Remove whitespace and replace comma with dot
            expression = expression.Replace(" ", "").Replace(",", ".");

            // Kiểm tra xem có phải là số đơn giản không
            // Check if it's a simple number
            if (double.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture, out double simpleResult))
            {
                return simpleResult;
            }

            // Xử lý biểu thức phức tạp
            // Handle complex expression
            return EvaluateExpressionRecursive(expression);
        }

        /// <summary>
        /// Tính toán biểu thức đệ quy
        /// Calculate expression recursively
        /// </summary>
        private double EvaluateExpressionRecursive(string expression)
        {
            // Xử lý phép cộng
            // Handle addition
            if (expression.Contains("+"))
            {
                string[] parts = expression.Split('+');
                double sum = 0;
                foreach (string part in parts)
                {
                    sum += EvaluateExpressionRecursive(part);
                }
                return sum;
            }

            // Xử lý phép trừ (chỉ tách ở dấu trừ không phải ở đầu)
            // Handle subtraction (only split at minus that's not at the beginning)
            int minusIndex = expression.LastIndexOf('-');
            if (minusIndex > 0) // Không phải ở đầu chuỗi
            {
                string leftPart = expression.Substring(0, minusIndex);
                string rightPart = expression.Substring(minusIndex + 1);
                return EvaluateExpressionRecursive(leftPart) - EvaluateExpressionRecursive(rightPart);
            }

            // Xử lý phép nhân
            // Handle multiplication
            if (expression.Contains("*"))
            {
                string[] parts = expression.Split('*');
                double product = 1;
                foreach (string part in parts)
                {
                    product *= EvaluateExpressionRecursive(part);
                }
                return product;
            }

            // Xử lý phép chia
            // Handle division
            if (expression.Contains("/"))
            {
                string[] parts = expression.Split('/');
                if (parts.Length < 2)
                {
                    throw new ArgumentException("Biểu thức không hợp lệ / Invalid expression");
                }

                double result = EvaluateExpressionRecursive(parts[0]);
                for (int i = 1; i < parts.Length; i++)
                {
                    double divisor = EvaluateExpressionRecursive(parts[i]);
                    if (Math.Abs(divisor) < 0.0001)
                    {
                        throw new DivideByZeroException("Không thể chia cho 0 / Cannot divide by zero");
                    }
                    result /= divisor;
                }
                return result;
            }

            // Nếu không có phép toán nào, parse trực tiếp
            // If no operation found, parse directly
            if (double.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture, out double finalResult))
            {
                return finalResult;
            }

            throw new ArgumentException($"Biểu thức không hợp lệ / Invalid expression: {expression}");
        }
    }

    /// <summary>
    /// Event arguments cho length change request
    /// Event arguments for length change request
    /// </summary>
    public class LengthChangeRequestEventArgs : EventArgs
    {
        public double Length { get; set; }
    }
}
