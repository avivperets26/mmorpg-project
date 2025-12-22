// Assets/Scripts/UI/Panels/MiniMapController.cs
using UnityEngine;

[DisallowMultipleComponent]
public class MiniMapController : MonoBehaviour
{
    // --------------------------------------------------
    // Wiring
    // --------------------------------------------------
    [Header("Wiring")]
    [Tooltip("Orthographic camera rendering the minimap to a RenderTexture.")]
    [SerializeField] private Camera minimapCamera;

    [Tooltip("Transform that actually moves (player root/controller). If empty, will auto-find by Player tag.")]
    [SerializeField] private Transform target;

    [Tooltip("Optional: MiniMapPanel RectTransform. If empty, uses this object's RectTransform.")]
    [SerializeField] private RectTransform miniMapPanel;

    // --------------------------------------------------
    // UI
    // --------------------------------------------------
    [Header("UI")]
    [Tooltip("UI arrow at the center of the minimap (MM Marker).")]
    [SerializeField] private RectTransform marker;

    [Tooltip("If true, rotates the marker based on target Y rotation.")]
    [SerializeField] private bool rotateMarker = true;

    [Tooltip("If false, marker stays fixed (0 rotation).")]
    [SerializeField] private bool markerStaysFixed = false;

    // --------------------------------------------------
    // Follow
    // --------------------------------------------------
    [Header("Follow")]
    [Tooltip("Height above the target (Y offset for the minimap camera).")]
    [SerializeField] private float height = 25f;

    [Tooltip("Optional world-space offset from the target position.")]
    [SerializeField] private Vector3 offset = Vector3.zero;

    [Tooltip("If true, rotate the minimap camera with the target yaw (rotating-map style).")]
    [SerializeField] private bool rotateMapWithTarget = false;

    // --------------------------------------------------
    // Zoom
    // --------------------------------------------------
    [Header("Zoom (Camera Ortho Size)")]
    [SerializeField] private float zoomStep = 4f;
    [SerializeField] private float minZoom = 12f;
    [SerializeField] private float maxZoom = 70f;

    private void Awake()
    {
        if (!miniMapPanel)
            miniMapPanel = transform as RectTransform;
    }

    private void LateUpdate()
    {
        if (!minimapCamera)
            return;

        ResolveTarget();
        if (!target)
            return;

        UpdateCamera();
        UpdateMarker();
    }

    // --------------------------------------------------
    // Internal
    // --------------------------------------------------
    private void ResolveTarget()
    {
        if (target) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
            target = player.transform;
    }

    private void UpdateCamera()
    {
        Vector3 worldPos = target.position + offset;

        minimapCamera.transform.position = new Vector3(
            worldPos.x,
            worldPos.y + height,
            worldPos.z
        );

        float yaw = rotateMapWithTarget ? target.eulerAngles.y : 0f;
        minimapCamera.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
    }

    private void UpdateMarker()
    {
        if (!marker) return;

        if (!rotateMarker || markerStaysFixed)
        {
            marker.localEulerAngles = Vector3.zero;
            return;
        }

        // North-up minimap: rotate marker instead of map.
        // UI rotates around Z; negate to match world yaw direction.
        float yaw = target.eulerAngles.y;
        marker.localEulerAngles = new Vector3(0f, 0f, -yaw);
    }

    // --------------------------------------------------
    // UI Button Hooks
    // --------------------------------------------------
    public void ZoomIn()
    {
        if (!minimapCamera || !minimapCamera.orthographic)
            return;

        minimapCamera.orthographicSize = Mathf.Clamp(
            minimapCamera.orthographicSize - zoomStep,
            minZoom,
            maxZoom
        );
    }

    public void ZoomOut()
    {
        if (!minimapCamera || !minimapCamera.orthographic)
            return;

        minimapCamera.orthographicSize = Mathf.Clamp(
            minimapCamera.orthographicSize + zoomStep,
            minZoom,
            maxZoom
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (height < 0f) height = 0f;
        if (zoomStep < 0.01f) zoomStep = 0.01f;
        if (minZoom < 0.1f) minZoom = 0.1f;
        if (maxZoom < minZoom) maxZoom = minZoom + 0.1f;
    }
#endif
}
