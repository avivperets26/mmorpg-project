using UnityEngine;
using UnityEngine.EventSystems;
using Game.Items;

public class InventorySlotTooltip : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IBeginDragHandler
{
    public ItemInstance itemInstance;
    [SerializeField] private ItemTooltipUI tooltip;
    public RectTransform targetOverride;

    void Awake()
    {
        // Prefer the closest tooltip in our canvas; fallback to a global search (including inactive)
        if (!tooltip)
        {
            var canvas = GetComponentInParent<Canvas>(true);
            if (canvas)
                tooltip = canvas.GetComponentInChildren<ItemTooltipUI>(true);

            if (!tooltip)
#if UNITY_2023_1_OR_NEWER
                tooltip = Object.FindFirstObjectByType<ItemTooltipUI>(FindObjectsInactive.Include);
#else
                tooltip = Object.FindObjectOfType<ItemTooltipUI>(true);
#endif
        }
    }

    void OnEnable()
    {
        // If the cursor is already over this slot when it appears, synthesize an enter.
        TryShowIfMouseIsOnMe();
    }

    void OnDisable()
    {
        // Only the slot that owns the tooltip is allowed to hide it
        if (tooltip) tooltip.HideOwner(this);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        // hard guard: no tooltip for empty slots
        if (tooltip == null || itemInstance == null || itemInstance.def == null)
            return;

        var target = targetOverride ? targetOverride : (RectTransform)transform;
        tooltip.ShowFrom(this, itemInstance, target);
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (tooltip) tooltip.HideOwner(this);
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (tooltip) tooltip.HideOwner(this);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (tooltip) tooltip.HideOwner(this);
    }

    // ---- helper ----
    private void TryShowIfMouseIsOnMe()
    {
        if (itemInstance == null || tooltip == null || EventSystem.current == null) return;

        // Current mouse position (both input systems supported)
        Vector2 pos;
#if ENABLE_INPUT_SYSTEM
        pos = UnityEngine.InputSystem.Mouse.current != null
            ? UnityEngine.InputSystem.Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;
#else
        pos = (Vector2)Input.mousePosition;
#endif

        // If the slot’s rect contains the cursor, simulate an enter now.
        var myRect = (RectTransform)transform;

        var canvas = tooltip.GetComponentInParent<Canvas>();
        var cam = (canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null
            : Camera.main;

        if (RectTransformUtility.RectangleContainsScreenPoint(myRect, pos, cam))
        {
            var es = EventSystem.current;
            var ped = new PointerEventData(es) { position = pos };
            OnPointerEnter(ped);
        }
    }
}

// ---- tiny helper to print full hierarchy path (for logs) ----
public static class TransformPathExtensions
{
    public static string GetHierarchyPath(this Transform t)
    {
        if (!t) return "<null>";
        var path = t.name;
        while (t.parent)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
