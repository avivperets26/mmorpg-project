using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a static live preview of the player model (no interaction).
/// - Renders a visual-only clone onto a RenderTexture shown in a RawImage.
/// - Recenters the clone by its bounds so feet are at y=0 and x/z are centered.
/// - Faces the camera using initialYaw + modelYawOffset (set 180 if prefab faces -Z).
/// </summary>
[RequireComponent(typeof(RawImage))]
public class CharacterPreviewController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Camera previewCameraPrefab;   // Solid Color, culling mask will be set here
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
    [Range(0f, 0.5f)][SerializeField] private float framePadding = 0.12f; // 12% margin
    [Range(0.5f, 2f)][SerializeField] private float zoom = 1.0f; // >1 = bigger, <1 = smaller


    private int _previewLayer;
    private RawImage _raw;
    private Camera _cam;
    private RenderTexture _rt;
    private Transform _pivot;
    private GameObject _clone;
    private Bounds _modelBoundsLocal;

    private void Awake()
    {
        _raw = GetComponent<RawImage>();
        _previewLayer = LayerMask.NameToLayer(previewLayerName);

        // RenderTexture target for the RawImage
        _rt = new RenderTexture(rtWidth, rtHeight, 24, RenderTextureFormat.ARGB32)
        {
            name = "CharacterPreviewRT"
        };
        _rt.Create();
        _raw.texture = _rt;

        // Mini-scene root (childed under this UI object for convenience)
        var root = new GameObject("PreviewRoot");
        root.transform.SetParent(transform, false);
        root.layer = _previewLayer;

        // Pivot (we’ll rotate/position relative to this)
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
        StripRuntimeScripts(_clone);     // keep only renderers/animators/etc.
        CenterCloneOnBounds(_clone.transform, out _modelBoundsLocal);
        // Make character face the camera (camera will be at world -Z looking toward +Z)
        // If prefab faces +Z in authoring → modelYawOffset = 0; if it faces -Z → 180.
        float totalYaw = 180f + initialYaw + modelYawOffset;
        _pivot.localRotation = Quaternion.Euler(0f, totalYaw, 0f);

        // Position & aim camera (decoupled from model yaw)
        PositionCamera();
        AutoFrame(); // ensure full body fits with padding
    }

    private void PositionCamera()
    {
        Vector3 target = _pivot.position + cameraOffset;

        // IMPORTANT: place the camera in world space, independent of the pivot rotation.
        // Camera sits on -Z and looks at target → model must face -Z to look at the camera.
        _cam.transform.position = target + Vector3.back * distance;
        _cam.transform.rotation = Quaternion.LookRotation(target - _cam.transform.position, Vector3.up);
    }

    /// <summary>Ensures the whole clone is on the preview layer.</summary>
    private void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursively(t.gameObject, layer);
    }

    /// <summary>Removes gameplay MonoBehaviours; keeps visuals/animators.</summary>
    private void StripRuntimeScripts(GameObject go)
    {
        var behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in behaviours)
            Destroy(mb);
        // Animators are not MonoBehaviours, so they remain.
    }

    /// <summary>
    /// Centers model: x/z centered, feet at y=0. Also returns local bounds (after centering).
    /// </summary>
    private void CenterCloneOnBounds(Transform t, out Bounds localBounds)
    {
        var rends = t.GetComponentsInChildren<Renderer>(true);
        localBounds = new Bounds(Vector3.zero, Vector3.zero);
        if (rends.Length == 0) return;

        // 1) Build combined WORLD bounds
        Bounds worldB = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) worldB.Encapsulate(rends[i].bounds);

        // 2) Convert the world center & a point at minY (same X/Z as center) to LOCAL
        Vector3 worldCenter = worldB.center;
        Vector3 worldMinPoint = new Vector3(worldCenter.x, worldB.min.y, worldCenter.z);

        Vector3 centerLocal = t.InverseTransformPoint(worldCenter);
        float minYLocal = t.InverseTransformPoint(worldMinPoint).y;

        // 3) Shift so X/Z are centered and feet rest on y=0
        Vector3 shift = new Vector3(centerLocal.x, minYLocal, centerLocal.z);
        t.localPosition -= shift;

        // 4) Recompute local bounds after the shift (more stable framing)
        //    We rebuild by transforming each renderer’s world bounds corners to local.
        bool first = true;
        foreach (var r in rends)
        {
            // approximate by using world bounds extents converted to a local AABB
            var wb = r.bounds;
            // sample 8 corners
            Vector3[] corners = new Vector3[8];
            Vector3 min = wb.min; Vector3 max = wb.max;
            int k = 0;
            for (int ix = 0; ix <= 1; ix++)
                for (int iy = 0; iy <= 1; iy++)
                    for (int iz = 0; iz <= 1; iz++)
                    {
                        corners[k++] = new Vector3(ix == 0 ? min.x : max.x, iy == 0 ? min.y : max.y, iz == 0 ? min.z : max.z);
                    }
            // accumulate in local space
            foreach (var c in corners)
            {
                Vector3 cl = t.InverseTransformPoint(c);
                if (first) { localBounds = new Bounds(cl, Vector3.zero); first = false; }
                else localBounds.Encapsulate(cl);
            }
        }
    }

    /// <summary>
    /// Push/pull camera so the model fits vertically with a small padding.
    /// Works with perspective cameras.
    /// </summary>
    private void AutoFrame()
    {
        if (!_cam || _modelBoundsLocal.size.sqrMagnitude < 1e-6f) return;

        // Bigger 'zoom' => smaller required height => camera moves closer
        float needHeight = (_modelBoundsLocal.size.y / Mathf.Max(zoom, 0.01f)) * (1f + framePadding * 2f);

        float vfov = _cam.fieldOfView * Mathf.Deg2Rad;
        float fitDist = (needHeight * 0.5f) / Mathf.Tan(vfov * 0.5f);

        distance = Mathf.Max(distance, fitDist);
        PositionCamera();
    }


    private void OnDestroy()
    {
        if (_rt != null)
        {
            if (_cam) _cam.targetTexture = null;
            _rt.Release();
            Destroy(_rt);
        }
    }
}
