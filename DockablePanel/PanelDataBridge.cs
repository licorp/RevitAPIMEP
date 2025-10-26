using System;

namespace Quoc_MEP
{
    /// <summary>
    /// Bridge để truyền data từ Panel sang Commands
    /// </summary>
    public static class PanelDataBridge
    {
        // Flags để biết request từ Panel hay Ribbon
        public static bool IsCalledFromPanel { get; set; } = false;
        
        // Data cho Change Length
        public static double? ChangeLengthValue { get; set; } = null;
        
        // Data cho Rotate
        public static double? RotateAngleValue { get; set; } = null;
        
        /// <summary>
        /// Reset tất cả flags sau khi command thực thi xong
        /// </summary>
        public static void Reset()
        {
            IsCalledFromPanel = false;
            ChangeLengthValue = null;
            RotateAngleValue = null;
        }
        
        /// <summary>
        /// Set data cho Change Length từ Panel
        /// </summary>
        public static void SetChangeLengthData(double lengthMm)
        {
            IsCalledFromPanel = true;
            ChangeLengthValue = lengthMm;
        }
        
        /// <summary>
        /// Set data cho Rotate từ Panel
        /// </summary>
        public static void SetRotateData(double angleDegrees)
        {
            IsCalledFromPanel = true;
            RotateAngleValue = angleDegrees;
        }
    }
}
