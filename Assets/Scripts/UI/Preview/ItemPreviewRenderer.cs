using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Offscreen "studio" that frames a prefab and renders it to a RenderTexture.
/// Keeps a hidden camera disabled between renders. Supports non-square RTs.
/// </summary>
public class ItemPreviewRenderer : MonoBehaviour
{
    private static ItemPreviewRenderer _instance;
    public static ItemPreviewRenderer Instance
    {
        get
        {
            if (_instance) return _instance;
            var go = new GameObject("_ItemPreviewRenderer");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ItemPreviewRenderer>();
            _instance.InitStudio();
            return _instance;
        }
    }

    private Camera _cam;
    private Light _keyLight;
    private Transform _stage;     // parent for instantiated models
    private int _previewLayer;    // -1 if layer not found

    // Cache per (ItemDefinition, width, height)
    private readonly Dictionary<(ItemDefinition, int, int), RenderTexture> _cache =
        new Dictionary<(ItemDefinition, int, int), RenderTexture>();

    private void InitStudio()
    {
        // Put the whole preview rig far away from the playable world
        transform.position = new Vector3(0f, 5000f, 0f);

        // Layer that preview models will use
        _previewLayer = LayerMask.NameToLayer("InventoryPreview"); // -1 if missing

        // --- Stage root -------------------------------------------------------
        _stage = new GameObject("Stage").transform;
        _stage.SetParent(transform, false);
        _stage.localPosition = Vector3.zero;
        _stage.localRotation = Quaternion.identity;
        _stage.localScale = Vector3.one;

        // --- Preview camera ---------------------------------------------------
        var camGO = new GameObject("PreviewCamera");
        camGO.transform.SetParent(transform, false);
        camGO.transform.localPosition = new Vector3(0f, 0f, -3f);
        camGO.transform.localRotation = Quaternion.identity;

        _cam = camGO.AddComponent<Camera>();
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = new Color(0, 0, 0, 0);
        _cam.orthographic = false;
        _cam.fieldOfView = 30f;
        _cam.nearClipPlane = 0.01f;
        _cam.farClipPlane = 100f;

        // Keep the preview camera completely manual / sandboxed
        _cam.allowHDR = false;
        _cam.allowMSAA = true;
        _cam.enabled = false;
        _cam.cullingMask = 0; // we set it per render based on the model's layer

        // ---- URP config: NO POST PROCESSING / NO VOLUMES ---------------------
        var urp = camGO.GetComponent<UniversalAdditionalCameraData>();
        if (urp == null)
            urp = camGO.AddComponent<UniversalAdditionalCameraData>();

        urp.renderPostProcessing = false;          // <- critical: no exposure / bloom / etc.
        urp.volumeLayerMask = 0;              // ignore all volumes
        urp.renderShadows = false;
        urp.requiresColorOption = CameraOverrideOption.Off;
        urp.requiresDepthOption = CameraOverrideOption.Off;
        urp.cameraStack.Clear();                   // make sure it’s not stacked anywhere

        // --- Key light for the preview ---------------------------------------
        var lightGO = new GameObject("KeyLight");
        lightGO.transform.SetParent(transform, false);

        _keyLight = lightGO.AddComponent<Light>();

        // Use a small spot light so it cannot flood the main scene
        _keyLight.type = LightType.Spot;
        _keyLight.range = 5f;
        _keyLight.spotAngle = 45f;
        _keyLight.intensity = 1.15f;
        _keyLight.bounceIntensity = 0f;
        _keyLight.shadowStrength = 0f;

        // Position the light to nicely hit the model on the stage
        lightGO.transform.localPosition = new Vector3(0.8f, 1.3f, -1.2f);
        lightGO.transform.LookAt(_stage.position + new Vector3(0f, 0.4f, 0f));

        if (_previewLayer >= 0)
        {
            _keyLight.cullingMask = 1 << _previewLayer;
        }
        else
        {
            _keyLight.cullingMask = 0;
            // Debug.LogWarning(
            //     "[ItemPreviewRenderer] Layer 'InventoryPreview' not found. " +
            //     "KeyLight cullingMask set to 0 to avoid affecting the main scene."
            // );
        }
    }



    /// <summary>
    /// Backward-compatible square render. (size x size)
    /// </summary>
    public RenderTexture Render(ItemDefinition def, int size)
    {
        size = Mathf.Max(64, size);
        return Render(def, size, size);
    }

