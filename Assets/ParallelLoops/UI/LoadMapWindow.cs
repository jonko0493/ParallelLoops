using HaruhiHeiretsuLib.Archive;
using HaruhiHeiretsuLib.Data;
using HaruhiHeiretsuLib.Graphics;
using ParallelLoops;
using ParallelLoops.Importers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class LoadMapWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Parallel Loops/Load Map")]
    public static void ShowExample()
    {
        LoadMapWindow wnd = GetWindow<LoadMapWindow>();
        wnd.titleContent = new GUIContent("LoadMapWindow");
    }

    private MapDefinitionsFile _mapDefinitionsFile;
    private DropdownField m_SectionDropdown;
    private DropdownField m_MapDropdown;

    public void CreateGUI()
    {
        _mapDefinitionsFile = MapDataImporter.LoadMapData();

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
        for (int i = 2; i < _mapDefinitionsFile.Sections.Count; i++)
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
        int section = int.Parse(newValue);

        for (int i = 0; i < _mapDefinitionsFile.Sections.Count; i++)
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

        MapDefinition mapDefinition = _mapDefinitionsFile.Sections[section].MapDefinitions[map];

        if (GlobalSettings.GrpBin is null)
        {
            Debug.Log("Loading grp.bin for the first time this session...");
            GlobalSettings.GrpBin = BinArchive<GraphicsFile>.FromFile(Path.Combine(GlobalSettings.HeiretsuPath, "grp.bin"));
        }

        GraphicsFile mapData = GlobalSettings.GrpBin.Files.First(f => f.Index == mapDefinition.MapDataIndex);
        List<string> modelsToLoad = new()
        {
            mapData.MapModel,
            mapData.MapBackgroundModel
        };
        modelsToLoad.AddRange(mapData.MapModelNames.Where(m => !string.IsNullOrWhiteSpace(m)));

        SgeImporter.CreateFbx(modelsToLoad.ToArray());
    }
}
