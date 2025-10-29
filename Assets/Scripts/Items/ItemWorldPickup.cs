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
        Debug.Log($"🪄 Interact called on {name}");

        if (!def)
        {
            Debug.LogWarning("❌ Missing ItemDefinition!");
            return;
        }

        var inv = interactor.GetComponent<PlayerInventory>();
        if (!inv)
        {
            Debug.LogWarning("❌ No PlayerInventory found on interactor!");
            return;
        }

        bool added = inv.TryAdd(def);
        Debug.Log($"📦 TryAdd result: {added}");

        if (added)
        {
            Debug.Log($"✅ Picked up {def.displayName}");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("⚠️ Inventory full or add failed");
        }
    }
}