    /// <summary>
    /// Aspect-aware render. widthPx/heightPx determine RT aspect and framing.
    /// </summary>
    public RenderTexture Render(ItemDefinition def, int widthPx, int heightPx)
    {
        if (!def) return null;

        widthPx = Mathf.Max(64, widthPx);
        heightPx = Mathf.Max(64, heightPx);

        var prefab = def.inventoryPreviewPrefab ? def.inventoryPreviewPrefab : def.worldPrefab;
        if (!prefab)
        {
            // Debug.LogWarning($"[ItemPreviewRenderer] No preview/world prefab on '{def.displayName}'.");
            return null;
        }

        // 🔍 DEBUG: log children on stage before we do anything
        // Debug.Log($"[ItemPreviewRenderer] Render START def='{def.displayName}', " +
        //           $"stageChildren(before)={_stage.childCount}");

        // 🛡️ 1) Temporarily hide any existing children on the stage
        //      (live previews, stale objects, etc.) so this render
        //      only sees the model we are about to spawn.
        var hiddenChildren = new List<GameObject>();
        foreach (Transform child in _stage)
        {
            if (!child) continue;
            var goChild = child.gameObject;
            if (goChild.activeSelf)
            {
                hiddenChildren.Add(goChild);
                goChild.SetActive(false);
            }
        }

        // Debug.Log($"[ItemPreviewRenderer] Render def='{def.displayName}' " +
        //           $"hid {hiddenChildren.Count} stage child(ren) during icon capture.");

        // Debug.Log(
        //     $"[ItemPreviewRenderer] Using prefab '{prefab.name}' for def='{def.displayName}' " +
        //     $"(inventoryPreview? {def.inventoryPreviewPrefab == prefab})"
        // );

        // Fresh RT for this preview
        var rt = new RenderTexture(widthPx, heightPx, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4
        };
        rt.Create();
        // Debug.Log(
        //     $"[ItemPreviewRenderer] Render def='{def.displayName}' " +
        //     $"size={widthPx}x{heightPx}, rtID={rt.GetInstanceID()}"
        // );

        // Instance model on the stage
        var go = Instantiate(prefab, _stage);
        go.name = $"Preview_{def.displayName}";

        // 🔍 DEBUG: log meshes inside this prefab
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = null;
            var mf = r.GetComponent<MeshFilter>();
            var smr = r as SkinnedMeshRenderer;
            if (mf != null) mesh = mf.sharedMesh;
            else if (smr != null) mesh = smr.sharedMesh;

            // Debug.Log($"[ItemPreviewRenderer] def='{def.displayName}' " +
            //           $"child='{r.gameObject.name}' mesh='{mesh?.name ?? "null"}'");
        }

        int modelLayer = (_previewLayer >= 0) ? _previewLayer : go.layer;
        SetLayerRecursively(go, modelLayer);
        _cam.cullingMask = 1 << modelLayer;

        Transform modelRoot = go.transform;
        if (!string.IsNullOrEmpty(def.preview?.modelRootPath))
        {
            var child = go.transform.Find(def.preview.modelRootPath);
            if (child) modelRoot = child;
        }

        if (def.preview != null)
        {
            modelRoot.localPosition += def.preview.positionOffset;
            modelRoot.localRotation *= Quaternion.Euler(def.preview.rotationOffsetEuler);
            modelRoot.localScale *= Mathf.Max(0.001f, def.preview.scale);
        }

        float padding = def.preview != null ? def.preview.padding : 1.1f;
        float aspect = (float)widthPx / heightPx;
        FrameModel(modelRoot, padding, aspect);

        // Render into RT
        var prevTarget = _cam.targetTexture;
        _cam.targetTexture = rt;

