// Assets/Scripts/UI/Preview/CharacterPreviewController.cs
using System;
using UnityEngine;
using UnityEngine.UI;
using Game.Items;
using Game.Items.Definitions;
using Game.Equipment;

/// <summary>
/// Displays a static live preview of the player model (no interaction).
/// - Renders a visual-only clone onto a RenderTexture shown in a RawImage.
/// - Recenters the clone by its bounds so feet are at y=0 and x/z are centered.
/// - Faces the camera using initialYaw + modelYawOffset (set 180 if prefab faces -Z).
/// - NEW: Mirrors equipped item visuals from the main EquipmentController.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class CharacterPreviewController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Camera previewCameraPrefab;   // Solid Color; culling mask set here
    [SerializeField] private GameObject playerPrefab;      // visual-only prefab (no input/AI)
    [SerializeField] private Light previewLightPrefab;     // optional

    [Header("Render Texture")]
    [SerializeField] private int rtWidth = 1024;
    [SerializeField] private int rtHeight = 1024;

    [Header("Camera Framing")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 1.7f, 0f); // chest-ish look-at
    [SerializeField] private float distance = 2.4f;
    [Tooltip("0 = face camera, 180 = back to camera")]
    [SerializeField] private float initialYaw = 0f;
    [Tooltip("Use 180 if your prefab faces -Z in authoring; 0 if it faces +Z")]
    [SerializeField] private float modelYawOffset = 180f;

    [Header("Layering")]
    [SerializeField] private string previewLayerName = "InventoryPreview";

    [Header("Framing")]
    [SerializeField] private AutoFrameMode autoFrame = AutoFrameMode.ExpandOnly; // default keeps old behavior
    [Range(0f, 0.5f)][SerializeField] private float framePadding = 0.12f;
    [Range(0.5f, 2f)][SerializeField] private float zoom = 1.0f;
    [SerializeField] private float maxDistance = 20f;

    // --- NEW: source controller to mirror, and a visuals controller on the clone ---
    [Header("Equipment Mirroring")]
    [SerializeField] private EquipmentController source; // optional: can bind from EquipmentController.Awake
    private EquipmentVisualsController _previewVis;      // lives on the clone
    private Transform _cloneSocketsRoot;                 // where sockets live on the clone (auto-picked)
    private Transform _cloneAvatarRoot;                  // armature root for skinned items (auto-picked)

    private int _previewLayer;
    private RawImage _raw;
    private Camera _cam;
    private RenderTexture _rt;
    private Transform _pivot;
    private GameObject _clone;
    private Bounds _modelBoundsLocal;
    public enum AutoFrameMode { Off, ExpandOnly, AlwaysFit }

    private static readonly EquipmentSlot[] ALL_SLOTS = (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot));

    // ---------------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------------
    /// <summary>Bind a live equipment source; preview will mirror it on RefreshNow / changes.</summary>
    public void Bind(EquipmentController src)
    {
        source = src;
        if (source != null)
            source.EquippedChanged += OnSourceChanged;
        // immediate sync if we’re already initialized
        RefreshNow();
    }

    /// <summary>Allows EquipmentController.SendMessage("RefreshNow") to work.</summary>
    public void RefreshNow() => SyncAllFromSource();

    private void OnEnable()
    {
        // When re-enabled, repaint what we have
        RefreshNow();
    }

    private void OnDisable()
    {
        if (source != null)
            source.EquippedChanged -= OnSourceChanged;
    }

    private void OnDestroy()
    {
        if (source != null)
            source.EquippedChanged -= OnSourceChanged;

        if (_rt != null)
        {
            if (_cam) _cam.targetTexture = null;
            _rt.Release();
            Destroy(_rt);
        }
    }

    // ---------------------------------------------------------------------
    // Init (unchanged core preview setup)
    // ---------------------------------------------------------------------
    private void Awake()
    {
        _raw = GetComponent<RawImage>();
        _previewLayer = LayerMask.NameToLayer(previewLayerName);

        // RenderTexture
        _rt = new RenderTexture(rtWidth, rtHeight, 24, RenderTextureFormat.ARGB32)
        {
            name = "CharacterPreviewRT"
        };
        _rt.Create();
        _raw.texture = _rt;

        // Mini-scene root
        var root = new GameObject("PreviewRoot");
        root.transform.SetParent(transform, false);
        root.layer = _previewLayer;

        // Pivot
        _pivot = new GameObject("Pivot").transform;
        _pivot.SetParent(root.transform, false);
        _pivot.localPosition = Vector3.zero;

        // Optional light
        if (previewLightPrefab)
        {
            var l = Instantiate(previewLightPrefab, root.transform);
            l.gameObject.layer = _previewLayer;
            l.cullingMask = 1 << _previewLayer;
        }

        // Camera
        _cam = Instantiate(previewCameraPrefab, root.transform);
        _cam.gameObject.layer = _previewLayer;
        _cam.cullingMask = 1 << _previewLayer;
        _cam.targetTexture = _rt;

        // Visual-only clone
        _clone = Instantiate(playerPrefab, _pivot);
        SetLayerRecursively(_clone, _previewLayer);
        StripRuntimeScripts(_clone); // keep renderers/animators
        CenterCloneOnBounds(_clone.transform, out _modelBoundsLocal);

        // Face the camera
        float totalYaw = 180f + initialYaw + modelYawOffset;
        _pivot.localRotation = Quaternion.Euler(0f, totalYaw, 0f);

        // Camera placement and framing
        PositionCamera();
        AutoFrame();

        // --- NEW: add a visuals controller to the clone and auto-wire sockets/avatar roots ---
        _previewVis = _clone.AddComponent<EquipmentVisualsController>();
        AutoWirePreviewVisuals(_clone.transform, out _cloneSocketsRoot, out _cloneAvatarRoot);

        // Initialize the preview visuals controller
        var cloneAnimator = _clone.GetComponentInChildren<Animator>(true);
        _previewVis.InitForPreview(_cloneSocketsRoot, _cloneAvatarRoot, cloneAnimator);

        // First sync if we already have a source assigned via Inspector
        RefreshNow();
    }

    // ---------------------------------------------------------------------
    // Equipment mirroring
    // ---------------------------------------------------------------------
    private void OnSourceChanged() => RefreshNow();

    private void SyncAllFromSource()
    {
        if (_previewVis == null) return;

        // Clear everything first
        foreach (var slot in ALL_SLOTS)
            _previewVis.OnUnequipped(slot);

        if (source == null) return;

        // Re-apply every equipped def to the preview clone
        foreach (var slot in ALL_SLOTS)
        {
            var def = source.GetEquippedDefinition(slot);
            if (def is IHasItemVisual hv && hv.Visual && hv.Visual.prefab)
                _previewVis.OnEquipped(slot, hv);
        }
    }

    // Auto-wire sockets & armature for Skinned items on the clone
    // CharacterPreviewController.cs
    private void AutoWirePreviewVisuals(Transform root, out Transform socketsRoot, out Transform avatarRoot)
    {
        // 1) Prefer explicit "Sockets" container if present
        socketsRoot = null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(t.name, "Sockets", StringComparison.OrdinalIgnoreCase))
            {
                socketsRoot = t;
                break;
            }
        }

        // 2) Else prefer the Animator transform (top of the rig / model)
        if (!socketsRoot)
        {
            var anim = root.GetComponentInChildren<Animator>(true);
            if (anim) socketsRoot = anim.transform;
        }

        // 3) Else fallback to the root
        if (!socketsRoot) socketsRoot = root;

        // Avatar root: animator or "Armature" or socketsRoot
        var animator = root.GetComponentInChildren<Animator>(true);
        if (animator) avatarRoot = animator.transform;
        else
        {
            avatarRoot = socketsRoot;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(t.name, "Armature", StringComparison.OrdinalIgnoreCase))
                {
                    avatarRoot = t;
                    break;
                }
            }
        }

