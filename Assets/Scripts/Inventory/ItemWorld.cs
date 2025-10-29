using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class ItemWorld : MonoBehaviour
{
    [Header("Data")]
    public ItemDefinition def;

    [Header("Pickup")]
    public float pickupRadius = 2.0f;     // E-key range
    public KeyCode pickupKey = KeyCode.E;

    [Header("UI")]
    public TextMeshPro label;             // assign the Label child in prefab

    void Awake()
    {
        if (def && label)
        {
            label.text = def.displayName;
            label.color = ItemDefinition.RarityColor(def.rarity);
        }
    }

    void Update()
    {
        // Optional proximity pickup via E
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player && Input.GetKeyDown(pickupKey))
        {
            if (Vector3.Distance(player.transform.position, transform.position) <= pickupRadius)
                TryPickupToPlayer(player);
        }
    }

    // Mouse click → immediate pickup if player exists (no distance check, add if you want)
    void OnMouseDown()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player) TryPickupToPlayer(player);
    }

    void TryPickupToPlayer(GameObject player)
    {
        var inv = player.GetComponent<PlayerInventory>();
        if (!inv || !def) return;

        if (inv.TryAdd(def))
        {
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Inventory full or no space for item.");
        }
    }

    // For visualizing E-pickup range
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
