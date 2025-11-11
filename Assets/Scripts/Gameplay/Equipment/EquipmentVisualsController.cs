// Assets/Scripts/Gameplay/Equipment/EquipmentVisualsController.cs
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

        // --------------------------------------------------------------------
        // Public API from EquipmentController
        // --------------------------------------------------------------------
        public void OnEquipped(EquipmentSlot slot, IHasItemVisual def)
        {
            // remove any existing visual in this slot
            Unequip(slot);

            // get the visual definition from the item
            var vis = def?.Visual;
            if (vis == null || vis.prefab == null) return;

            // pick a socket name
            var socketName = !string.IsNullOrEmpty(vis.socketName)
                ? vis.socketName
                : (slot == EquipmentSlot.LeftHand ? "LeftHand_Socket"
                                                  : slot == EquipmentSlot.RightHand ? "RightHand_Socket"
                                                                                    : null);

            var socket = FindSocket(socketName);
            if (!socket)
            {
                Debug.LogWarning($"[Visuals] Socket '{socketName ?? "<root>"}' not found.");
                return;
            }

            // spawn
            var go = Instantiate(vis.prefab);
            go.name = $"{slot}_VIS_{vis.prefab.name}";

            // attach using our unified helper (adds HelmetOffset support, grip align, etc.)
            AttachVisualToSocket(go.transform, slot, socket, vis);

            // animator override (optional)
            ApplyAnimatorOverride(vis);

            _active[slot] = go;
        }

        public void OnUnequipped(EquipmentSlot slot) => Unequip(slot);

        // --------------------------------------------------------------------
        // Internals
        // --------------------------------------------------------------------
        private void Unequip(EquipmentSlot slot)
        {
            if (_active.TryGetValue(slot, out var go) && go) Destroy(go);
            _active.Remove(slot);
        }

        /// <summary>
        /// Parents 't' under the correct attach parent (HelmetOffset for helmets when present,
        /// otherwise the socket), then applies per-item local offsets.
        /// Also supports GripAlign when attachMode == SocketWithGrip.
        /// </summary>
        private void AttachVisualToSocket(Transform t, EquipmentSlot slot, Transform socket, ItemVisualDefinition vis)
        {
            // Prefer a per-rig shim for helmets (single place to tune on the rig)
            Transform attachParent = socket;
            if (slot == EquipmentSlot.Helm || vis.slot == EquipmentSlot.Helm)
            {
                var shim = socket.Find("HelmetOffset");
                if (shim) attachParent = shim;
            }

            // Parent first (no world-space changes)
            t.SetParent(attachParent, false);

            // Optional "GripAlign" alignment for hand-held items
            if (vis.attachMode == VisualAttachMode.SocketWithGrip)
            {
                var grip = attachParent.Find("GripAlign");
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

            // Apply per-item visual offsets (from the ItemVisualDefinition)
            t.localPosition += vis.localPosition;
            t.localRotation = t.localRotation * Quaternion.Euler(vis.localEulerAngles);
            t.localScale = Vector3.Scale(t.localScale, vis.localScale);

            // If this item is skinned, remap to avatar bones
            if (vis.attachMode == VisualAttachMode.SkinnedToAvatar)
                RemapSkinned(t.gameObject, vis);
        }

        private void RemapSkinned(GameObject go, ItemVisualDefinition vis)
        {
            if (!avatarRoot) return;
            var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (!smr) return;

            var map = new Dictionary<string, Transform>();
            foreach (var t in avatarRoot.GetComponentsInChildren<Transform>(true))
                map[t.name] = t;

            var bones = smr.bones;
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] && map.TryGetValue(bones[i].name, out var repl))
                    bones[i] = repl;
            }
            smr.bones = bones;

            if (smr.rootBone)
            {
                var rootName = string.IsNullOrEmpty(vis.skinnedRootBoneNameOverride)
                    ? smr.rootBone.name
                    : vis.skinnedRootBoneNameOverride;

                if (map.TryGetValue(rootName, out var rb))
                    smr.rootBone = rb;
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
