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
        if (!tooltip)
#if UNITY_2023_1_OR_NEWER
            tooltip = Object.FindFirstObjectByType<ItemTooltipUI>(FindObjectsInactive.Include);
#else
            tooltip = Object.FindObjectOfType<ItemTooltipUI>(true);
#endif
    }

    void OnDisable() { tooltip?.Hide(); }

    public void OnPointerEnter(PointerEventData e)
    {
        if (itemInstance == null || tooltip == null) return;
        var target = targetOverride ? targetOverride : (RectTransform)transform;
        tooltip.Show(itemInstance, target);
    }

    public void OnPointerExit(PointerEventData e) { tooltip?.Hide(); }
    public void OnPointerDown(PointerEventData e) { tooltip?.Hide(); }
    public void OnBeginDrag(PointerEventData e) { tooltip?.Hide(); }
}
