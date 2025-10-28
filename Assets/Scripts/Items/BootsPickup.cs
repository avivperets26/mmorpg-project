using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BootsPickup : MonoBehaviour
{
    public EquipmentItem boots;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var stats = other.GetComponent<PlayerStats>();
        if (!stats) return;

        if (boots != null)
        {
            stats.EquipBoots(boots.moveSpeedMultiplier);
            Debug.Log($"Equipped {boots.displayName} (+{(boots.moveSpeedMultiplier - 1f) * 100f:0}% speed)");
        }
        else
        {
            stats.EquipBoots(1.2f); // fallback
        }

        Destroy(gameObject); // consume pickup
    }
}
