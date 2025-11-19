// Assets/Scripts/UI/Widgets/Inventory/InventoryItemView.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Items;

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
        //           $"raycastTarget={raw?.raycastTarget} layer={gameObject.layer} " +
        //           $"hasDragCtrl={(dragCtrl != null)} hasItem={(item != null)} hasDef={(item?.def != null)}");
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

        // LEFT = drag/select (existing behavior)
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            dragCtrl?.OnItemClicked(this); // forwards to InventoryDragController
            return;
        }

        // RIGHT = use/equip from inventory
        if (eventData.button == PointerEventData.InputButton.Right && item != null && item.def != null)
        {
            if (equipment != null)
            {
                bool usedOrEquipped = equipment.TryEquip(item.def, fromInventory: true);

                // For now: if TryEquip / TryUsePotion returns true, remove one item from inventory.
                // (When we add stacking, this will consume 1 stack.)
                if (usedOrEquipped)
                {
                    Debug.Log($"[ItemView] Right-click use/equip succeeded for '{item.def.displayName}', removing from inventory.");
                    if (item.def is PotionItemDefinition)
                    {
                        if (inventory != null && !inventory.ConsumeOne(item))
                        {
                            Debug.LogWarning("[ItemView] ConsumeOne failed, removing entire potion item.");
                            inventory.Remove(item);
                        }
                    }
                    else
                    {
                        inventory?.Remove(item);
                    }
                }
                else
                {
                    Debug.Log($"[ItemView] Right-click use/equip FAILED for '{item.def.displayName}'.");
                }
            }
        }
    }
}
