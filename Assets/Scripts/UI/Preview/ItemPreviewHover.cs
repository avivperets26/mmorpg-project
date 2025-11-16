// Assets/Scripts/UI/Widgets/Inventory/ItemPreviewHover.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Items; // for ItemDefinition

/// <summary>
/// Handles per-slot hover spin using ItemPreviewRenderer.LivePreview.
/// Each instance only ever touches its own RawImage.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class ItemPreviewHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Data")]
    public ItemDefinition def;
    public int rtWidth = 256;
    public int rtHeight = 256;
    public Texture initialStaticTexture; // set by InventoryUI when it creates the icon

    [Header("Spin")]
    public float spinDegreesPerSecond = 40f;
    public float returnDegreesPerSecond = 180f; // (unused now but kept for tuning later)

    private RawImage _raw;
    private ItemPreviewRenderer.LivePreview _live;
    private bool _hovering;

    void Awake()
    {
        _raw = GetComponent<RawImage>();
        if (initialStaticTexture == null && _raw != null)
            initialStaticTexture = _raw.texture;
    }

    void OnDisable()
    {
        StopLive();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovering = true;

        if (def == null || _raw == null)
            return;

        if (_live != null)
            return; // already running

        // Start a per-slot live preview
        _live = ItemPreviewRenderer.Instance.BeginLive(def, rtWidth, rtHeight);
        if (_live == null)
        {
            // fallback: keep static
            return;
        }

        // This slot now shows the live RT
        _raw.texture = _live.rt;

        // Kick off a simple spin loop
        StartCoroutine(SpinLoop());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovering = false;
        StopLive();
    }

    private System.Collections.IEnumerator SpinLoop()
    {
        while (_hovering && _live != null)
        {
            if (_live.modelRoot != null)
            {
                // Rotate around *world* Y so it always feels like left/right spin
                _live.modelRoot.Rotate(
                    Vector3.up,
                    spinDegreesPerSecond * Time.deltaTime,
                    Space.World
                );
            }

            // Render one frame into this slot's RT only
            ItemPreviewRenderer.Instance.RenderFrame(_live);

            yield return null;
        }
    }


    private void StopLive()
    {
        if (_live != null)
        {
            ItemPreviewRenderer.Instance.EndLive(_live);
            _live = null;
        }

        if (_raw != null && initialStaticTexture != null)
        {
            _raw.texture = initialStaticTexture;
        }
    }
}
