// Assets/Scripts/UI/InventorySlotTooltip.cs
using UnityEngine;
using UnityEngine.EventSystems;
using Game.Items;

public class InventorySlotTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ItemInstance itemInstance;
    [SerializeField] private ItemTooltipUI tooltip;

    // NEW: which rect should the tooltip hug? (defaults to this)
    public RectTransform targetOverride;

    void Awake()
    {
        if (!tooltip)
        {
#if UNITY_2023_1_OR_NEWER
            tooltip = Object.FindFirstObjectByType<ItemTooltipUI>(FindObjectsInactive.Include);
#else
            tooltip = Object.FindObjectOfType<ItemTooltipUI>(true);
#endif
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemInstance == null || tooltip == null) return;
        var target = targetOverride ? targetOverride : (RectTransform)transform;   // <<< use container if provided
        tooltip.Show(itemInstance, target);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null) tooltip.Hide();
    }
}
