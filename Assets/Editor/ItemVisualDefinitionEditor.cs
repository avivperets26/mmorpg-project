// Assets/Editor/ItemVisualDefinitionEditor.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Game.Items;
using Game.Items.Definitions;

[CustomEditor(typeof(ItemVisualDefinition))]
[CanEditMultipleObjects]
public class ItemVisualDefinitionEditor : Editor
{
    SerializedProperty slot, prefab;
    SerializedProperty attachMode, socketName, useGripAlign;
    SerializedProperty localPosition, localEulerAngles, localScale;

    SerializedProperty previewAllOverride;          // NEW

    SerializedProperty rightHandOverride, leftHandOverride;
    SerializedProperty previewRightHandOverride, previewLeftHandOverride;

    SerializedProperty skinnedRootBoneNameOverride, animatorOverrideForThisItem;

    void OnEnable()
    {
        slot                        = serializedObject.FindProperty(nameof(ItemVisualDefinition.slot));
        prefab                      = serializedObject.FindProperty(nameof(ItemVisualDefinition.prefab));
        attachMode                  = serializedObject.FindProperty(nameof(ItemVisualDefinition.attachMode));
        socketName                  = serializedObject.FindProperty(nameof(ItemVisualDefinition.socketName));
        useGripAlign                = serializedObject.FindProperty(nameof(ItemVisualDefinition.useGripAlign));

        localPosition               = serializedObject.FindProperty(nameof(ItemVisualDefinition.localPosition));
        localEulerAngles            = serializedObject.FindProperty(nameof(ItemVisualDefinition.localEulerAngles));
        localScale                  = serializedObject.FindProperty(nameof(ItemVisualDefinition.localScale));

        previewAllOverride          = serializedObject.FindProperty(nameof(ItemVisualDefinition.previewAllOverride));

        rightHandOverride           = serializedObject.FindProperty(nameof(ItemVisualDefinition.rightHandOverride));
        leftHandOverride            = serializedObject.FindProperty(nameof(ItemVisualDefinition.leftHandOverride));
        previewRightHandOverride    = serializedObject.FindProperty(nameof(ItemVisualDefinition.previewRightHandOverride));
        previewLeftHandOverride     = serializedObject.FindProperty(nameof(ItemVisualDefinition.previewLeftHandOverride));

        skinnedRootBoneNameOverride = serializedObject.FindProperty(nameof(ItemVisualDefinition.skinnedRootBoneNameOverride));
        animatorOverrideForThisItem = serializedObject.FindProperty(nameof(ItemVisualDefinition.animatorOverrideForThisItem));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Slot", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(slot);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Prefab to spawn when equipped", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(prefab);
        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("Attachment", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(attachMode);
        EditorGUILayout.PropertyField(socketName);
        using (new EditorGUI.DisabledScope((VisualAttachMode)attachMode.enumValueIndex != VisualAttachMode.SocketWithGrip))
        {
            EditorGUILayout.PropertyField(useGripAlign, new GUIContent("Use Grip Align"));
        }
        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("Base local offsets (WORLD / PLAYER)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(localPosition);
        EditorGUILayout.PropertyField(localEulerAngles);
        EditorGUILayout.PropertyField(localScale);
        EditorGUILayout.Space(6);

        // Preview-wide (single block, any slot)
        EditorGUILayout.LabelField("Preview-wide override (applies only in preview)", EditorStyles.boldLabel);
        DrawOffsetOverride(previewAllOverride, "Preview All Override");
        EditorGUILayout.Space(6);

        // Hand-specific sections only for hand slots
        var slotEnum = (EquipmentSlot)slot.enumValueIndex;
        bool isHand = slotEnum == EquipmentSlot.RightHand || slotEnum == EquipmentSlot.LeftHand;
        if (isHand)
        {
            EditorGUILayout.LabelField("WORLD overrides by hand", EditorStyles.boldLabel);
            DrawOffsetOverride(rightHandOverride, "Right Hand Override");
            DrawOffsetOverride(leftHandOverride,  "Left Hand Override");
            EditorGUILayout.Space(6);

            EditorGUILayout.LabelField("PREVIEW overrides by hand", EditorStyles.boldLabel);
            DrawOffsetOverride(previewRightHandOverride, "Preview Right Hand Override");
            DrawOffsetOverride(previewLeftHandOverride,  "Preview Left Hand Override");
            EditorGUILayout.Space(6);
        }

        EditorGUILayout.LabelField("Skinned mapping (if SkinnedToAvatar)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(skinnedRootBoneNameOverride);
        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("Optional animator override", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(animatorOverrideForThisItem);

        serializedObject.ApplyModifiedProperties();
    }

    static void DrawOffsetOverride(SerializedProperty prop, string label)
    {
        var enabled = prop.FindPropertyRelative(nameof(OffsetOverride.enabled));
        var mode    = prop.FindPropertyRelative(nameof(OffsetOverride.mode));
        var pos     = prop.FindPropertyRelative(nameof(OffsetOverride.localPosition));
        var eul     = prop.FindPropertyRelative(nameof(OffsetOverride.localEulerAngles));
        var scl     = prop.FindPropertyRelative(nameof(OffsetOverride.localScale));

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enabled);
            using (new EditorGUI.DisabledScope(!enabled.boolValue))
            {
                EditorGUILayout.PropertyField(mode);
                EditorGUILayout.PropertyField(pos,  new GUIContent("Local Position"));
                EditorGUILayout.PropertyField(eul,  new GUIContent("Local Euler Angles"));
                EditorGUILayout.PropertyField(scl,  new GUIContent("Local Scale"));
                if ((OffsetMode)mode.enumValueIndex == OffsetMode.Absolute)
                {
                    EditorGUILayout.HelpBox("Absolute = ignores GripAlign/Base and sets exact TRS.", MessageType.Info);
                }
            }
        }
    }
}
#endif
