using System;
using UnityEngine;
using Game.Items;

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Base run speed without any gear.")]
    public float baseMoveSpeed = 5f;
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

    // NEW: weapon-driven aggregates
    public float equipCritChance;          // percentage points added by gear (0..100)
    public float equipAttackSpeedRating;   // rating points (1.10 aps => +10 rating)

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

    [Header("Rules")]
    public bool refillOnLevelUp = true;

    [Header("Experience")]
    [SerializeField] private int currentXp = 0;
    [SerializeField] private int xpToNext = 100;

    // ===== Startup configuration (Inspector-controlled) =====
    public enum StartVitalsMode { Full, Percent, Absolute }

    [Header("Startup Vitals (Inspector)")]
    public StartVitalsMode startMode = StartVitalsMode.Percent;

    [Range(0f, 1f)] public float startHpPercent = 0.5f;
    [Range(0f, 1f)] public float startMpPercent = 0.5f;

    [Tooltip("Used only when mode = Absolute. -1 means use Max.")]
    public int startHpAbsolute = -1;
    [Tooltip("Used only when mode = Absolute. -1 means use Max.")]
    public int startMpAbsolute = -1;

    // --- Events the UI / systems can listen to ---
    public event Action OnVitalsChanged;
    public event Action OnXpChanged;
    public event Action OnLevelChanged;
    public event Action OnDerivedChanged;


    // --- Convenience ---
    public bool BootsEquipped => moveSpeedMultiplier > 1f;
    public float GetEffectiveMoveSpeed() => baseMoveSpeed * moveSpeedMultiplier;

    // Public vitals getters (caps are computed from level & attributes)
    public int CurrentHp => currentHp;
    public int CurrentMp => currentMp;
    public int MaxHp => 100 + 10 * level + 20 * vitality; // mirrors StatAllocationUI
    public int MaxMp => 50 + 5 * level + 15 * energy;    // mirrors StatAllocationUI
    public int CurrentXp => currentXp;
    public int XpToNext => xpToNext;
    public float XpNormalized => xpToNext <= 0 ? 0f : Mathf.Clamp01(currentXp / (float)xpToNext);



    // ---------- Unity ----------
    private void Start()
    {
        // XP to next level using your WoW-style step: XP_n = level * 50
        xpToNext = ExpToNextFor(level);

        // Compute starting HP/MP based on inspector mode
        int maxHp = MaxHp;
        int maxMp = MaxMp;

        switch (startMode)
        {
            case StartVitalsMode.Full:
                currentHp = maxHp;
                currentMp = maxMp;
                break;

            case StartVitalsMode.Percent:
                currentHp = Mathf.Clamp(Mathf.RoundToInt(maxHp * startHpPercent), 0, maxHp);
                currentMp = Mathf.Clamp(Mathf.RoundToInt(maxMp * startMpPercent), 0, maxMp);
                break;

            case StartVitalsMode.Absolute:
                currentHp = Mathf.Clamp(startHpAbsolute < 0 ? maxHp : startHpAbsolute, 0, maxHp);
                currentMp = Mathf.Clamp(startMpAbsolute < 0 ? maxMp : startMpAbsolute, 0, maxMp);
                break;
        }

        RaiseVitals();
        RaiseXP();
    }

    private void OnValidate()
    {
        // keep sliders sane
        startHpPercent = Mathf.Clamp01(startHpPercent);
        startMpPercent = Mathf.Clamp01(startMpPercent);

        // level must be at least 1
        if (level < 1) level = 1;

        // Recompute XP needed to go from 'level' -> 'level + 1'
        xpToNext = ExpToNextFor(level); // WoW-style step: level * 50

        // If we're in the editor (not playing), keep current vitals within new caps
        if (!Application.isPlaying)
        {
            currentHp = Mathf.Min(currentHp, MaxHp);
            currentMp = Mathf.Min(currentMp, MaxMp);
        }
    }

    // ---- Movement from boots ----
    public void EquipBoots(float speedMultiplier) => moveSpeedMultiplier = Mathf.Max(speedMultiplier, 0.01f);
    public void UnequipBoots() => moveSpeedMultiplier = 1f;

    // ---- Armor aggregations ----
    public void AddArmor(int defense, int magicResist)
    {
        equipDefense += Mathf.Max(0, defense);
        equipMagicResist += Mathf.Max(0, magicResist);
        RaiseDerived();
    }
    public void RemoveArmor(int defense, int magicResist)
    {
        equipDefense = Mathf.Max(0, equipDefense - Mathf.Max(0, defense));
        equipMagicResist = Mathf.Max(0, equipMagicResist - Mathf.Max(0, magicResist));
        RaiseDerived();
    }

    // ---- Weapon aggregations (simple MVP) ----
    public void AddWeapon(DamageProfile dmg)
    {
        equipDamageMin += Mathf.Max(0, dmg.min);
        equipDamageMax += Mathf.Max(0, dmg.max);
        equipWizardry += Mathf.Max(0, dmg.wizardry);

        // Crit: store as percentage points (0..100)
        equipCritChance += Mathf.Max(0f, dmg.critChance * 100f);

        // Attack speed rating: 1.00 APS => 0, 1.20 => +20, 0.85 => -15
        float ratingDelta = Mathf.Round((dmg.attackSpeed - 1f) * 100f);
        equipAttackSpeedRating += ratingDelta;   // allow negative for slow weapons
        RaiseDerived();
    }

    public void RemoveWeapon(DamageProfile dmg)
    {
        equipDamageMin = Mathf.Max(0, equipDamageMin - Mathf.Max(0, dmg.min));
        equipDamageMax = Mathf.Max(0, equipDamageMax - Mathf.Max(0, dmg.max));
        equipWizardry = Mathf.Max(0, equipWizardry - Mathf.Max(0, dmg.wizardry));

        equipCritChance = Mathf.Max(0f, equipCritChance - Mathf.Max(0f, dmg.critChance * 100f));

        float ratingDelta = Mathf.Round((dmg.attackSpeed - 1f) * 100f);
        equipAttackSpeedRating -= ratingDelta;   // undo (keeps sign)
        RaiseDerived();
    }

    // ---- On-kill bonuses ----
    public void AddOnKill(float hp, float mana)
    {
        equipHpOnKill += Mathf.Max(0f, hp);
        equipManaOnKill += Mathf.Max(0f, mana);
        RaiseDerived();
    }
    public void RemoveOnKill(float hp, float mana)
    {
        equipHpOnKill = Mathf.Max(0f, equipHpOnKill - Mathf.Max(0f, hp));
        equipManaOnKill = Mathf.Max(0f, equipManaOnKill - Mathf.Max(0f, mana));
        RaiseDerived();
    }

    // === Helpers used by the Stat UI ===
    public bool TrySpendPoint()
    {
        if (availableStatPoints <= 0) return false;
        availableStatPoints--;
        return true;
    }

    public void RefundPoint() => availableStatPoints++;

    public void ApplyDelta(int dStr, int dDex, int dVit, int dEng)
    {
        strength += Mathf.Max(0, dStr);
        dexterity += Mathf.Max(0, dDex);
        vitality += Mathf.Max(0, dVit);
        energy += Mathf.Max(0, dEng);

        // Recompute caps first
        int newMaxHp = MaxHp;
        int newMaxMp = MaxMp;

        // Option A: always refill on stat spend
        currentHp = newMaxHp;
        currentMp = newMaxMp;
        RaiseVitals();

        // If you still want the generic clamp helper for other uses:
        // RecalculateCapsAndClamp();  // then you can remove RaiseVitals() above
    }

    // ================== Public API ==================
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
        xpToNext = ExpToNextFor(level);

        if (refillOnLevelUp)
        {
            currentHp = MaxHp;
            currentMp = MaxMp;
            RaiseVitals();
        }

        RaiseXP();
        OnLevelChanged?.Invoke();
    }

    // XP needed to go from 'currentLevel' -> 'currentLevel + 1'
    // Based on your WoW approximation: XP_step = currentLevel * 50
    private int ExpToNextFor(int currentLevel)
    {
        // WoW-style: incremental per level = currentLevel * 50
        return Mathf.Max(1, currentLevel) * 50;
    }

    private int TotalXpToReachLevel(int targetLevel)
    {
        // Σ(i * 50) = 25 * (L^2 - L)
        int L = Mathf.Max(1, targetLevel);
        return 25 * (L * L - L);
    }

    public int GetMobXp(int mobLevel)
    {
        int sameLevelXp = (mobLevel * 5) + 45;
        int diff = mobLevel - level;

        float mult =
            diff == 0 ? 1f :
            diff > 0 ? (1f + 0.05f * diff) :
                       (1f + (2f / 11f) * diff); // diff is negative

        // Prevent negatives
        int xp = Mathf.RoundToInt(sameLevelXp * Mathf.Max(0f, mult));
        return xp;
    }

    public void GainXpFromMob(int mobLevel) => GainXp(GetMobXp(mobLevel));

    private void RaiseVitals() => OnVitalsChanged?.Invoke();
    private void RaiseXP() => OnXpChanged?.Invoke();

    private void RaiseDerived() => OnDerivedChanged?.Invoke();

}
