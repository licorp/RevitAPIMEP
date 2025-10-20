using System;

namespace Quoc_MEP
{
    /// <summary>
    /// Constants and configuration values for the Rotate Elements addon
    /// </summary>
    public static class Constants
    {
        // UI Constants
        public const string RIBBON_TAB_NAME = "Quoc_MEP";
        public const string RIBBON_PANEL_NAME = "MEP";
        public const string COMMAND_NAME = "RotateElements";
        public const string COMMAND_TEXT = "Rotate Element";
        
        // Default values
        public const double DEFAULT_ANGLE = 90.0;
        public const double AXIS_LENGTH_EXTENSION = 10.0; // feet
        
        // Error messages
        public static class ErrorMessages
        {
            public const string NO_ELEMENTS_SELECTED = "Please select elements to rotate first.\nVui lòng chọn các đối tượng cần xoay trước.";
            public const string NO_AXIS_SELECTED = "Please select a pipe or fitting as rotation axis.\nVui lòng chọn ống hoặc phụ kiện làm trục xoay.";
            public const string INVALID_AXIS = "Unable to determine rotation axis from selected element.\nKhông thể xác định trục xoay từ element đã chọn.";
            public const string ROTATION_FAILED = "Rotation failed: {0}";
            public const string INVALID_ANGLE = "Please enter a valid angle.\nVui lòng nhập góc xoay hợp lệ.";
        }
        
        // Success messages
        public static class Messages
        {
            public const string ROTATION_SUCCESS = "Successfully rotated {0} elements by {1}°";
            public const string ROTATION_UNDONE = "Rotation undone by user";
        }
        
        // Tooltips and descriptions
        public static class Tooltips
        {
            public const string ROTATE_BUTTON = "Rotate selected MEP elements around a specified axis";
            public const string ROTATE_BUTTON_LONG = "Select MEP elements (pipes, ducts, fittings) and rotate them around a chosen axis element. " +
                                                    "The tool provides precise angle control and real-time preview.";
            public const string SELECT_AXIS = "Select a pipe or fitting as rotation axis / Chọn ống hoặc phụ kiện làm trục xoay:";
            public const string CONFIRM_ROTATION = "Di chuyển có đúng hướng không?\nIs the rotation direction correct?";
        }
        
        // MEP Categories that can be selected as axis or rotated
        public static readonly string[] MEP_CATEGORIES = {
            "Pipes",
            "Pipe Fittings", 
            "Ducts",
            "Duct Fittings",
            "Pipe Accessories",
            "Duct Accessories"
        };
    }
}
