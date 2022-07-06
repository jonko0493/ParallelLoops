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
using UnityEngine.UIElements;

public class LoadMapWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Parallel Loops/Load Map", priority = 0)]
    public static void ShowExample()
    {
        LoadMapWindow wnd = GetWindow<LoadMapWindow>();
        wnd.titleContent = new GUIContent("LoadMapWindow");
    }

    private MapDefinitionsFile _mapDefinitionsFile;
    private CameraDataFile _cameraDataFile;
    private DropdownField m_SectionDropdown;
    private DropdownField m_MapDropdown;

    public void CreateGUI()
    {
        (_mapDefinitionsFile, _cameraDataFile) = MapAndCameraDataImporter.LoadMapAndCameraData();

        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);

        m_MapDropdown = labelFromUXML.Q<DropdownField>("mapDropdown");
        m_MapDropdown.choices.Clear();
        m_MapDropdown.SetEnabled(false);

        m_SectionDropdown = labelFromUXML.Q<DropdownField>("sectionDropdown");
        m_SectionDropdown.choices.Clear();
        for (int i = 2; i < _mapDefinitionsFile.Sections.Count + 2; i++)
        {
            m_SectionDropdown.choices.Add($"{i}");
        }
        m_SectionDropdown.RegisterValueChangedCallback(v => SectionDropdown_onSelectionChanged(v.newValue));

        Button loadButton = labelFromUXML.Q<Button>("mapLoadButton");
        loadButton.clicked += LoadButton_clicked;
    }

    private void SectionDropdown_onSelectionChanged(string newValue)
    {
        m_MapDropdown.SetEnabled(true);
        m_MapDropdown.choices.Clear();
        m_MapDropdown.value = "";

        int section = int.Parse(newValue);
        for (int i = 0; i < _mapDefinitionsFile.Sections[section - 2].MapDefinitions.Count; i++)
        {
            m_MapDropdown.choices.Add($"{i}");
        }
    }

    private void LoadButton_clicked()
    {
        if (!int.TryParse(m_SectionDropdown.value, out int section) || !int.TryParse(m_MapDropdown.value, out int map))
        {
            return;
        }

        GameObject[] gameObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject gameObject in gameObjects)
        {
            DestroyImmediate(gameObject);
        }

        MapDefinition mapDefinition = _mapDefinitionsFile.Sections[section - 2].MapDefinitions[map];

        GraphicsFile mapData = new();
        mapData.Initialize(File.ReadAllBytes(Path.Combine(GlobalSettings.WorkspacePath, "grp", $"{mapDefinition.MapDataIndex:D4}.MAP")), 0);
        List<string> modelsToLoad = new()
        {
            mapData.MapModel,
            mapData.MapBackgroundModel
        };
        modelsToLoad.AddRange(mapData.MapModelNames.Where(m => !string.IsNullOrWhiteSpace(m)));

        SgeImporter.CreateFbx(modelsToLoad.ToArray());

        SceneVisibilityManager sceneVisibilityManager = SceneVisibilityManager.instance;
        //GameObject bgObject = (GameObject)Instantiate(Resources.Load($"Models/{mapData.MapBackgroundModel}.sge"));
        //sceneVisibilityManager.DisablePicking(bgObject, true);
        GameObject mapObject = (GameObject)Instantiate(Resources.Load($"Models/{mapData.MapModel}.sge"));

        HeiretsuMap mapProperties = mapObject.AddComponent<HeiretsuMap>();
        mapProperties.Section = section;
        mapProperties.MapIndex = map;
        sceneVisibilityManager.DisablePicking(mapObject, true);
        List<GameObject> objects = new();

        for (int i = 0; i < mapData.MapEntries.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(mapData.MapEntries[i].Name))
            {
                GameObject gameObject = (GameObject)Instantiate(Resources.Load($"Models/{mapData.MapEntries[i].Name}.sge"));
                gameObject.name = $"{i:D3}_{mapData.MapEntries[i].Name}";
                Vector3 position = new(-mapData.MapEntries[i].X, mapData.MapEntries[i].Z, -mapData.MapEntries[i].Y); // reverse since unity is Y-up
                gameObject.transform.Translate(position * Sge.MODEL_SCALE);
                gameObject.transform.Rotate(new(0, 1, 0), -mapData.MapEntries[i].Rotation - 90);

                objects.Add(gameObject);
            }
            else if (mapData.MapEntries[i].X != 0 || mapData.MapEntries[i].Y != 0 || mapData.MapEntries[i].Z != 0)
            {
                GameObject preVec = new() { name = $"{i:D3}_Precalculated_Vector" };
                Vector3 position = new(-mapData.MapEntries[i].X, mapData.MapEntries[i].Z, -mapData.MapEntries[i].Y); // reverse since unity is Y-up
                preVec.transform.Translate(position * Sge.MODEL_SCALE);

                objects.Add(preVec);
            }
        }

        CameraDataEntry cameraData = _cameraDataFile.CameraDataEntries[mapDefinition.CameraDataEntryIndex];

        GameObject cameraObject = new() { name = "Camera" };
        HeiretsuCamera heiretsuCamera = cameraObject.AddComponent<HeiretsuCamera>();
        heiretsuCamera.CameraIndex = mapDefinition.CameraDataEntryIndex;
        heiretsuCamera.MinPitch = cameraData.MinPitch;
        heiretsuCamera.MaxPitch = cameraData.MaxPitch;
        heiretsuCamera.MinYaw = cameraData.MinYaw;
        heiretsuCamera.MaxYaw = cameraData.MaxYaw;
        Camera camera = cameraObject.AddComponent<Camera>();
        Vector3 cameraPosition = new(-cameraData.XPosition, cameraData.ZPosition, -cameraData.YPosition);
        cameraObject.transform.Translate(cameraPosition * Sge.MODEL_SCALE);
        Vector3 lookPosition = new(-cameraData.XLook, cameraData.ZLook, -cameraData.YLook);
        cameraObject.transform.rotation = Quaternion.LookRotation(lookPosition * Sge.MODEL_SCALE - cameraPosition * Sge.MODEL_SCALE);
        camera.fieldOfView = 46.8f / cameraData.Zoom;
        camera.farClipPlane = 10000;
    }
}
