using UnityEditor;
using UnityEngine;

public class MeshPreview : EditorWindow
{
    public static MeshPreview instance;

    private string meshPath;
    private string materialPath;
    private string assetPath;

    public static void ShowPreview(string meshPath, string materialPath, string assetPath)
    {
        var window = GetWindow<MeshPreview>(false, "Mesh Preview", true);
        window.meshPath = meshPath;
        window.materialPath = materialPath;
        window.assetPath = assetPath;
        instance = window;
        window.Show();
        window.Repaint();
    }

    public new void Close()
    {
        instance = null;
        base.Close();
    }

    private void OnGUI()
    {
        GUILayout.Label("Preview data", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Mesh Path", meshPath ?? "");
        EditorGUILayout.LabelField("Material Path", materialPath ?? "");
        EditorGUILayout.LabelField("Asset Path", assetPath ?? "");
        EditorGUILayout.HelpBox("Preview rendering is not implemented yet.", MessageType.Info);
    }
}
