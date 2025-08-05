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
            string omsiPath = @"E:\SteamLibrary\steamapps\common\OMSI 2"; // OMSI base path
            string vehiclePath = omsiManager.vehicleName;

            if (string.IsNullOrEmpty(vehiclePath) || vehiclePath == "No vehicle")
            {
                Console.WriteLine("No vehicle found.");
                return null;
            }

            string fullVehicleDir = Path.Combine(omsiPath, Path.GetDirectoryName(vehiclePath));
            Console.WriteLine("Vehicle Directory: " + fullVehicleDir);

            string scriptFolder = FindScriptFolder(fullVehicleDir);
            if (scriptFolder == null)
            {
                Console.WriteLine("No script folder found.");
                return null;
            }

            Console.WriteLine("Script Folder: " + scriptFolder);

            var st = Directory.GetFiles(scriptFolder, "*.txt", SearchOption.AllDirectories);

            foreach (var file in Directory.GetFiles(scriptFolder, "*.txt", SearchOption.AllDirectories))
            {
                
                string[] lines = File.ReadAllLines(file);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().Equals("[const]", StringComparison.OrdinalIgnoreCase))
                    {
                        // Next line should be the constant name
                        if (i + 1 < lines.Length &&
                            lines[i + 1].Trim().Equals(constantName, StringComparison.OrdinalIgnoreCase))
                        {
                            // Next line after that should be the value
                            if (i + 2 < lines.Length)
                            {
                                string valueLine = lines[i + 2].Trim();
                                string firstToken = valueLine.Split(' ')[0]; // In case there are comments

                                if (double.TryParse(firstToken,
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out double value))
                                {
                                    return value;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

    }
}
