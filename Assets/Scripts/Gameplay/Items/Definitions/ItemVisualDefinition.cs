// Assets/Scripts/Gameplay/Items/Definitions/ItemVisualDefinition.cs
using UnityEngine;
using Game.Items;

namespace Game.Items.Definitions
{
    public enum VisualAttachMode { SocketStatic, SocketWithGrip, SkinnedToAvatar }

    // How to interpret an override
    public enum OffsetMode { Additive, Absolute }

    // Reusable override block
    [System.Serializable]
    public struct OffsetOverride
    {
        public bool enabled;
        public OffsetMode mode;              // Additive = relative to Grip/Base, Absolute = exact TRS
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale;

        public static OffsetOverride Disabled => new OffsetOverride
        {
            enabled = false,
            mode = OffsetMode.Additive,
            localPosition = Vector3.zero,
            localEulerAngles = Vector3.zero,
            localScale = Vector3.one
        };
    }

    [CreateAssetMenu(menuName = "Items/Item Visual Definition", fileName = "ItemVisual_")]
    public class ItemVisualDefinition : ScriptableObject
    {
        [Header("Slot")]
        public EquipmentSlot slot;

        [Header("Prefab to spawn when equipped")]
        public GameObject prefab;

        [Header("Attachment")]
        public VisualAttachMode attachMode = VisualAttachMode.SocketWithGrip;
        public string socketName;                  // e.g., "RightHand_Socket", "Head_Socket"
        public bool useGripAlign = true;           // only when attachMode == SocketWithGrip

        [Header("Base local offsets (WORLD / PLAYER)")]
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale = Vector3.one;

        [Header("PREVIEW-wide override (applies on preview in addition to, or replacing, Base)")]
        public OffsetOverride previewAllOverride;  // NEW: replaces old (usePreviewOverrides + 3 vectors)

        [Header("WORLD overrides by hand (relative unless Mode=Absolute)")]
        public OffsetOverride rightHandOverride;
        public OffsetOverride leftHandOverride;

        [Header("PREVIEW overrides by hand (relative unless Mode=Absolute)")]
        public OffsetOverride previewRightHandOverride;
        public OffsetOverride previewLeftHandOverride;

        [Header("Skinned mapping (if SkinnedToAvatar)")]
        public string skinnedRootBoneNameOverride;

        [Header("Optional animator override")]
        public AnimatorOverrideController animatorOverrideForThisItem;
    }
}
