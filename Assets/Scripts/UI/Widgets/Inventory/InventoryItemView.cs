using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class InventoryItemView : MonoBehaviour,
    IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
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

    private void OnEnable()
    {
        // Debug.Log($"[ItemView] ENABLE name='{name}' " +
        // //           $"raycastTarget={raw?.raycastTarget} layer={gameObject.layer} " +
        // //           $"hasDragCtrl={(dragCtrl != null)} hasItem={(item != null)} hasDef={(item?.def != null)}");
    }

    public void OnPointerEnter(PointerEventData e)
    {
        // Debug.Log($"[ItemView] ENTER '{name}'");
    }

    public void OnPointerExit(PointerEventData e)
    {
        // Debug.Log($"[ItemView] EXIT '{name}'");
    }

    public void OnPointerDown(PointerEventData e)
    {
        // Debug.Log($"[ItemView] DOWN '{name}' btn={e.button} pos={e.position}");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Debug.Log($"[ItemView] CLICK '{name}' btn={eventData.button} " +
        //           $"dragCtrlNull={(dragCtrl == null)}");

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            dragCtrl?.OnItemClicked(this); // forwards to InventoryDragController
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right && item != null && item.def != null)
        {
            if (equipment != null && equipment.TryEquip(item.def))
                inventory?.Remove(item);
        }
    }
}
