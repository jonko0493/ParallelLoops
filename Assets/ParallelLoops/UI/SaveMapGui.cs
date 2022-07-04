using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Data;
using HaruhiHeiretsuLib.Graphics;
using ParallelLoops;
using ParallelLoops.Importers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SaveMapGui
{
    [MenuItem("Parallel Loops/Save Map", priority = 2)]
    public static void ShowExample()
    {
        string folderPath = EditorUtility.SaveFolderPanel("Save map data", "", "");

        if (!string.IsNullOrEmpty(folderPath))
        {
            GameObject[] gameObjects = Object.FindObjectsOfType<GameObject>();
            HeiretsuMap map = gameObjects.First(o => o.name.StartsWith("MP") && o.name.EndsWith("(Clone)")).GetComponent<HeiretsuMap>();
            GameObject cameraObject = gameObjects.First(o => o.name == "Camera");
            HeiretsuCamera camera = cameraObject.GetComponent<HeiretsuCamera>();

            (MapDefinitionsFile mapDefinitionsFile, CameraDataFile cameraDataFile) = MapAndCameraDataImporter.LoadMapAndCameraData();
            GraphicsFile loadedMap = new();
            loadedMap.Initialize(File.ReadAllBytes(Path.Combine(GlobalSettings.WorkspacePath, "grp", $"{mapDefinitionsFile.Sections[map.Section - 2].MapDefinitions[map.MapIndex].MapDataIndex}.bin")), 0);

            mapDefinitionsFile.Sections[map.Section - 2].MapDefinitions[map.MapIndex].CameraDataEntryIndex = (short)camera.CameraIndex;
            cameraDataFile.CameraDataEntries[camera.CameraIndex].XPosition = -camera.transform.position.x / Sge.MODEL_SCALE;
            cameraDataFile.CameraDataEntries[camera.CameraIndex].YPosition = -camera.transform.position.z / Sge.MODEL_SCALE;
            cameraDataFile.CameraDataEntries[camera.CameraIndex].ZPosition = camera.transform.position.y / Sge.MODEL_SCALE;
            Vector3 lookToPoint = camera.transform.position + camera.transform.forward * Sge.MODEL_SCALE;
            cameraDataFile.CameraDataEntries[camera.CameraIndex].XLook = -lookToPoint.x / Sge.MODEL_SCALE;
            cameraDataFile.CameraDataEntries[camera.CameraIndex].YLook = -lookToPoint.z / Sge.MODEL_SCALE;
            cameraDataFile.CameraDataEntries[camera.CameraIndex].ZLook = lookToPoint.y / Sge.MODEL_SCALE;
            cameraDataFile.CameraDataEntries[camera.CameraIndex].Zoom = cameraObject.GetComponent<Camera>().fieldOfView / 46.8f;
            cameraDataFile.CameraDataEntries[camera.CameraIndex].MinYaw = camera.MinYaw;
            cameraDataFile.CameraDataEntries[camera.CameraIndex].MaxYaw = camera.MaxYaw;
            cameraDataFile.CameraDataEntries[camera.CameraIndex].MinPitch = camera.MinPitch;
            cameraDataFile.CameraDataEntries[camera.CameraIndex].MaxPitch = camera.MaxPitch;

            foreach (GameObject gameObject in gameObjects)
            {
                if (int.TryParse(gameObject.name[0..3], out int mapEntryIndex))
                {
                    loadedMap.MapEntries[mapEntryIndex].X = -gameObject.transform.position.x / Sge.MODEL_SCALE;
                    loadedMap.MapEntries[mapEntryIndex].Y = -gameObject.transform.position.z / Sge.MODEL_SCALE;
                    loadedMap.MapEntries[mapEntryIndex].Z = gameObject.transform.position.y / Sge.MODEL_SCALE;
                }
            }

            string[] datBinMap = File.ReadAllLines("Assets/ParallelLoops/Data/dat_bin_map.txt");
            string[] grpBinMap = File.ReadAllLines("Assets/ParallelLoops/Data/grp_bin_map.txt");

            File.WriteAllText(Path.Combine(folderPath, $"{datBinMap.First(d => d.Contains($"dat-{mapDefinitionsFile.Index:D4}"))}.csv"), mapDefinitionsFile.GetCsv());
            File.WriteAllText(Path.Combine(folderPath, $"{datBinMap.First(d => d.Contains($"dat-{cameraDataFile.Index:D4}"))}.csv"), cameraDataFile.GetCsv());
            string mapCsv = string.Join("\n", loadedMap.MapEntries.Select(e => e.GetCsvLine()));
            mapCsv = $"Model,X,Y,Z,Unknown0C,Unknown10,Unknown12,Unknown14,Unknown16,Unknown18,Unknown1A,Unknown1C,Unknown1E,Unknown20,Unknown22,Unknown24,Unknown26,Unknown28,Unknown2A\n{mapCsv}";
            File.WriteAllText(Path.Combine(folderPath, $"{grpBinMap.First(g => g.Contains($"grp-{loadedMap.Index:D4}"))}_map.csv"), mapCsv);

            EditorUtility.DisplayDialog("Map Saved!", "Saved map data files. Ensure you compile before testing in-game.", "OK");
        }
    }
}