        // Transparent clear, so no “ghost” from previous renders
        var prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, new Color(0f, 0f, 0f, 0f));
        RenderTexture.active = prevActive;

        _cam.enabled = true;
        _cam.Render();
        _cam.enabled = false;
        _cam.targetTexture = prevTarget;

        // Remove our temporary preview instance
        Destroy(go);

        // 🛡️ 2) Re-enable previously hidden children (live previews, etc.)
        for (int i = 0; i < hiddenChildren.Count; i++)
        {
            if (hiddenChildren[i] != null)
                hiddenChildren[i].SetActive(true);
        }

        // Debug.Log($"[ItemPreviewRenderer] Render END def='{def.displayName}', " +
        //           $"stageChildren(after)={_stage.childCount}");

        return rt;
    }




    private void FrameModel(Transform modelRoot, float padding, float aspect)
    {
        var renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        Bounds bounds;
        if (renderers.Length == 0)
            bounds = new Bounds(modelRoot.position, Vector3.one * 0.5f);
        else
        {
            bounds = new Bounds(renderers[0].bounds.center, renderers[0].bounds.size);
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
        }

        PositionCameraForBounds(bounds, padding, aspect);
    }

    private void PositionCameraForBounds(Bounds worldBounds, float padding, float aspect)
    {
        padding = Mathf.Max(1.0f, padding);
        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;

        // Vertical FOV is fixed; compute the required distance to fit both axes.
        float vFovRad = _cam.fieldOfView * Mathf.Deg2Rad;

        // vertical fit
        float distY = (extents.y * padding) / Mathf.Tan(vFovRad * 0.5f);

        // horizontal fit uses horizontal FOV derived from vertical FOV + aspect
        float hFovRad = 2f * Mathf.Atan(Mathf.Tan(vFovRad * 0.5f) * aspect);
        float distX = (extents.x * padding) / Mathf.Tan(hFovRad * 0.5f);

        float dist = Mathf.Max(distX, distY) + extents.z * padding + 0.1f;

        _cam.transform.position = center + new Vector3(0, 0, -dist);
        _cam.transform.rotation = Quaternion.LookRotation(center - _cam.transform.position, Vector3.up);
        _cam.nearClipPlane = Mathf.Max(0.01f, dist * 0.05f);
        _cam.farClipPlane = dist * 4f;
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        if (layer < 0) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    // Add this nested handle type inside ItemPreviewRenderer
    public class LivePreview
    {
        public GameObject go;
        public Transform modelRoot;
        public RenderTexture rt;
        public Quaternion baseLocalRotation;
        public Quaternion baseWorldRotation;
        public int width;
        public int height;
    }

    /// <summary>
    /// Starts a live (per-frame) preview for hover. Caller must later call EndLive.
    /// </summary>
    public LivePreview BeginLive(ItemDefinition def, int widthPx, int heightPx)
    {
        if (!def) return null;

        widthPx = Mathf.Max(64, widthPx);
        heightPx = Mathf.Max(64, heightPx);

        var prefab = def.inventoryPreviewPrefab ? def.inventoryPreviewPrefab : def.worldPrefab;
        if (!prefab) return null;

        var lp = new LivePreview
        {
            width = widthPx,
            height = heightPx,
            rt = new RenderTexture(widthPx, heightPx, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4
            }
        };
        lp.rt.Create();

        // Instance to stage
        lp.go = Instantiate(prefab, _stage);
        lp.go.name = $"LivePreview_{def.displayName}";

        int modelLayer = (_previewLayer >= 0) ? _previewLayer : lp.go.layer;
        SetLayerRecursively(lp.go, modelLayer);
        _cam.cullingMask = 1 << modelLayer;

        lp.modelRoot = lp.go.transform;
        if (!string.IsNullOrEmpty(def.preview?.modelRootPath))
        {
            var child = lp.go.transform.Find(def.preview.modelRootPath);
            if (child) lp.modelRoot = child;
        }

        // Apply preview transforms
        if (def.preview != null)
        {
            lp.modelRoot.localPosition += def.preview.positionOffset;
            lp.modelRoot.localRotation *= Quaternion.Euler(def.preview.rotationOffsetEuler);
            lp.modelRoot.localScale *= Mathf.Max(0.001f, def.preview.scale);
        }

        lp.baseLocalRotation = lp.modelRoot.localRotation;
        lp.baseWorldRotation = lp.modelRoot.rotation;

        float padding = def.preview != null ? def.preview.padding : 1.1f;
        float aspect = (float)widthPx / heightPx;
        FrameModel(lp.modelRoot, padding, aspect);

        return lp;
    }

    /// <summary>Renders one frame into the live RT.</summary>
    public void RenderFrame(LivePreview lp)
    {
        if (lp == null || lp.rt == null) return;
        var prevTarget = _cam.targetTexture;
        _cam.targetTexture = lp.rt;
        _cam.enabled = true;
        _cam.Render();
        _cam.enabled = false;
        _cam.targetTexture = prevTarget;
    }

    /// <summary>Ends a live preview and disposes resources.</summary>
    public void EndLive(LivePreview lp)
    {
        if (lp == null) return;
        if (lp.go) Destroy(lp.go);
        if (lp.rt != null)
        {
            lp.rt.Release();
            Object.Destroy(lp.rt);
        }
    }
}
