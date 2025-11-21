// Assets/Scripts/Gameplay/Combat/IDamageable.cs
using UnityEngine;

/// <summary>
/// Minimal contract for anything that can be hit by the player.
/// </summary>
public interface IDamageable
{
    /// <param name="amount">Damage dealt (already calculated).</param>
    /// <param name="hitPoint">World hit point (for FX / popups).</param>
    /// <param name="hitNormal">Surface normal at hit point.</param>
    void TakeHit(int amount, Vector3 hitPoint, Vector3 hitNormal);
}
