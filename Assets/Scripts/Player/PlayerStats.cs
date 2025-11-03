// Assets/Scripts/Player/PlayerStats.cs
using UnityEngine;
using Game.Items;

public class PlayerStats : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Base run speed without any gear.")]
    public float baseMoveSpeed = 5f;

    // Multiplicative bonus from boots (or other effects)
    private float moveSpeedMultiplier = 1f;

    // --- Combat aggregates from equipped items (additive for MVP) ---
    [Header("Derived (equipment)")]
    public int equipDefense;
    public int equipMagicResist;
    public int equipDamageMin;
    public int equipDamageMax;
    public float equipWizardry;      // for magic builds (if you use it)
    public float equipHpOnKill;
    public float equipManaOnKill;

    // --- Core attributes & points you can spend ---
    [Header("Core Attributes")]
    public int strength = 5;
    public int dexterity = 5;
    public int vitality = 5;   // NEW
    public int energy = 5;

    [Tooltip("Unspent points you can distribute.")]
    public int availableStatPoints = 10; // start with 10 so it’s easy to test

    [Header("Progression")]
    public int level = 1;

    // --- Convenience ---
    public bool BootsEquipped => moveSpeedMultiplier > 1f;
    public float GetEffectiveMoveSpeed() => baseMoveSpeed * moveSpeedMultiplier;

    // ---- Movement from boots ----
    public void EquipBoots(float speedMultiplier)
    {
        moveSpeedMultiplier = Mathf.Max(speedMultiplier, 0.01f);
    }
    public void UnequipBoots()
    {
        moveSpeedMultiplier = 1f;
    }

    // ---- Armor aggregations ----
    public void AddArmor(int defense, int magicResist)
    {
        equipDefense += Mathf.Max(0, defense);
        equipMagicResist += Mathf.Max(0, magicResist);
    }
    public void RemoveArmor(int defense, int magicResist)
    {
        equipDefense = Mathf.Max(0, equipDefense - Mathf.Max(0, defense));
        equipMagicResist = Mathf.Max(0, equipMagicResist - Mathf.Max(0, magicResist));
    }

    // ---- Weapon aggregations (simple MVP) ----
    public void AddWeapon(DamageProfile dmg)
    {
        equipDamageMin += Mathf.Max(0, dmg.min);
        equipDamageMax += Mathf.Max(0, dmg.max);
        equipWizardry += Mathf.Max(0, dmg.wizardry);
        // crit/speed can be handled elsewhere if needed
    }
    public void RemoveWeapon(DamageProfile dmg)
    {
        equipDamageMin = Mathf.Max(0, equipDamageMin - Mathf.Max(0, dmg.min));
        equipDamageMax = Mathf.Max(0, equipDamageMax - Mathf.Max(0, dmg.max));
        equipWizardry = Mathf.Max(0, equipWizardry - Mathf.Max(0, dmg.wizardry));
    }

    // ---- On-kill bonuses ----
    public void AddOnKill(float hp, float mana)
    {
        equipHpOnKill += Mathf.Max(0f, hp);
        equipManaOnKill += Mathf.Max(0f, mana);
    }
    public void RemoveOnKill(float hp, float mana)
    {
        equipHpOnKill = Mathf.Max(0f, equipHpOnKill - Mathf.Max(0f, hp));
        equipManaOnKill = Mathf.Max(0f, equipManaOnKill - Mathf.Max(0f, mana));
    }

    // === Helpers used by the Stat UI ===
    public bool TrySpendPoint()
    {
        if (availableStatPoints <= 0) return false;
        availableStatPoints--;
        return true;
    }

    public void RefundPoint()
    {
        availableStatPoints++;
    }

    /// <summary>
    /// Apply pending deltas from the Character Stats dialog.
    /// Clamp to non-negative increments to keep it simple/safe.
    /// </summary>
    public void ApplyDelta(int dStr, int dDex, int dVit, int dEng)
    {
        strength += Mathf.Max(0, dStr);
        dexterity += Mathf.Max(0, dDex);
        vitality += Mathf.Max(0, dVit);
        energy += Mathf.Max(0, dEng);
    }
}
