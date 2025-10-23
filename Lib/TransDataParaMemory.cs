using System;
using System.IO;

namespace Quoc_MEP
{
    /// <summary>
    /// Helper class để lưu trữ cấu hình Trans Data Para giữa các phiên làm việc
    /// </summary>
    public static class TransDataParaMemory
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
                
                return Path.Combine(folder, "trans_data_para_config.txt");
            }
        }

        /// <summary>
        /// Lấy cấu hình đã lưu
        /// </summary>
        public static TransDataParaConfig GetLastConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string[] lines = File.ReadAllLines(ConfigFilePath);
                    if (lines.Length >= 5)
                    {
                        return new TransDataParaConfig
                        {
                            SourceGroup = lines[0],
                            SourceParameter = lines[1],
                            TargetGroup = lines[2],
                            TargetParameter = lines[3],
                            OverwriteExisting = bool.Parse(lines[4])
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error reading Trans Data Para config: {ex.Message}");
            }
            
            return new TransDataParaConfig(); // Return empty config
        }

        /// <summary>
        /// Lưu cấu hình để dùng lần sau
        /// </summary>
        public static void SaveLastConfig(TransDataParaConfig config)
        {
            try
            {
                string[] lines = new string[]
                {
                    config.SourceGroup ?? "",
                    config.SourceParameter ?? "",
                    config.TargetGroup ?? "",
                    config.TargetParameter ?? "",
                    config.OverwriteExisting.ToString()
                };
                
                File.WriteAllLines(ConfigFilePath, lines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error saving Trans Data Para config: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Cấu hình Trans Data Para
    /// </summary>
    public class TransDataParaConfig
    {
        public string SourceGroup { get; set; }
        public string SourceParameter { get; set; }
        public string TargetGroup { get; set; }
        public string TargetParameter { get; set; }
        public bool OverwriteExisting { get; set; }
    }
}
