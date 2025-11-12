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
        [SerializeField] private Transform socketsRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform avatarRoot;

        private readonly Dictionary<EquipmentSlot, GameObject> _active = new();
        private readonly Dictionary<string, Transform> _sockets = new();

        private bool _isPreviewContext = false;

        void Awake()
        {
            if (!socketsRoot) socketsRoot = transform;
            CacheSockets();
        }

        private static string DefaultSocketForSlot(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.RightHand: return "RightHand_Socket";
                case EquipmentSlot.LeftHand: return "LeftHand_Socket";
                case EquipmentSlot.Helm: return "Head_Socket";
                case EquipmentSlot.Wings: return "Wings_Socket";
                case EquipmentSlot.Orb: return "Orb_Socket";
                case EquipmentSlot.Pet: return "Pet_Socket";
                default: return null;
            }
        }

        // ---------- tiny helpers ----------
        private static void ApplyAdditive(Transform t, Vector3 pos, Vector3 euler, Vector3 scale)
        {
            t.localPosition += pos;
            t.localRotation = t.localRotation * Quaternion.Euler(euler);
            t.localScale = Vector3.Scale(t.localScale, (scale == Vector3.zero ? Vector3.one : scale));
        }

        private static void ApplyAbsolute(Transform t, Vector3 pos, Vector3 euler, Vector3 scale)
        {
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(euler);
            t.localScale = (scale == Vector3.zero ? Vector3.one : scale);
        }

        private static void ApplyOverride(Transform t, OffsetOverride ov)
        {
            if (!ov.enabled) return;
            if (ov.mode == OffsetMode.Absolute) ApplyAbsolute(t, ov.localPosition, ov.localEulerAngles, ov.localScale);
            else ApplyAdditive(t, ov.localPosition, ov.localEulerAngles, ov.localScale);
        }
        // ----------------------------------

        private void SetLayerRecursively(Transform t, int layer)
        {
            var stack = new Stack<Transform>();
            stack.Push(t);
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                cur.gameObject.layer = layer;
                for (int i = 0; i < cur.childCount; i++)
                    stack.Push(cur.GetChild(i));
            }
        }

        public void OnEquipped(EquipmentSlot slot, IHasItemVisual def)
        {
            Unequip(slot);

            var vis = def?.Visual;
            if (!vis || !vis.prefab) return;

            var socketName = !string.IsNullOrEmpty(vis.socketName)
                ? vis.socketName
                : (slot == EquipmentSlot.LeftHand ? "LeftHand_Socket"
                   : slot == EquipmentSlot.RightHand ? "RightHand_Socket"
                   : slot == EquipmentSlot.Helm ? "Head_Socket"
                   : null);

            var socket = FindSocket(socketName);
            if (!socket)
            {
                Debug.LogWarning($"[Visuals] Socket '{(socketName ?? "<root>")}' not found.");
                return;
            }

            var go = Instantiate(vis.prefab);
            go.name = $"{slot}_VIS_{vis.prefab.name}";
            SetLayerRecursively(go.transform, socket.gameObject.layer);

            AttachVisualToSocket(go.transform, slot, socket, vis);

            ApplyAnimatorOverride(vis);
            _active[slot] = go;
        }

        public void OnUnequipped(EquipmentSlot slot) => Unequip(slot);

        public void InitForPreview(Transform sockets, Transform avatar, Animator anim = null)
        {
            socketsRoot = sockets ? sockets : transform;
            avatarRoot = avatar;
            animator = anim ? anim : animator;
            _isPreviewContext = true;
            CacheSockets();
        }

        private void Unequip(EquipmentSlot slot)
        {
            if (_active.TryGetValue(slot, out var go) && go) Destroy(go);
            _active.Remove(slot);
        }

        private void AttachVisualToSocket(Transform t, EquipmentSlot slot, Transform socket, ItemVisualDefinition vis)
        {
            bool isHandSlot = (slot == EquipmentSlot.RightHand || slot == EquipmentSlot.LeftHand);

            // 1) choose attach parent (Helm may use a shim)
            Transform attachParent = socket;
            if (slot == EquipmentSlot.Helm || vis.slot == EquipmentSlot.Helm)
            {
                var shim = socket.Find("HelmetOffset");
                if (shim) attachParent = shim;
            }

            // 2) parent without changing local space
            t.SetParent(attachParent, false);

            // 3) GripAlign baseline if requested (only matters for SocketWithGrip)
            if (vis.attachMode == VisualAttachMode.SocketWithGrip && vis.useGripAlign)
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

            // 4) Base (world/player) additive
            ApplyAdditive(t, vis.localPosition, vis.localEulerAngles, vis.localScale);

            // 5) WORLD hand-specific additive (hand slots only)
            if (!_isPreviewContext && isHandSlot)
            {
                if (slot == EquipmentSlot.RightHand) ApplyOverride(t, vis.rightHandOverride);
                else ApplyOverride(t, vis.leftHandOverride);
            }

            // 6) PREVIEW-wide
            if (_isPreviewContext && vis.previewAllOverride.enabled)
            {
                if (vis.previewAllOverride.mode == OffsetMode.Absolute)
                {
                    // Absolute preview TRS = replace everything done so far
                    ApplyAbsolute(t, vis.previewAllOverride.localPosition, vis.previewAllOverride.localEulerAngles, vis.previewAllOverride.localScale);
                }
                else
                {
                    // Additive preview on top of base/world-hand
                    ApplyAdditive(t, vis.previewAllOverride.localPosition, vis.previewAllOverride.localEulerAngles, vis.previewAllOverride.localScale);
                }
            }

            // 7) PREVIEW hand-specific additive (hand slots only)
            if (_isPreviewContext && isHandSlot)
            {
                if (slot == EquipmentSlot.RightHand) ApplyOverride(t, vis.previewRightHandOverride);
                else ApplyOverride(t, vis.previewLeftHandOverride);
            }

            // 8) Skinned mapping
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
                if (bones[i] && map.TryGetValue(bones[i].name, out var repl))
                    bones[i] = repl;
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

#if UNITY_EDITOR
            Debug.Log(
                "[Visuals] Cached " + _sockets.Count +
                " under '" + socketsRoot.name + "'. " +
                "Head_Socket=" + _sockets.ContainsKey("Head_Socket") + ", " +
                "LeftHand_Socket=" + _sockets.ContainsKey("LeftHand_Socket") + ", " +
                "RightHand_Socket=" + _sockets.ContainsKey("RightHand_Socket")
            );
#endif
        }

        private Transform FindSocket(string name) =>
            string.IsNullOrEmpty(name) ? socketsRoot : (_sockets.TryGetValue(name, out var t) ? t : null);
    }
}
