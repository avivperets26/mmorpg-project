using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Minimal item UI controller:
/// - shows a tooltip on hover
/// - gentle scale on hover
/// - drag follows mouse
/// - on drag end:
///     * if released outside the InventoryPanel -> asks parent to drop to world
///     * if released inside -> notifies parent with local UI position for snapping
/// Parent can listen via SendMessageUpwards:
///   void OnRequestWorldDrop(ItemUI ui)
///   void OnItemDroppedInInventory(ItemUI ui, Vector2 localPos)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ItemUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Wiring")]
    public Image iconImage;                  // optional: for 2D temp icon
    public RectTransform inventoryRoot;      // the InventoryPanel RectTransform
    public TooltipUI tooltip;                // your TooltipUI instance

    [Header("Tooltip")]
    [TextArea] public string tooltipText = "";

    [Header("Drag & Drop")]
    public LayerMask groundMask;             // set to Ground for world drop raycast
    public float hoverScale = 1.04f;

    RectTransform rt;
    Canvas canvas;
    Transform originalParent;
    int originalSibling;
    Vector3 baseScale;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        baseScale = transform.localScale;

        // Fallback: try to auto-find tooltip in parent canvas if not assigned
        if (!tooltip) tooltip = GetComponentInParent<TooltipUI>();
    }

    /// <summary>Optional helper to configure the visual + tooltip at runtime.</summary>
    public void Bind(Sprite icon, string info, RectTransform invRoot, TooltipUI tip = null)
    {
        if (iconImage) iconImage.sprite = icon;
        tooltipText = info;
        if (invRoot) inventoryRoot = invRoot;
        if (tip) tooltip = tip;
    }

    // -------- Hover --------
    public void OnPointerEnter(PointerEventData _)
    {
        transform.localScale = baseScale * hoverScale;
        if (!string.IsNullOrEmpty(tooltipText)) tooltip?.Show(tooltipText);
    }

    public void OnPointerExit(PointerEventData _)
    {
        transform.localScale = baseScale;
        tooltip?.Hide();
    }

    // -------- Drag --------
    public void OnBeginDrag(PointerEventData e)
    {
        originalParent = transform.parent;
        originalSibling = transform.GetSiblingIndex();

        // move to top canvas while dragging so it renders above everything
        transform.SetParent(canvas.transform, worldPositionStays: true);
        tooltip?.Hide();
    }

    public void OnDrag(PointerEventData e)
    {
        if (!canvas) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, e.position, e.pressEventCamera, out var lp))
        {
            rt.anchoredPosition = lp;
        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        bool overInventory = inventoryRoot &&
            RectTransformUtility.RectangleContainsScreenPoint(inventoryRoot, e.position, e.pressEventCamera);

        if (!overInventory)
        {
            // Ask parent to drop to world (parent decides where/if to spawn)
            SendMessageUpwards("OnRequestWorldDrop", this, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            // Provide inventory-local position for snapping to grid
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    inventoryRoot, e.position, e.pressEventCamera, out var localPos))
            {
                // Parent should compute target cell from localPos and snap/place.
                SendMessageUpwards("OnItemDroppedInInventory", new ItemDropPayload(this, localPos),
                    SendMessageOptions.DontRequireReceiver);
            }
        }

        // Return under original parent hierarchy (parent may reparent again after handling)
        transform.SetParent(originalParent, worldPositionStays: false);
        transform.SetSiblingIndex(originalSibling);
        rt.anchoredPosition = Vector2.zero;
    }

    // Helper payload so parent can receive both UI and local position cleanly
    public struct ItemDropPayload
    {
        public ItemUI ui;
        public Vector2 localPos;
        public ItemDropPayload(ItemUI ui, Vector2 localPos) { this.ui = ui; this.localPos = localPos; }
    }
}