#if UNITY_EDITOR
    Debug.Log($"[Preview] socketsRoot='{socketsRoot.name}', avatarRoot='{avatarRoot.name}'");
#endif
    }


    // Helper to set private serialized fields cleanly
    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var f = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (f != null) f.SetValue(obj, value);
    }

    // ------------------------ existing helpers ----------------------------

    private void PositionCamera()
    {
        Vector3 target = _pivot.position + cameraOffset;
        _cam.transform.position = target + Vector3.back * distance;
        _cam.transform.rotation = Quaternion.LookRotation(target - _cam.transform.position, Vector3.up);
    }

    private void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursively(t.gameObject, layer);
    }

    private void StripRuntimeScripts(GameObject go)
    {
        var behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in behaviours)
            Destroy(mb);
        // Animators remain (not MonoBehaviour)
    }

    private void CenterCloneOnBounds(Transform t, out Bounds localBounds)
    {
        var rends = t.GetComponentsInChildren<Renderer>(true);
        localBounds = new Bounds(Vector3.zero, Vector3.zero);
        if (rends.Length == 0) return;

        Bounds worldB = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) worldB.Encapsulate(rends[i].bounds);

        Vector3 worldCenter = worldB.center;
        Vector3 worldMinPoint = new Vector3(worldCenter.x, worldB.min.y, worldCenter.z);

        Vector3 centerLocal = t.InverseTransformPoint(worldCenter);
        float minYLocal = t.InverseTransformPoint(worldMinPoint).y;

        Vector3 shift = new Vector3(centerLocal.x, minYLocal, centerLocal.z);
        t.localPosition -= shift;

        bool first = true;
        foreach (var r in rends)
        {
            var wb = r.bounds;
            Vector3[] corners = new Vector3[8];
            Vector3 min = wb.min; Vector3 max = wb.max;
            int k = 0;
            for (int ix = 0; ix <= 1; ix++)
                for (int iy = 0; iy <= 1; iy++)
                    for (int iz = 0; iz <= 1; iz++)
                        corners[k++] = new Vector3(ix == 0 ? min.x : max.x, iy == 0 ? min.y : max.y, iz == 0 ? min.z : max.z);

            foreach (var c in corners)
            {
                Vector3 cl = t.InverseTransformPoint(c);
                if (first) { localBounds = new Bounds(cl, Vector3.zero); first = false; }
                else localBounds.Encapsulate(cl);
            }
        }
    }

    private void AutoFrame()
    {
        if (!_cam || _modelBoundsLocal.size.sqrMagnitude < 1e-6f) return;

        float needHeight = (_modelBoundsLocal.size.y / Mathf.Max(zoom, 0.01f)) * (1f + framePadding * 2f);
        float vfov = _cam.fieldOfView * Mathf.Deg2Rad;
        float fitDist = (needHeight * 0.5f) / Mathf.Tan(vfov * 0.5f);

        switch (autoFrame)
        {
            case AutoFrameMode.Off:
                // do nothing; keep whatever 'distance' you set in the Inspector
                break;

            case AutoFrameMode.ExpandOnly:   // old behavior
                distance = Mathf.Min(Mathf.Max(distance, fitDist), maxDistance);
                PositionCamera();
                break;

            case AutoFrameMode.AlwaysFit:    // force-fit exactly
                distance = Mathf.Clamp(fitDist, 0.1f, maxDistance);
                PositionCamera();
                break;
        }
    }
}
