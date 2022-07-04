using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Data;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ParallelLoops.Importers
{
    public class MapAndCameraDataImporter
    {
        public static (MapDefinitionsFile mapDef, CameraDataFile cameraData) LoadMapAndCameraData()
        {
            MapDefinitionsFile mapDefFile = new();
            mapDefFile.Initialize(File.ReadAllBytes(Path.Combine(GlobalSettings.WorkspacePath, "dat", $"{GlobalSettings.MapDefFileIndex:D4}.bin")), 0);

            CameraDataFile cameraDataFile = new();
            cameraDataFile.Initialize(File.ReadAllBytes(Path.Combine(GlobalSettings.WorkspacePath, "dat", $"{GlobalSettings.CameraDataFileIndex:D4}.bin")), 0);

            return (mapDefFile, cameraDataFile);
        }
    }
}
