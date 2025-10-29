// Assets/Scripts/Inventory/ItemPreviewRenderer.cs
using UnityEngine;

public class ItemPreviewRenderer
{
    private const string PreviewLayerName = "InventoryPreview";

    private static ItemPreviewRenderer _instance;
    public static ItemPreviewRenderer Instance => _instance ??= new ItemPreviewRenderer();

    private Camera _cam;
    private Transform _root;
    private Light _keyLight;
    private int _layer;
    private bool _initialized;

    private ItemPreviewRenderer() { }

    private void EnsureInit()
    {
        if (_initialized) return;

        _layer = LayerMask.NameToLayer(PreviewLayerName);
        if (_layer < 0)
        {
            Debug.LogWarning($"[Preview] Layer '{PreviewLayerName}' not found. Using Default (0). " +
                             "Tip: add an 'InventoryPreview' layer to isolate previews.");
            _layer = 0; // Default
        }

        var goRoot = new GameObject("[InventoryPreviewRoot]");
        Object.DontDestroyOnLoad(goRoot);
        goRoot.hideFlags = HideFlags.HideAndDontSave;
        _root = goRoot.transform;

        var camGO = new GameObject("PreviewCamera");
        camGO.transform.SetParent(_root, false);
        _cam = camGO.AddComponent<Camera>();
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = new Color(0, 0, 0, 0);
        _cam.cullingMask = 1 << _layer;
        _cam.orthographic = false;
        _cam.nearClipPlane = 0.01f;
        _cam.farClipPlane = 100f;
        _cam.fieldOfView = 35f; // a bit tighter for items
        _cam.enabled = false;

        var lightGO = new GameObject("KeyLight");
        lightGO.transform.SetParent(_root, false);
        _keyLight = lightGO.AddComponent<Light>();
        _keyLight.type = LightType.Directional;
        _keyLight.intensity = 1.35f;
        _keyLight.color = Color.white;
        _keyLight.transform.rotation = Quaternion.Euler(30, -30, 0);

        _initialized = true;
    }

    public RenderTexture Render(ItemDefinition def, int size = 256)
    {
        EnsureInit();

        var prefab = def.previewPrefab != null ? def.previewPrefab : def.worldPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[Preview] No prefab on '{def.name}'.");
            return MakeBlank(size);
        }

        // Clone the prefab under hidden root
        var instance = Object.Instantiate(prefab, _root);
        instance.name = $"{def.name}_Preview";
        SetLayerRecursively(instance, _layer);

        // Locate the subtree to actually render (e.g., "Model")
        Transform modelRoot = instance.transform;
        if (!string.IsNullOrEmpty(def.preview.modelRootPath))
        {
            var t = instance.transform.Find(def.preview.modelRootPath);
            if (t != null) modelRoot = t;
        }

        // Strip any in-world UI/labels so they don't show in preview
        DisablePreviewNoise(modelRoot);

        // Apply per-item overrides (rotation/scale/offset for PREVIEW ONLY)
        modelRoot.localPosition += def.preview.positionOffset;
        modelRoot.localRotation = Quaternion.Euler(def.preview.rotationOffsetEuler) * modelRoot.localRotation;
        modelRoot.localScale *= Mathf.Max(0.0001f, def.preview.scale);

        // Compute bounds ONLY from renderers under modelRoot
        var bounds = CalculateRendererBounds(modelRoot.gameObject);
        if (bounds.size == Vector3.zero)
        {
            Debug.LogWarning($"[Preview] '{def.name}' has no active Renderers under '{modelRoot.name}'.");
            Object.DestroyImmediate(instance);
            return MakeBlank(size);
        }

        // Frame the model nicely
        FrameModel(bounds, Mathf.Max(1f, def.preview.padding));

        // Render to RT
        var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        rt.Create();

        var old = RenderTexture.active;
        _cam.targetTexture = rt;
        RenderTexture.active = rt;
        _cam.Render();
        RenderTexture.active = old;
        _cam.targetTexture = null;

        Object.DestroyImmediate(instance);
        return rt;
    }

    private void FrameModel(Bounds b, float padding)
    {
        // Fit largest dimension using perspective FOV
        float maxSize = Mathf.Max(b.size.x, b.size.y, b.size.z);
        float dist = (maxSize * 0.5f * padding) / Mathf.Tan(_cam.fieldOfView * Mathf.Deg2Rad * 0.5f);

        Vector3 center = b.center;
        // Pull the camera back along -Z and tilt a little for style
        _cam.transform.position = center + new Vector3(0, 0, -(dist + Mathf.Abs(b.extents.z)));
        _cam.transform.LookAt(center);
        _cam.transform.rotation *= Quaternion.Euler(10f, 20f, 0);

        _keyLight.transform.rotation = _cam.transform.rotation;
    }

    private static void DisablePreviewNoise(Transform root)
    {
        // Disable any Canvases / TextMeshPro / legacy Text
        foreach (var c in root.GetComponentsInChildren<Canvas>(true)) c.enabled = false;
        foreach (var gr in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) gr.enabled = false;
#if TMP_PRESENT
        foreach (var tmp in root.GetComponentsInChildren<TMPro.TMP_Text>(true)) tmp.enabled = false;
#endif
        // Common name-based cleanups (labels, gizmos)
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.Contains("Label", System.StringComparison.OrdinalIgnoreCase) ||
                t.name.Contains("Text", System.StringComparison.OrdinalIgnoreCase))
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    private static Bounds CalculateRendererBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform t in obj.transform) SetLayerRecursively(t.gameObject, layer);
    }

    private static RenderTexture MakeBlank(int size)
    {
        var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        return rt;
    }
}
