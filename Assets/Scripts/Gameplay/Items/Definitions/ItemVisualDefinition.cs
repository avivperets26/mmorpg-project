// Assets\Scripts\Gameplay\Items\Definitions\ItemVisualDefinition.cs

using UnityEngine;
using Game.Items; // where your EquipmentSlot enum lives

namespace Game.Items.Definitions
{
    public enum VisualAttachMode { SocketStatic, SocketWithGrip, SkinnedToAvatar }

    [CreateAssetMenu(menuName = "Items/Item Visual Definition", fileName = "ItemVisual_")]
    public class ItemVisualDefinition : ScriptableObject
    {
        [Header("Slot")]
        public EquipmentSlot slot;                 // e.g., RightHand, Helm

        [Header("Prefab to spawn when equipped")]
        public GameObject prefab;

        [Header("Attachment")]
        public VisualAttachMode attachMode = VisualAttachMode.SocketWithGrip;
        public string socketName;                  // "RightHand_Socket", "Head_Socket"
        public bool useGripAlign = true;           // looks for child "GripAlign" on the socket

        [Header("Optional local offsets")]
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale = Vector3.one;

        [Header("Skinned mapping (if SkinnedToAvatar)")]
        public string skinnedRootBoneNameOverride;

        [Header("Optional animator override")]
        public AnimatorOverrideController animatorOverrideForThisItem;
    }
}
