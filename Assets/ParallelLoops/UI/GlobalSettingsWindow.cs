using ParallelLoops;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class GlobalSettingsWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Parallel Loops/Settings")]
    public static void ShowExample()
    {
        GlobalSettingsWindow wnd = GetWindow<GlobalSettingsWindow>();
        wnd.titleContent = new GUIContent("Settings");
    }

    private static TextField m_HeiretsuDirectoryPath;
    private static TextField m_BlenderPath;

    public void OnEnable()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        //Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);

        m_HeiretsuDirectoryPath = labelFromUXML.Q<TextField>("heiretsuPathTextField");
        m_BlenderPath = labelFromUXML.Q<TextField>("blenderPathTextField");

        Button heiretsuPathButton = labelFromUXML.Q<Button>("heiretsuPathButton");
        Button blenderPathButton = labelFromUXML.Q<Button>("blenderPathButton");
        Button saveSettingsButton = labelFromUXML.Q<Button>("saveSettingsButton");
        heiretsuPathButton.clicked += HeiretsuPathButton_clicked;
        blenderPathButton.clicked += BlenderPathButton_clicked;
        saveSettingsButton.clicked += SaveSettingsButton_clicked;
    }

    private void HeiretsuPathButton_clicked()
    {
        string heiretsuPath = EditorUtility.OpenFolderPanel("Open unpacked Heiretsu files folder", "", "");
        if (!string.IsNullOrEmpty(heiretsuPath))
        {
            m_HeiretsuDirectoryPath.value = heiretsuPath;
        }
    }

    private void BlenderPathButton_clicked()
    {
        string blenderPath = EditorUtility.OpenFilePanel("Open unpacked Heiretsu files folder", "", "");
        if (!string.IsNullOrEmpty(blenderPath))
        {
            m_BlenderPath.value = blenderPath;
        }
    }

    private void SaveSettingsButton_clicked()
    {
        DestroyImmediate(this);
    }

    public void CreateGUI()
    {
        GlobalSettings.LoadSettings();

        m_HeiretsuDirectoryPath.value = GlobalSettings.HeiretsuPath;
        m_BlenderPath.value = GlobalSettings.BlenderPath;
    }

    public void OnDestroy()
    {
        if (GlobalSettings.HeiretsuPath != m_HeiretsuDirectoryPath.value)
        {
            GlobalSettings.HeiretsuPath = m_HeiretsuDirectoryPath.value;
            GlobalSettings.SetUpWorkspace();
        }
        GlobalSettings.BlenderPath = m_BlenderPath.value;
        GlobalSettings.SaveSettings();
    }
}
