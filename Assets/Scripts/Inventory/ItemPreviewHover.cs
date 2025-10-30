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
        // We don't end live immediately; we animate back to 0 first.
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
            // Move back toward 0 smoothly
            _yAngle = Mathf.MoveTowardsAngle(_yAngle, 0f, returnDegreesPerSecond * Time.deltaTime);

            // If we reached 0, end live and restore static RT
            if (Mathf.Abs(Mathf.DeltaAngle(_yAngle, 0f)) <= epsilonDegrees)
            {
                EndLiveAndRestore();
                return;
            }
        }

        // Apply rotation around Y relative to the base preview rotation
        if (_live.modelRoot != null)
        {
            _live.modelRoot.rotation = Quaternion.AngleAxis(_yAngle, Vector3.up) * _live.baseWorldRotation;
        }

        // Re-render the frame into the live RT
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
