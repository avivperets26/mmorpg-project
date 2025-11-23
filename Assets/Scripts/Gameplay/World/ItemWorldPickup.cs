// Assets/Scripts/Items/ItemWorldPickup.cs
using UnityEngine;
using TMPro;
using Game.Items;

[RequireComponent(typeof(Collider))]
public class ItemWorldPickup : MonoBehaviour, IInteractable
{
    [Header("Data")]
    public ItemDefinition def;

    [Header("Pickup")]
    [Min(0.1f)] public float pickupRadius = 2.0f;

    [Header("UI (optional)")]
    // ✅ Use TMP_Text so you can assign TextMeshProUGUI
    public TMP_Text label;

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

        // Try on this object, then parents, then as a fallback anywhere in the scene
        var inv =
            interactor.GetComponent<PlayerInventory>() ??
            interactor.GetComponentInParent<PlayerInventory>();

        if (!inv)
        {
#if UNITY_2023_1_OR_NEWER
            inv = FindFirstObjectByType<PlayerInventory>();
#else
            inv = FindObjectOfType<PlayerInventory>();
#endif
        }

        if (!inv)
        {
            Debug.LogWarning("❌ No PlayerInventory found for interactor! " +
                             "Make sure PlayerInventory exists in the scene.");
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

    /// <summary>
    /// Called by WorldItem when a pickup is spawned from the inventory.
    /// Updates the definition + label text/color so tier colors are preserved.
    /// </summary>
    public void SetRuntimeDefinition(ItemDefinition definition, int stackCount)
    {
        if (definition == null) return;

        def = definition;

        if (label != null)
        {
            label.text = def.displayName;
            // Use your tier color helper (same as in Awake)
            label.color = RarityRules.GetLabelColor(def.defaultTier);
            // or, if you prefer legacy:
            // label.color = ItemDefinition.RarityColor(def.legacyRarity);
        }

        // If you ever track stack count on the pickup itself, update it here too.
        // (Right now you don't, so we just ignore stackCount.)
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
