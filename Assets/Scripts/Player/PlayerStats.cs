// Assets/Scripts/Player/PlayerStats.cs
using System;
using UnityEngine;
using Game.Items;

[DisallowMultipleComponent]
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
    public float equipWizardry;
    public float equipHpOnKill;
    public float equipManaOnKill;

    // --- Core attributes & points you can spend ---
    [Header("Core Attributes")]
    public int strength = 5;
    public int dexterity = 5;
    public int vitality = 5;
    public int energy = 5;

    [Tooltip("Unspent points you can distribute.")]
    public int availableStatPoints = 10;

    [Header("Progression")]
    public int level = 1;

    // --- Vitals & Experience (runtime) ---
    [Header("Vitals (runtime)")]
    [SerializeField] private int currentHp;
    [SerializeField] private int currentMp;

    [Header("Experience")]
    [SerializeField] private int currentXp = 0;
    [SerializeField] private int xpToNext = 100;

    // --- Events the UI / systems can listen to ---
    public event Action OnVitalsChanged;
    public event Action OnXpChanged;
    public event Action OnLevelChanged;

    // --- Convenience ---
    public bool BootsEquipped => moveSpeedMultiplier > 1f;
    public float GetEffectiveMoveSpeed() => baseMoveSpeed * moveSpeedMultiplier;

    // Public vitals getters (caps are computed from level & attributes)
    public int CurrentHp => currentHp;
    public int CurrentMp => currentMp;
    public int MaxHp => 100 + 10 * level + 20 * vitality; // mirrors StatAllocationUI
    public int MaxMp => 50 + 5 * level + 15 * energy;   // mirrors StatAllocationUI

    public int CurrentXp => currentXp;
    public int XpToNext => xpToNext;
    public float XpNormalized => xpToNext <= 0 ? 0f : Mathf.Clamp01(currentXp / (float)xpToNext);

    // ---------- Unity ----------
    private void Start()
    {
        // Initialize vitals to full on start
        currentHp = MaxHp;
        currentMp = MaxMp;

        // Initialize XP requirement for next level
        xpToNext = ExpRequiredFor(level + 1);

        RaiseVitals();
        RaiseXP();
    }

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

    /// <summary>Legacy support if you ever need to apply deltas again.</summary>
    public void ApplyDelta(int dStr, int dDex, int dVit, int dEng)
    {
        strength += Mathf.Max(0, dStr);
        dexterity += Mathf.Max(0, dDex);
        vitality += Mathf.Max(0, dVit);
        energy += Mathf.Max(0, dEng);

        // If attributes changed, caps may have changed too.
        RecalculateCapsAndClamp();
    }

    // ================== New public API for HUD / gameplay ==================

    public void GainXp(int amount)
    {
        if (amount <= 0) return;

        currentXp += amount;
        while (currentXp >= xpToNext)
        {
            currentXp -= xpToNext;
            LevelUp();
        }
        RaiseXP();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        currentHp = Mathf.Max(0, currentHp - amount);
        RaiseVitals();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHp = Mathf.Min(MaxHp, currentHp + amount);
        RaiseVitals();
    }

    public void SpendMana(int amount)
    {
        if (amount <= 0) return;
        currentMp = Mathf.Max(0, currentMp - amount);
        RaiseVitals();
    }

    public void RestoreMana(int amount)
    {
        if (amount <= 0) return;
        currentMp = Mathf.Min(MaxMp, currentMp + amount);
        RaiseVitals();
    }

    /// <summary>
    /// Call this after changing level or attributes to ensure current values
    /// don't exceed new caps.
    /// </summary>
    public void RecalculateCapsAndClamp()
    {
        currentHp = Mathf.Min(currentHp, MaxHp);
        currentMp = Mathf.Min(currentMp, MaxMp);
        RaiseVitals();
    }

    // ====================== Internals ======================

    private void LevelUp()
    {
        level++;
        xpToNext = ExpRequiredFor(level + 1);

        // On level-up, refill to the new caps feels nice (change to taste)
        currentHp = MaxHp;
        currentMp = MaxMp;

        RaiseVitals();
        RaiseXP();
        OnLevelChanged?.Invoke();
    }

    // Simple curve: triangular numbers * 100
    // L2=100, L3=300, L4=600, L5=1000, ...
    private int ExpRequiredFor(int targetLevel)
    {
        int n = Mathf.Max(2, targetLevel);
        return (n - 1) * n / 2 * 100;
    }

    private void RaiseVitals() => OnVitalsChanged?.Invoke();
    private void RaiseXP() => OnXpChanged?.Invoke();
}
