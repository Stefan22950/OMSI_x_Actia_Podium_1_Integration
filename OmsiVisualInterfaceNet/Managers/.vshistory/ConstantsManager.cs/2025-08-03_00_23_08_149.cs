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
            if (vehiclePath == "No vehicle")
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

            foreach (var file in Directory.GetFiles(scriptFolder, "*.txt", SearchOption.AllDirectories))
            {
                string[] lines = File.ReadAllLines(file);
                bool insideConstBlock = false;

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();

                    if (line.Equals("[const]", StringComparison.OrdinalIgnoreCase))
                    {
                        insideConstBlock = true;
                        continue;
                    }
                    if (line.StartsWith("[") && !line.Equals("[const]", StringComparison.OrdinalIgnoreCase))
                    {
                        insideConstBlock = false; // End of const block
                    }

                    if (insideConstBlock && line.StartsWith(constantName))
                    {
                        var parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && double.TryParse(parts[1], out double val))
                        {
                            return val;
                        }
                    }
                }
            }
            return null;
        }
    }
}
