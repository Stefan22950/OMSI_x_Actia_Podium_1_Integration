using OmsiHook;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OmsiVisualInterfaceNet.Managers
{
    public class ConstantsManager
    {
        private readonly OmsiManager omsiManager;
        static OmsiRoadVehicleInst? playerVehicle;

        public ConstantsManager(OmsiManager omsiManager)
        {
            this.omsiManager = omsiManager;
        }

        static string GetCurrentVehiclePath(string logFile)
        {
            if (!File.Exists(logFile)) return null;

            string[] lines = File.ReadAllLines(logFile);
            string pattern = @"Vehicle loaded:\s*(.+\.bus)";

            foreach (string line in lines)
            {
                var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                    return match.Groups[1].Value;
            }
            return null;
        }

        static string FindScriptFolder(string vehicleDir)
        {
            foreach (var dir in Directory.GetDirectories(vehicleDir, "*", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(dir).ToLower().Contains("script"))
                    return dir;
            }
            return null;
        }

        public double? FindConstantValue(string constantName)
        {
            string omsiPath = @"E:\SteamLibrary\steamapps\common\OMSI 2"; // Your OMSI base path
            string logFile = Path.Combine(omsiPath, "logfile.txt");

            // 1. Get current vehicle path from logfile
            string vehiclePath = omsiManager.vehicleName;
            if (vehiclePath == "No vehicle" || vehiclePath == null)
            {
                Console.WriteLine("No vehicle found in logfile.");
                return 0;
            }

            string fullVehicleDir = Path.Combine(omsiPath, Path.GetDirectoryName(vehiclePath));
            Console.WriteLine("Vehicle Directory: " + fullVehicleDir);

            // 2. Search for 'script' folder inside vehicle folder
            string scriptFolder = FindScriptFolder(fullVehicleDir);
            if (scriptFolder == null)
            {
                Console.WriteLine("No script folder found.");
                return 0;
            }

            Console.WriteLine("Script Folder: " + scriptFolder);

            // Search all files in the "script" folder and subfolders
            foreach (var file in Directory.GetFiles(scriptFolder, "*.*", SearchOption.AllDirectories))
            {
                // Skip files that are not script files (usually .osc or .txt)
                if (!file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    continue;

                bool insideConstBlock = false;
                string[] lines = File.ReadAllLines(file);

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    // Detect start of [const] block
                    if (line.Equals("[const]", StringComparison.OrdinalIgnoreCase))
                    {
                        insideConstBlock = true;
                        continue;
                    }

                    // End of [const] block when another [block] appears
                    if (insideConstBlock && line.StartsWith("[") && !line.Equals("[const]", StringComparison.OrdinalIgnoreCase))
                    {
                        insideConstBlock = false;
                    }

                    if (insideConstBlock && line.StartsWith(constantName, StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double value))
                        {
                            return value; // Found!
                        }
                    }
                }
            }

            return null; // Not found
        }
    }
}
