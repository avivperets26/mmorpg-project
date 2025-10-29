using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemWorld : MonoBehaviour
{
    [Header("Data")]
    public ItemDefinition def;

    [Header("Pickup")]
    public float pickupRadius = 2.0f;
    public KeyCode pickupKey = KeyCode.E;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }

    void Update()
    {
        // Simple nearby + key press pickup (replace with your interact system later)
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player || def == null) return;

        float dist = Vector3.Distance(player.transform.position, transform.position);
        if (dist <= pickupRadius && Input.GetKeyDown(pickupKey))
        {
            var inv = player.GetComponent<PlayerInventory>();
            if (inv && inv.TryAdd(def))
            {
                Destroy(gameObject); // picked up!
            }
            else
            {
                Debug.Log("Inventory full or placement failed.");
            }
        }
    }
}
