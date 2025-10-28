using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Base run speed without any gear.")]
    public float baseMoveSpeed = 5f;

    // Multiplicative bonuses (e.g., 1.2 = +20%)
    private float moveSpeedMultiplier = 1f;

    // Optional: expose whether boots are on
    public bool BootsEquipped => moveSpeedMultiplier > 1f;

    public float GetEffectiveMoveSpeed() => baseMoveSpeed * moveSpeedMultiplier;

    public void EquipBoots(float speedMultiplier)
    {
        // e.g., 1.2f = +20% speed
        moveSpeedMultiplier = Mathf.Max(speedMultiplier, 0.01f);
    }

    public void UnequipBoots()
    {
        moveSpeedMultiplier = 1f;
    }
}
