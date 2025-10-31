using UnityEngine;
using UnityEngine.EventSystems;
using Game.Items;

public class InventorySlotTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ItemInstance itemInstance;
    [SerializeField] private ItemTooltipUI tooltip; // drag your scene singleton or instantiate

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
        if (itemInstance != null && tooltip != null)
            tooltip.Show(itemInstance, transform as RectTransform);
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null) tooltip.Hide();
    }
}
