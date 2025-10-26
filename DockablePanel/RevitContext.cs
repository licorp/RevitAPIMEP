using Autodesk.Revit.UI;

namespace Quoc_MEP
{
    /// <summary>
    /// Static class để lưu trữ Revit context (UIApplication)
    /// </summary>
    public static class RevitContext
    {
        private static UIApplication _uiApplication;

        /// <summary>
        /// Get hoặc set UIApplication
        /// </summary>
        public static UIApplication UIApplication
        {
            get => _uiApplication;
            set
            {
                _uiApplication = value;
                MEPToolsPanelLogger.Info($"RevitContext.UIApplication set: {value != null}");
            }
        }

        /// <summary>
        /// Check nếu UIApplication đã được set
        /// </summary>
        public static bool IsInitialized => _uiApplication != null;
    }
}
