using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Swaps a static preview into a live preview while hovered and rotates it.
/// On exit, eases back to original Y rotation and restores the static RT.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class ItemPreviewHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Runtime wiring (set by InventoryUI)")]
    public ItemDefinition def;
    public int rtWidth;
    public int rtHeight;
    public Texture initialStaticTexture;   // the cached static RT from first render

    [Header("Spin Tuning")]
    public float spinDegreesPerSecond = 40f;     // how fast to rotate while hovered
    public float returnDegreesPerSecond = 180f;  // how fast to return to 0 when not hovered
    public float epsilonDegrees = 0.5f;          // when we consider "returned"

    private RawImage _raw;
    private bool _hovering;

    // Live renderer handle/state
    private ItemPreviewRenderer.LivePreview _live;
    private float _yAngle; // degrees relative to base rotation

    private void Awake()
    {
        _raw = GetComponent<RawImage>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_hovering || def == null) return;
        _hovering = true;

        // Start a live preview session and swap the texture
        _live = ItemPreviewRenderer.Instance.BeginLive(def, rtWidth, rtHeight);
        _yAngle = 0f;
        if (_live != null && _live.rt != null)
        {
            _raw.texture = _live.rt;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovering = false;
        // End immediately to avoid overlapping live previews from other slots.
        EndLiveAndRestore(immediate: true);
    }


    private void Update()
    {
        if (_live == null) return;

        if (_hovering)
        {
            _yAngle += spinDegreesPerSecond * Time.deltaTime;
            if (_yAngle >= 360f) _yAngle -= 360f;
        }
        else
        {
            // Not hovering anymore: we now end live immediately in OnPointerExit.
            return;
        }

        if (_live.modelRoot != null)
            _live.modelRoot.rotation = Quaternion.AngleAxis(_yAngle, Vector3.up) * _live.baseWorldRotation;

        ItemPreviewRenderer.Instance.RenderFrame(_live);
    }


    private void OnDisable()
    {
        EndLiveAndRestore(immediate: true);
    }

    private void EndLiveAndRestore(bool immediate = false)
    {
        if (_live != null)
        {
            ItemPreviewRenderer.Instance.EndLive(_live);
            _live = null;
        }

        if (_raw != null && initialStaticTexture != null)
            _raw.texture = initialStaticTexture;

        if (immediate) _yAngle = 0f;
    }
}
