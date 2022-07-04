using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Data;
using HaruhiHeiretsuLib.Graphics;
using HaruhiHeiretsuLib.Strings.Events;
using HaruhiHeiretsuLib.Strings.Scripts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ParallelLoops
{
    [InitializeOnLoad]
    public class GlobalSettings
    {
        public const int MapDefFileIndex = 58;

        public static string HeiretsuPath { get; set; }
        public static string BlenderPath { get; set; }
        public static BinArchive<DataFile> DatBin { get; set; }
        public static BinArchive<EventFile> EvtBin { get; set; }
        public static BinArchive<GraphicsFile> GrpBin { get; set; }
        public static BinArchive<ScriptFile> ScrBin { get; set; }
        
        private static string AppDataDirectory = "Assets/ParallelLoops";
        private static string SettingsFile = Path.Combine(AppDataDirectory, "globalsettings.ini");

        static GlobalSettings()
        {
            if (!LoadSettings())
            {
                GlobalSettingsWindow globalSettingsWindow = EditorWindow.CreateWindow<GlobalSettingsWindow>();
                globalSettingsWindow.Show();
            }
        }

        public static bool LoadSettings()
        {            
            if (File.Exists(SettingsFile))
            {
                string[] settingsLines = File.ReadAllLines(SettingsFile);

                foreach (string line in settingsLines)
                {
                    if (line.StartsWith("HeiretsuPath", StringComparison.OrdinalIgnoreCase))
                    {
                        HeiretsuPath = line.Split('=')[1].Trim();
                    }
                    else if (line.StartsWith("BlenderPath"))
                    {
                        BlenderPath = line.Split("=")[1].Trim();
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static void SaveSettings()
        {
            List<string> settings = new();
            settings.Add($"HeiretsuPath={HeiretsuPath}");
            settings.Add($"BlenderPath={BlenderPath}");

            File.WriteAllLines(SettingsFile, settings);
        }
    }
}
