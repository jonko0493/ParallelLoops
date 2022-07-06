using ParallelLoops;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class GlobalSettingsWindow : EditorWindow
{
    //[SerializeField]
    //private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Parallel Loops/Settings")]
    public static void ShowExample()
    {
        GlobalSettingsWindow wnd = GetWindow<GlobalSettingsWindow>();
        wnd.titleContent = new GUIContent("GlobalSettingsWindow");
    }

    private static TextField m_HeiretsuDirectoryPath;
    private static TextField m_BlenderPath;

    public void OnEnable()
    {
        m_HeiretsuDirectoryPath = new() { label = "Heiretsu Game Directory" };
        m_BlenderPath = new() { label = "Blender Executable" };
    }

    public void CreateGUI()
    {
        GlobalSettings.LoadSettings();

        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        Label label = new() { text = "Paths" };

        m_HeiretsuDirectoryPath.value = GlobalSettings.HeiretsuPath;
        m_BlenderPath.value = GlobalSettings.BlenderPath;

        root.Add(label);
        root.Add(m_HeiretsuDirectoryPath);
        root.Add(m_BlenderPath);

        // Instantiate UXML
        //VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        //root.Add(labelFromUXML);
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
