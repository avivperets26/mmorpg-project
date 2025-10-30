using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class InventoryItemView : MonoBehaviour, IPointerClickHandler
{
    public InventoryItem item;               // set by InventoryUI when building views
    public RectTransform container;          // the footprint container (parent)
    public RawImage raw;                     // the image that shows the RT
    public InventoryDragController dragCtrl; // injected by InventoryUI
    public Texture previewTexture;
    private void Awake()
    {
        if (!raw) raw = GetComponent<RawImage>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Left-click picks up / places
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            dragCtrl?.OnItemClicked(this);
        }
    }
}
