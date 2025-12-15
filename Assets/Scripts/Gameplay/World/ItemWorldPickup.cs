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
    // Use TMP_Text so you can assign TextMeshProUGUI
    public TMP_Text label;

    public Transform Transform => transform;
    public float MaxUseDistance => pickupRadius;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col)
            col.isTrigger = true;

        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (col)
            col.isTrigger = true;

        if (def && label)
        {
            label.text = def.displayName;
            label.color = RarityRules.GetLabelColor(def.defaultTier);
        }
    }

    public void Interact(GameObject interactor)
    {
        if (!def)
        {
            Debug.LogWarning("[Pickup] Missing ItemDefinition!");
            return;
        }

        // Try on this object, then parents, then as a fallback anywhere in the scene
        var inv =
            interactor.GetComponent<PlayerInventory>() ??
            interactor.GetComponentInParent<PlayerInventory>();

        var equip =
            interactor.GetComponent<EquipmentController>() ??
            interactor.GetComponentInParent<EquipmentController>();

        if (!inv)
        {
#if UNITY_2023_1_OR_NEWER
            inv = FindFirstObjectByType<PlayerInventory>();
#else
            inv = FindObjectOfType<PlayerInventory>();
#endif
        }

        if (!equip)
        {
#if UNITY_2023_1_OR_NEWER
            equip = FindFirstObjectByType<EquipmentController>();
#else
            equip = FindObjectOfType<EquipmentController>();
#endif
        }

        if (!inv && !equip)
        {
            Debug.LogWarning("[Pickup] No PlayerInventory/EquipmentController found for interactor!");
            return;
        }

        // Auto-equip into an empty slot if requirements are met; else fall back to inventory.
        bool equipped = equip != null && equip.TryAutoEquipFromPickup(def);
        bool added = equipped || (inv != null && inv.TryAdd(def));
        if (added)
        {
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("[Pickup] Inventory full or add failed");
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
            label.color = RarityRules.GetLabelColor(def.defaultTier);
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
