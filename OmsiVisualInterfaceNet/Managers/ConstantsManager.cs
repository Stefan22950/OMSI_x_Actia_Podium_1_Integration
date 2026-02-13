using OmsiVisualInterfaceNet.Managers.SolarisIII;

namespace OmsiVisualInterfaceNet.Managers
{
    public class ConstantsManager
    {
        private readonly IOmsiManager omsiManager;
        public ConstantsManager(IOmsiManager omsiManager)
        {
            this.omsiManager = omsiManager;
        }

        static List<string> FindScriptFolder(string vehicleDir)
        {
            List<string> list = new List<string>();

            foreach (var dir in Directory.GetDirectories(vehicleDir, "*", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(dir).ToLower().Contains("script"))
                    list.Add(dir);
            }
            return list;
        }

        public double? FindConstantValue(string constantName)
        {
            string omsiPath = @"E:\SteamLibrary\steamapps\common\OMSI 2";
            string vehiclePath = omsiManager.vehicleName;

            if (string.IsNullOrEmpty(vehiclePath) || vehiclePath == "No vehicle")
            {
                Console.WriteLine("No vehicle found.");
                return null;
            }

            string fullVehicleDir = Path.Combine(omsiPath, Path.GetDirectoryName(vehiclePath));
            string busFile = Path.Combine(omsiPath, vehiclePath);
            Console.WriteLine("Vehicle Directory: " + fullVehicleDir);

            List<string> scriptFolders = FindScriptFolder(fullVehicleDir);

            string[] busFileLines = File.ReadAllLines(busFile);

            foreach(var scriptFolder in scriptFolders)
            {
                foreach (var file in Directory.GetFiles(scriptFolder, "*.txt", SearchOption.AllDirectories))
                {
                    string[] lines = File.ReadAllLines(file);
                    string scriptFileName = Path.GetFileName(file);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Trim().Equals("[const]", StringComparison.OrdinalIgnoreCase))
                        {
                            if (i + 1 < lines.Length &&
                                lines[i + 1].Trim().Equals(constantName, StringComparison.OrdinalIgnoreCase))
                            {
                                if (i + 2 < lines.Length)
                                {
                                    string valueLine = lines[i + 2].Trim();
                                    string firstToken = valueLine.Split(' ')[0];

                                    if (double.TryParse(firstToken,
                                        System.Globalization.NumberStyles.Any,
                                        System.Globalization.CultureInfo.InvariantCulture,
                                        out double value))
                                    {
                                        bool scriptReferenced = busFileLines
                                            .Any(line => line.IndexOf(scriptFileName, StringComparison.OrdinalIgnoreCase) >= 0);

                                        if (scriptReferenced)
                                        {
                                            Console.WriteLine($"Constant found in: {scriptFileName}");
                                            return value;
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Skipping {scriptFileName} (not referenced in .bus file)");
                                        }
                                    }
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
