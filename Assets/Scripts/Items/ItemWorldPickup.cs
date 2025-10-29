using UnityEngine;
using TMPro;

/// <summary>
/// Generic world pickup for any ItemDefinition.
/// Provides label setup, and implements IInteractable for inventory pickup.
/// </summary>
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
        if (col) col.isTrigger = false; // we raycast; keep solid collider for click-hit. Trigger also works if you prefer.
    }

    void Awake()
    {
        if (def && label)
        {
            label.text = def.displayName;
            label.color = ItemDefinition.RarityColor(def.rarity);
        }
    }

    public bool Interact(GameObject interactor)
    {
        if (!def) return false;

        var inv = interactor.GetComponent<PlayerInventory>();
        if (!inv) return false;

        if (inv.TryAdd(def))
        {
            Destroy(gameObject);
            return true;
        }

        // Could show "Inventory full" UI here.
        return false;
    }
}
