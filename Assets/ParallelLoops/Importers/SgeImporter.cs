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
                UnityEngine.Debug.Log("Go into Parallel Loops > Settings and set the Heiretsu Game Directory and Blender Path.");
            }

            string pythonSgeScript = Path.GetFullPath("Assets/ParallelLoops/Importers/sge_import.py");

            var outputFbxs = new List<string>();

            if (GlobalSettings.GrpBin is null)
            {
                UnityEngine.Debug.Log("Loading grp.bin for the first time this session...");
                GlobalSettings.GrpBin = BinArchive<GraphicsFile>.FromFile(Path.Combine(GlobalSettings.HeiretsuPath, "grp.bin"));
            }
            if (GlobalSettings.DatBin is null)
            {
                UnityEngine.Debug.Log("Loading dat.bin for the first time this session...");
                GlobalSettings.DatBin = BinArchive<DataFile>.FromFile(Path.Combine(GlobalSettings.HeiretsuPath, "dat.bin"));
            }

            byte[] graphicsFileNameMap = GlobalSettings.DatBin.Files.First(f => f.Index == 8).GetBytes();
            int numGraphicsFiles = BitConverter.ToInt32(graphicsFileNameMap.Skip(0x10).Take(4).Reverse().ToArray());

            var indexToNameMap = new Dictionary<int, string>();
            for (int i = 0; i < numGraphicsFiles; i++)
            {
                indexToNameMap.Add(BitConverter.ToInt32(graphicsFileNameMap.Skip(0x14 * (i + 1)).Take(4).Reverse().ToArray()), Encoding.ASCII.GetString(graphicsFileNameMap.Skip(0x14 * (i + 1) + 0x04).TakeWhile(b => b != 0x00).ToArray()));
            }

            foreach (GraphicsFile file in GlobalSettings.GrpBin.Files)
            {
                file.TryResolveName(indexToNameMap);
            }
            foreach (GraphicsFile file in GlobalSettings.GrpBin.Files)
            {
                if (file.FileType == GraphicsFile.GraphicsFileType.SGE)
                {
                    file.Sge.ResolveTextures(file.Name, GlobalSettings.GrpBin.Files);
                }
            }

            string sgeDir = Path.Combine(Path.GetTempPath(), "SGEs");
            if (!Directory.Exists(sgeDir))
            {
                Directory.CreateDirectory(sgeDir);
            }

            if (!Directory.Exists("Assets/Models"))
            {
                Directory.CreateDirectory("Assets/Models");
            }

            string[] existingModels = Directory.GetFiles("Assets/Models").Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();
            modelNames = modelNames.Where(m => existingModels.All(e => m != e)).ToArray();

            foreach (string modelName in modelNames)
            {
                string filePath = Path.Combine(sgeDir, $"{modelName}.sge");
                File.WriteAllText($"{filePath}.json", GlobalSettings.GrpBin.Files.First(g => g.Name == modelName).Sge.DumpJson());

                var blender = new Process() { StartInfo = new ProcessStartInfo(GlobalSettings.BlenderPath, $"--background -noaudio -P \"{pythonSgeScript}\" \"{filePath}.json\" fbx") };
                blender.Start();
                blender.WaitForExit();
                outputFbxs.Add($"{filePath}.fbx");
            }
            if (!Directory.Exists("Assets/Textures"))
            {
                Directory.CreateDirectory("Assets/Textures");
            }

            string[] existingTextures = Directory.GetFiles("Assets/Textures");

            foreach (string outputFbx in outputFbxs)
            {
                string assetPath = Path.Combine("Assets/Models", Path.GetFileName(outputFbx));
                File.Move(outputFbx, assetPath);
                AssetDatabase.ImportAsset(assetPath);
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                ModelImporter modelImporter = importer as ModelImporter;
                modelImporter.ExtractTextures("Assets/Textures");
            }

            string[] newTextures = Directory.GetFiles("Assets/Textures").Where(t => existingTextures.All(e => t != e)).ToArray();
            
            foreach (string newTexture in newTextures)
            {
                AssetDatabase.ImportAsset(Path.Combine("Assets/Textures", Path.GetFileName(newTexture)));
            }
        }
    }
}
