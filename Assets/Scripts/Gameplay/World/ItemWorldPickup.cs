// Assets/Scripts/Items/ItemWorldPickup.cs
using UnityEngine;
using TMPro;
using Game.Items; // <-- add this

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
        gameObject.layer = LayerMask.NameToLayer("Default"); // or your "Interactable" layer
    }

    void Awake()
    {
        if (def && label)
        {
            label.text = def.displayName;
            // WAS: ItemDefinition.RarityColor(def.rarity)
            label.color = RarityRules.GetLabelColor(def.defaultTier);
            // If you prefer legacy colors, use:
            // label.color = ItemDefinition.RarityColor(def.legacyRarity);
        }
    }

    public void Interact(GameObject interactor)
    {
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
        if (added)
        {
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("⚠️ Inventory full or add failed");
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.35f);
        Gizmos.DrawSphere(transform.position, pickupRadius);
        Gizmos.color = new Color(0.2f, 1f, 0.6f, 1f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
#endif
}
