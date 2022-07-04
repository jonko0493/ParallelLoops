using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Data;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ParallelLoops.Importers
{
    public class MapDataImporter
    {
        public static MapDefinitionsFile LoadMapData()
        {
            if (GlobalSettings.DatBin is null)
            {
                Debug.Log("Loading dat.bin for the first time this session...");
                GlobalSettings.DatBin = BinArchive<DataFile>.FromFile(Path.Combine(GlobalSettings.HeiretsuPath, "dat.bin"));
            }

            DataFile currentMapDefFile = GlobalSettings.DatBin.Files.First(f => f.Index == GlobalSettings.MapDefFileIndex);
            MapDefinitionsFile mapDefFile = new();
            mapDefFile.Initialize(currentMapDefFile.Data.ToArray(), currentMapDefFile.Offset);
            mapDefFile.CompressedData = currentMapDefFile.CompressedData;
            mapDefFile.Index = currentMapDefFile.Index;
            mapDefFile.MagicInteger = currentMapDefFile.MagicInteger;
            GlobalSettings.DatBin.Files[GlobalSettings.DatBin.Files.IndexOf(currentMapDefFile)] = mapDefFile;

            return mapDefFile;
        }
    }
}
