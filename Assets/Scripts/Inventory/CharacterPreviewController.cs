// Assets/Scripts/Inventory/CharacterPreviewController.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RawImage))]
public class CharacterPreviewController : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
{
    [Header("Wiring")]
    [SerializeField] private Camera previewCameraPrefab;    // a prefab with clear flags: Solid Color, CullingMask: InventoryPreview
    [SerializeField] private GameObject playerPrefab;       // your player visual prefab (no input/AI needed)
    [SerializeField] private Light previewLightPrefab;      // optional, set layer to InventoryPreview

    [Header("Render Texture")]
    [SerializeField] private int rtWidth = 1024;
    [SerializeField] private int rtHeight = 1024;

    [Header("Camera Rig")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 1.6f, 2.2f); // behind & above, looking at chest
    [SerializeField] private float yaw = 180f;     // spin around Y
    [SerializeField] private float pitch = 5f;     // look slightly down/up
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 35f;
    [SerializeField] private float distance = 2.2f;
    [SerializeField] private float minDistance = 1.2f;
    [SerializeField] private float maxDistance = 3.5f;

    [Header("Mouse Controls")]
    [SerializeField] private float dragSensitivity = 0.25f; // degrees per pixel
    [SerializeField] private float scrollSensitivity = 0.25f;
    [SerializeField] private string previewLayerName = "InventoryPreview";
    private int _previewLayer;
    private RawImage _raw;
    private Camera _cam;
    private RenderTexture _rt;
    private Transform _pivot;            // look-at pivot
    private GameObject _clone;           // player clone with visuals only
    private Vector2 _lastDragPos;
    private bool _dragging;

    void Awake()
    {
        _raw = GetComponent<RawImage>();
        _previewLayer = LayerMask.NameToLayer(previewLayerName);

        // Setup RT
        _rt = new RenderTexture(rtWidth, rtHeight, 24, RenderTextureFormat.ARGB32);
        _rt.name = "CharacterPreviewRT";
        _rt.Create();
        _raw.texture = _rt;

        // Spawn tiny preview scene under this UI object
        var root = new GameObject("PreviewRoot");
        root.transform.SetParent(transform, false);
        root.layer = _previewLayer;

        _pivot = new GameObject("Pivot").transform;
        _pivot.SetParent(root.transform, false);
        _pivot.localPosition = Vector3.zero;

        // Light (optional)
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

        // Clone player (visuals only)
        _clone = Instantiate(playerPrefab, _pivot);
        SetLayerRecursively(_clone, _previewLayer);
        StripRuntimeScripts(_clone); // keep only renderers/animator

        // Pose camera
        RepositionCamera();
    }

    void OnEnable()
    {
        // Optional: if you have an equipment system, subscribe here to mirror gear.
        // EquipmentBus.OnEquipped += MirrorEquipment;
        // EquipmentBus.OnUnequipped += MirrorEquipment;
        MirrorFromLivePlayer(); // initial sync
    }

    void OnDisable()
    {
        // EquipmentBus.OnEquipped -= MirrorEquipment;
        // EquipmentBus.OnUnequipped -= MirrorEquipment;
    }

    void OnDestroy()
    {
        if (_rt != null)
        {
            _cam.targetTexture = null;
            _rt.Release();
            Destroy(_rt);
        }
    }

    // === Input ===
    public void OnBeginDrag(PointerEventData e)
    {
        _dragging = true;
        _lastDragPos = e.position;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_dragging) return;
        var delta = e.position - _lastDragPos;
        _lastDragPos = e.position;

        // Rotate UP/DOWN (pitch) and a little Yaw
        pitch = Mathf.Clamp(pitch - delta.y * dragSensitivity, minPitch, maxPitch);
        yaw += delta.x * dragSensitivity;

        RepositionCamera();
    }

    public void OnScroll(PointerEventData e)
    {
        distance = Mathf.Clamp(distance - e.scrollDelta.y * scrollSensitivity, minDistance, maxDistance);
        RepositionCamera();
    }

    private void RepositionCamera()
    {
        // Orbit around pivot using yaw/pitch/distance
        var rot = Quaternion.Euler(pitch, yaw, 0f);
        var dir = rot * Vector3.back; // back from pivot
        _cam.transform.position = _pivot.position + dir * distance + cameraOffset;
        _cam.transform.rotation = Quaternion.LookRotation((_pivot.position + cameraOffset) - _cam.transform.position, Vector3.up);
    }

    // === Syncing ===
    private void MirrorFromLivePlayer()
    {
        // If you have a live Player root in scene, copy Animator avatar + current equipment
        var live = GameObject.FindWithTag("Player");
        if (!live) return;

        var liveAnimator = live.GetComponentInChildren<Animator>();
        var cloneAnimator = _clone.GetComponentInChildren<Animator>();
        if (liveAnimator && cloneAnimator)
        {
            cloneAnimator.runtimeAnimatorController = liveAnimator.runtimeAnimatorController;
            cloneAnimator.avatar = liveAnimator.avatar;
            cloneAnimator.Update(0f);
        }

        // If your equipment system attaches meshes under bones, call your existing
        // builder/apply method here using the same item definitions:
        // previewEquipment.ApplyFrom(liveEquipment);
    }

    private void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
    }

    private void StripRuntimeScripts(GameObject go)
    {
        // Remove gameplay-only scripts (movement, input, health, etc.)
        var all = go.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in all)
        {
            // Keep Animator and any render helpers; remove everything else
            if (mb is Animator) continue;
            Destroy(mb);
        }
    }
}
