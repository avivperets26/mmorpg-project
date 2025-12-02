// Assets\Scripts\UI\Widgets\Tooltips\InventorySlotTooltip.cs
using UnityEngine;
using UnityEngine.EventSystems;
using Game.Items;

public class InventorySlotTooltip : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IBeginDragHandler
{
    public ItemInstance itemInstance;
    [SerializeField] private ItemTooltipUI tooltip;
    [SerializeField] private TooltipCompareOrchestrator orchestrator;

    public RectTransform targetOverride;

    [Header("Debugging")]
    [SerializeField] private bool enableLogs = false;
    private string _tag => "InventorySlot";

    void Awake()
    {
        if (!orchestrator)
        {
            var inParent = GetComponentInParent<TooltipCompareOrchestrator>(true);
            if (inParent) orchestrator = inParent;
            else
            {
#if UNITY_2023_1_OR_NEWER
                orchestrator = Object.FindFirstObjectByType<TooltipCompareOrchestrator>(FindObjectsInactive.Include);
#else
                orchestrator = Object.FindObjectOfType<TooltipCompareOrchestrator>(true);
#endif
            }
        }
    }

    void OnEnable()
    {
        TryShowIfMouseIsOnMe();
        UITooltipDebug.Log(enableLogs, this, _tag, "OnEnable() -> TryShowIfMouseIsOnMe()");
    }

    void OnDisable()
    {
        if (tooltip) tooltip.HideOwner(this);
        UITooltipDebug.Log(enableLogs, this, _tag, "OnDisable() -> HideOwner");
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (orchestrator == null || itemInstance == null || itemInstance.def == null) return;
        var target = targetOverride ? targetOverride : (RectTransform)transform;
        UITooltipDebug.Log(enableLogs, this, _tag, $"Enter '{gameObject.name}' item='{itemInstance.def.displayName}'");
        orchestrator.ShowForInventory(itemInstance, target);
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (orchestrator) { orchestrator.HideBoth(); }
        UITooltipDebug.Log(enableLogs, this, _tag, "Exit -> HideBoth()");
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (tooltip) tooltip.HideOwner(this);
        UITooltipDebug.Log(enableLogs, this, _tag, "PointerDown -> HideOwner()");
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (tooltip) tooltip.HideOwner(this);
        UITooltipDebug.Log(enableLogs, this, _tag, "BeginDrag -> HideOwner()");
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
