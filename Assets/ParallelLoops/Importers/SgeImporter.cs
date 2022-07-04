using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Data;
using HaruhiHeiretsuLib.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace ParallelLoops.Importers
{
    public class SgeImporter
    {
        public static void CreateFbx(params string[] modelNames)
        {
            if (string.IsNullOrEmpty(GlobalSettings.HeiretsuPath) || string.IsNullOrEmpty(GlobalSettings.BlenderPath))
            {
                UnityEngine.Debug.LogError("Go into Parallel Loops > Settings and set the Heiretsu Game Directory and Blender Path.");
                return;
            }

            string pythonSgeScript = Path.GetFullPath("Assets/ParallelLoops/Importers/sge_import.py");

            var outputFbxs = new List<string>();

            string[] files = Directory.GetFiles(Path.Combine(GlobalSettings.WorkspacePath, "grp"));

            if (GlobalSettings.SgeCache is null)
            {
                GlobalSettings.SgeCache = new();
                foreach (string file in files)
                {
                    if (Path.GetExtension(file).Equals(".sge", StringComparison.OrdinalIgnoreCase))
                    {
                        GraphicsFile sgeFile = new();
                        sgeFile.Initialize(File.ReadAllBytes(file), 0);
                        GlobalSettings.SgeCache.Add(sgeFile);
                        sgeFile.Sge.ResolveTextures(Path.GetFileNameWithoutExtension(file)[5..], files);
                    }
                }
            }

            string sgeDir = Path.Combine(Path.GetTempPath(), "SGEs");
            if (!Directory.Exists(sgeDir))
            {
                Directory.CreateDirectory(sgeDir);
            }

            if (!Directory.Exists("Assets/Resources/Models"))
            {
                Directory.CreateDirectory("Assets/Resources/Models");
            }

            string[] existingModels = Directory.GetFiles("Assets/Resources/Models").Select(f => Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(f))).ToArray(); // twice to strip .sge.fbx
            modelNames = modelNames.Where(m => existingModels.All(e => m != e)).ToArray();

            foreach (string modelName in modelNames)
            {
                string filePath = Path.Combine(sgeDir, $"{modelName}.sge");
                File.WriteAllText($"{filePath}.json", GlobalSettings.SgeCache.First(g => g.Sge.Name == modelName).Sge.DumpJson());

                var blender = new Process() { StartInfo = new ProcessStartInfo(GlobalSettings.BlenderPath, $"--background -noaudio -P \"{pythonSgeScript}\" \"{filePath}.json\" fbx") };
                blender.Start();
                blender.WaitForExit();
                outputFbxs.Add($"{filePath}.fbx");
            }
            if (!Directory.Exists("Assets/Resources/Textures"))
            {
                Directory.CreateDirectory("Assets/Resources/Textures");
            }

            string[] existingTextures = Directory.GetFiles("Assets/Resources/Textures");
            string tempTexDir = Path.Combine(Path.GetTempPath(), "ParallelLoops_Tex_Import");

            foreach (string outputFbx in outputFbxs)
            {
                string assetPath = Path.Combine("Assets/Resources/Models", Path.GetFileName(outputFbx));
                File.Copy(outputFbx, assetPath, overwrite: true);
                File.Delete(outputFbx);
                AssetDatabase.ImportAsset(assetPath);
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                ModelImporter modelImporter = importer as ModelImporter;
                modelImporter.ExtractTextures(tempTexDir);
                foreach (string texFile in Directory.GetFiles(tempTexDir))
                {
                    File.Copy(texFile, Path.Combine("Assets/Resources/Textures", Path.GetFileName(texFile)), overwrite: true);
                    File.Delete(texFile);
                }
            }

            string[] newTextures = Directory.GetFiles("Assets/Resources/Textures").Where(t => existingTextures.All(e => t != e)).ToArray();
            
            foreach (string newTexture in newTextures)
            {
                AssetDatabase.ImportAsset(Path.Combine("Assets/Resources/Textures", Path.GetFileName(newTexture)));
            }
        }
    }
}
