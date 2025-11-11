// Assets\Scripts\Gameplay\Equipment\EquipmentVisualsController.cs
using System.Collections.Generic;
using UnityEngine;
using Game.Items;
using Game.Items.Definitions;

namespace Game.Equipment
{
    [DisallowMultipleComponent]
    public class EquipmentVisualsController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Transform socketsRoot; // e.g., KnightModel (so it can find your sockets)
        [SerializeField] private Animator animator;     // Player animator
        [SerializeField] private Transform avatarRoot;  // Armature (for skinned items)

        private readonly Dictionary<EquipmentSlot, GameObject> _active = new();
        private readonly Dictionary<string, Transform> _sockets = new();

        void Awake()
        {
            if (!socketsRoot) socketsRoot = transform;
            CacheSockets();
        }

        public void OnEquipped(EquipmentSlot slot, IHasItemVisual def)
        {
            // remove any existing visual in this slot
            Unequip(slot);

            // get the visual definition from the item
            var vis = def?.Visual;
            if (vis == null || vis.prefab == null) return;

            // decide which socket to use:
            //  - if the asset specified a socketName, use it
            //  - otherwise choose by the target slot (Left/Right hand)
            var socketName = !string.IsNullOrEmpty(vis.socketName)
                ? vis.socketName
                : (slot == EquipmentSlot.LeftHand ? "LeftHand_Socket" : "RightHand_Socket");

            var socket = FindSocket(socketName);
            if (!socket)
            {
                Debug.LogWarning($"[Visuals] Socket '{socketName}' not found.");
                return;
            }

            // spawn and attach
            var go = Instantiate(vis.prefab);
            go.name = $"{slot}_VIS_{vis.prefab.name}";

            switch (vis.attachMode)
            {
                case VisualAttachMode.SocketStatic:
                    Parent(go.transform, socket, vis);
                    break;

                case VisualAttachMode.SocketWithGrip:
                    Parent(go.transform, socket, vis, gripAlign: vis.useGripAlign ? "GripAlign" : null);
                    break;

                case VisualAttachMode.SkinnedToAvatar:
                    Parent(go.transform, socket, vis);
                    RemapSkinned(go, vis);
                    break;
            }

            ApplyAnimatorOverride(vis);
            _active[slot] = go;
        }


        public void OnUnequipped(EquipmentSlot slot) => Unequip(slot);

        // ---- internals ----
        private void Unequip(EquipmentSlot slot)
        {
            if (_active.TryGetValue(slot, out var go) && go) Destroy(go);
            _active.Remove(slot);
        }

        private void Parent(Transform t, Transform socket, ItemVisualDefinition vis, string gripAlign = null)
        {
            t.SetParent(socket, false);
            if (!string.IsNullOrEmpty(gripAlign))
            {
                var grip = socket.Find(gripAlign);
                if (grip)
                {
                    t.localPosition = grip.localPosition;
                    t.localRotation = grip.localRotation;
                    t.localScale = grip.localScale;
                }
                else
                {
                    t.localPosition = Vector3.zero;
                    t.localRotation = Quaternion.identity;
                    t.localScale = Vector3.one;
                }
            }
            t.localPosition += vis.localPosition;
            t.localRotation = t.localRotation * Quaternion.Euler(vis.localEulerAngles);
            t.localScale = Vector3.Scale(t.localScale, vis.localScale);
        }

        private void RemapSkinned(GameObject go, ItemVisualDefinition vis)
        {
            if (!avatarRoot) return;
            var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (!smr) return;

            var map = new Dictionary<string, Transform>();
            foreach (var t in avatarRoot.GetComponentsInChildren<Transform>(true)) map[t.name] = t;

            var bones = smr.bones;
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] && map.TryGetValue(bones[i].name, out var repl)) bones[i] = repl;
            smr.bones = bones;

            if (smr.rootBone)
            {
                var rootName = string.IsNullOrEmpty(vis.skinnedRootBoneNameOverride) ? smr.rootBone.name : vis.skinnedRootBoneNameOverride;
                if (map.TryGetValue(rootName, out var rb)) smr.rootBone = rb;
            }
        }

        private void ApplyAnimatorOverride(ItemVisualDefinition vis)
        {
            if (!animator || !vis.animatorOverrideForThisItem) return;
            var aoc = new AnimatorOverrideController(animator.runtimeAnimatorController);
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            vis.animatorOverrideForThisItem.GetOverrides(pairs);
            aoc.ApplyOverrides(pairs);
            animator.runtimeAnimatorController = aoc;
        }

        private void CacheSockets()
        {
            _sockets.Clear();
            foreach (var t in socketsRoot.GetComponentsInChildren<Transform>(true))
                _sockets[t.name] = t;
        }

        private Transform FindSocket(string name) =>
            string.IsNullOrEmpty(name) ? socketsRoot : (_sockets.TryGetValue(name, out var t) ? t : null);
    }
}
