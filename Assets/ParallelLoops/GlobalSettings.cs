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
        public const int CameraDataFileIndex = 36;
        public const int MapDefFileIndex = 58;

        public static string WorkspacePath => "ParallelLoopsWorkspace";
        public static string HeiretsuPath { get; set; }
        public static string BlenderPath { get; set; }
        public static List<GraphicsFile> SgeCache { get; set; }
        
        private static string AppDataDirectory = "Assets/ParallelLoops";
        private static string SettingsFile = Path.Combine(AppDataDirectory, "globalsettings.ini");

        static GlobalSettings()
        {
            if (!LoadSettings())
            {
                GlobalSettingsWindow.ShowExample();
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

                if (string.IsNullOrEmpty(HeiretsuPath) || string.IsNullOrEmpty(BlenderPath))
                {
                    return false;
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
            List<string> settings = new()
            {
                $"HeiretsuPath={HeiretsuPath}",
                $"BlenderPath={BlenderPath}"
            };

            File.WriteAllLines(SettingsFile, settings);
        }

        public static void SetUpWorkspace()
        {
            if (Directory.Exists(WorkspacePath))
            {
                if (EditorUtility.DisplayDialog("Overwrite Workspace", "You have changed your Heiretsu path -- would you like to overwrite your workspace with those game files?", "Yes", "No"))
                {
                    Directory.Delete(WorkspacePath, true);
                }
                else
                {
                    return;
                }
            }

            try
            {
                Directory.CreateDirectory(WorkspacePath);

                EditorUtility.DisplayProgressBar("Setting Up Workspace", "Loading dat.bin...", 0 / 100f);
                BinArchive<DataFile> datBin = BinArchive<DataFile>.FromFile(Path.Combine(HeiretsuPath, "dat.bin"));
                EditorUtility.DisplayProgressBar("Setting Up Workspace", "Loading evt.bin...", 1 / 100f);
                BinArchive<EventFile> evtBin = BinArchive<EventFile>.FromFile(Path.Combine(HeiretsuPath, "evt.bin"));
                EditorUtility.DisplayProgressBar("Setting Up Workspace", "Loading grp.bin...", 3 / 100f);
                BinArchive<GraphicsFile> grpBin = BinArchive<GraphicsFile>.FromFile(Path.Combine(HeiretsuPath, "grp.bin"));
                EditorUtility.DisplayProgressBar("Setting Up Workspace", "Loading scr.bin...", 8 / 100f);
                BinArchive<ScriptFile> scrBin = BinArchive<ScriptFile>.FromFile(Path.Combine(HeiretsuPath, "scr.bin"));

                Directory.CreateDirectory(Path.Combine(WorkspacePath, "dat"));
                foreach (DataFile file in datBin.Files)
                {
                    File.WriteAllBytes(Path.Combine(WorkspacePath, "dat", $"{file.Index:D4}.bin"), file.Data.ToArray());
                    EditorUtility.DisplayProgressBar("Setting Up Workspace", "Unpacking dat.bin...", (10 + 10 * (file.Index / (float)datBin.Files.Count)) / 100f);
                }

                Directory.CreateDirectory(Path.Combine(WorkspacePath, "evt"));
                foreach (EventFile file in evtBin.Files)
                {
                    File.WriteAllBytes(Path.Combine(WorkspacePath, "evt", $"{ file.Index:D4}.bin"), file.Data.ToArray());
                    EditorUtility.DisplayProgressBar("Setting Up Workspace", "Unpacking evt.bin...", (20 + 15 * (file.Index / (float)evtBin.Files.Count)) / 100f);
                }

                Directory.CreateDirectory(Path.Combine(WorkspacePath, "grp"));
                EditorUtility.DisplayProgressBar("Setting Up Workspace", "Resolving graphics file names...", 35 / 100f);
                byte[] graphicsFileNameMap = datBin.Files.First(f => f.Index == 8).GetBytes();
                int numGraphicsFiles = BitConverter.ToInt32(graphicsFileNameMap.Skip(0x10).Take(4).Reverse().ToArray());
                Dictionary<int, string> graphicsIndexToNameMap = new();
                for (int i = 0; i < numGraphicsFiles; i++)
                {
                    graphicsIndexToNameMap.Add(BitConverter.ToInt32(graphicsFileNameMap.Skip(0x14 * (i + 1)).Take(4).Reverse().ToArray()), Encoding.ASCII.GetString(graphicsFileNameMap.Skip(0x14 * (i + 1) + 0x04).TakeWhile(b => b != 0x00).ToArray()));
                }
                foreach (GraphicsFile file in grpBin.Files)
                {
                    file.TryResolveName(graphicsIndexToNameMap);
                    string fileNameComponent = string.IsNullOrEmpty(file.Name) ? "" : $"_{file.Name}";
                    File.WriteAllBytes(Path.Combine(WorkspacePath, "grp", $"{file.Index:D4}{fileNameComponent}.{file.FileType}"), file.Data.ToArray());
                    EditorUtility.DisplayProgressBar("Setting Up Workspace", "Unpacking grp.bin...", (36 + 54 * (file.Index / (float)grpBin.Files.Count)) / 100f);
                }

                Directory.CreateDirectory(Path.Combine(WorkspacePath, "scr"));
                byte[] scriptNameList = scrBin.Files[0].GetBytes();
                List<string> scriptNames = ScriptFile.ParseScriptListFile(scriptNameList);
                Dictionary<int, string> scriptIndexToNameMap = scriptNames.ToDictionary(keySelector: n => scriptNames.IndexOf(n) + 1);
                foreach (ScriptFile file in scrBin.Files)
                {
                    file.Name = scriptIndexToNameMap[file.Index];
                    File.WriteAllBytes(Path.Combine(WorkspacePath, "scr", $"{file.Index:D4}_{file.Name}.bin"), file.Data.ToArray());
                    EditorUtility.DisplayProgressBar("Setting Up Workspace", "Unpacking scr.bin...", (10 + 90 * (file.Index / (float)scrBin.Files.Count)) / 100f);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
