using System;
using System.IO;

namespace Quoc_MEP
{
    /// <summary>
    /// Helper class to persist the last used rotation angle
    /// </summary>
    public static class AngleMemory
    {
        private static string ConfigFilePath
        {
            get
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folder = Path.Combine(appData, "QuocMEPAddin");
                
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                
                return Path.Combine(folder, "last_angle.txt");
            }
        }

        /// <summary>
        /// Get the last used angle, defaults to 0.0
        /// </summary>
        public static double GetLastAngle()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string content = File.ReadAllText(ConfigFilePath);
                    if (double.TryParse(content, out double angle))
                    {
                        return angle;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error reading last angle: {ex.Message}");
            }
            
            return 0.0;
        }

        /// <summary>
        /// Save the angle for next time
        /// </summary>
        public static void SaveLastAngle(double angle)
        {
            try
            {
                File.WriteAllText(ConfigFilePath, angle.ToString("F2"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error saving last angle: {ex.Message}");
            }
        }
    }
}
