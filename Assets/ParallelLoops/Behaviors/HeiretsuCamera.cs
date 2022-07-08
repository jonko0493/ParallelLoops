using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Data;
using ParallelLoops;
using ParallelLoops.Importers;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class HeiretsuCamera : MonoBehaviour
{
    public int CameraIndex;
    public bool IgnoreDialog = false;
    public float MinYaw;
    public float MaxYaw;
    public float MinPitch;
    public float MaxPitch;

    private void OnDrawGizmosSelected()
    {
        (MapDefinitionsFile mapDefinitionsFile, CameraDataFile cameraDataFile) = MapAndCameraDataImporter.LoadMapAndCameraData();
        int numCameraUses = mapDefinitionsFile.Sections.Sum(s => s.MapDefinitions.Count(m => m.CameraDataEntryIndex == CameraIndex));
        if (numCameraUses > 1 && !IgnoreDialog)
        {
            int result = EditorUtility.DisplayDialogComplex("Editing a Camera with Multiple Uses",
                $"This camera is used in {mapDefinitionsFile.Sections.Sum(s => s.MapDefinitions.Count(m => m.CameraDataEntryIndex == CameraIndex)) - 1} other maps. Editing it will change all of those maps." +
                $"It is recommended to create a new camera instead. Would you like to do this?", "Yes, create a new camera", "No, let me edit this camera", "Cancel");

            switch (result)
            {
                case 0:
                    if (EditorUtility.DisplayDialog("Set Camera Type", "Should the camera be movable?", "Yes", "No"))
                    {
                        cameraDataFile.CameraDataEntries.Insert(cameraDataFile.StaticCameraIndex, new(cameraDataFile.CameraDataEntries[CameraIndex].GetCsvLine()));
                        foreach (MapDefinitionSection section in mapDefinitionsFile.Sections)
                        {
                            foreach (MapDefinition mapDefinition in section.MapDefinitions)
                            {
                                if (mapDefinition.CameraDataEntryIndex >= cameraDataFile.StaticCameraIndex)
                                {
                                    mapDefinition.CameraDataEntryIndex++;
                                }
                            }
                        }
                        File.WriteAllBytes(Path.Combine(GlobalSettings.WorkspacePath, "dat", $"{GlobalSettings.MapDefFileIndex:D4}.bin"), mapDefinitionsFile.GetBytes());
                        CameraIndex = cameraDataFile.StaticCameraIndex;
                        cameraDataFile.StaticCameraIndex++;
                    }
                    else
                    {
                        MinYaw = MaxYaw = MinPitch = MaxPitch = 0;
                        cameraDataFile.CameraDataEntries.Add(new(cameraDataFile.CameraDataEntries[CameraIndex].GetCsvLine()));
                        CameraIndex = cameraDataFile.CameraDataEntries.Count - 1;
                    }
                    File.WriteAllBytes(Path.Combine(GlobalSettings.WorkspacePath, "dat", $"{GlobalSettings.CameraDataFileIndex:D4}.bin"), cameraDataFile.GetBytes());
                    break;
                case 1:
                    IgnoreDialog = true;
                    break;
                case 2:
                    Selection.objects = new Object[0];
                    return;
            }
        }
    }
}
