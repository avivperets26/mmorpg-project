using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class ItemWorldPickup : MonoBehaviour, IInteractable
{
    [Header("Data")]
    public ItemDefinition def;

    [Header("Pickup")]
    [Min(0.1f)] public float pickupRadius = 2.0f;

    [Header("UI (optional)")]
    public TextMeshPro label;   // assign if you want the floating label

    public Transform Transform => transform;
    public float MaxUseDistance => pickupRadius;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = false; // solid collider for click-raycast
    }

    void Awake()
    {
        if (def && label)
        {
            label.text = def.displayName;
            label.color = ItemDefinition.RarityColor(def.rarity);
        }
    }

    // Must match IInteractable: void, not bool
    public void Interact(GameObject interactor)
    {
        if (!def) return;

        var inv = interactor.GetComponent<PlayerInventory>();
        if (inv == null) return;

        // Your inventory API — adjust as needed
        bool added = inv.TryAdd(def);
        if (added)
        {
            // TODO: VFX/SFX if you want
            Destroy(gameObject);
        }
        else
        {
            // Optional: show "Inventory full" feedback
        }
    }
}
