#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemDefinition), true)]
public class ItemDefinitionEditor : Editor
{
    // Props we want to custom-draw
    SerializedProperty widthProp;
    SerializedProperty heightProp;
    SerializedProperty socketsMaxProp;
    SerializedProperty canBeBlessedProp;
    SerializedProperty socketSlotTypeProp;

    void OnEnable()
    {
        widthProp          = serializedObject.FindProperty("width");
        heightProp         = serializedObject.FindProperty("height");
        socketsMaxProp     = serializedObject.FindProperty("socketsMax");
        canBeBlessedProp   = serializedObject.FindProperty("canBeBlessed");
        socketSlotTypeProp = serializedObject.FindProperty("socketSlotType");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw everything EXCEPT the fields we will place into our custom group
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "canBeBlessed",
            "socketSlotType",
            "socketsMax" // hidden, but exclude anyway for safety
        );

        EditorGUILayout.Space();

        // --- Group: Blessing & Sockets --------------------------------------
        EditorGUILayout.LabelField("Blessing & Sockets", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.PropertyField(canBeBlessedProp, new GUIContent("Can Be Blessed"));
            EditorGUILayout.PropertyField(socketSlotTypeProp, new GUIContent("Socket Slot Type"));

            // Dynamic sockets slider
            int w = Mathf.Max(1, widthProp.intValue);
            int h = Mathf.Max(1, heightProp.intValue);
            int maxSockets = Mathf.Max(1, w * h);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Sockets (Dynamic Limit)", EditorStyles.miniBoldLabel);

            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.IntSlider(new GUIContent("Max Sockets"), socketsMaxProp.intValue, 0, maxSockets);
            if (EditorGUI.EndChangeCheck())
            {
                socketsMaxProp.intValue = next;
            }

            EditorGUILayout.HelpBox($"Capped by footprint: {w} × {h} → {maxSockets}", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
