using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class InventoryItemView : MonoBehaviour, IPointerClickHandler
{
    public InventoryItem item;
    public RectTransform container;
    public RawImage raw;
    public InventoryDragController dragCtrl;
    public Texture previewTexture;
    public EquipmentController equipment;
    public PlayerInventory inventory;

    private void Awake()
    {
        if (!raw) raw = GetComponent<RawImage>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // pick up / place inside grid (existing behavior)
            dragCtrl?.OnItemClicked(this);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right && item != null && item.def != null)
        {
            // Equip directly from inventory
            if (equipment != null && equipment.TryEquip(item.def))
            {
                // remove from inventory grid
                inventory?.Remove(item);
            }
        }
    }
}
