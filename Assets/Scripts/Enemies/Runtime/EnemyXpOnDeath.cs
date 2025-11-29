using UnityEngine;
using Game.Enemies; // for EnemyStats, EnemyHealth, EnemyTier, SpecialDamageType

/// <summary>
/// Listens to EnemyHealth.OnDeath, awards XP to the player and spawns an
/// XP popup using the DamagePopup prefab.
///
/// Drop this on the enemy root (same place as EnemyStats / EnemyHealth).
/// </summary>
[DisallowMultipleComponent]
public class EnemyXpOnDeath : MonoBehaviour
{
    [Header("Wiring")]
    public EnemyStats stats;
    public EnemyHealth health;

    [Tooltip("Player that should receive XP when this enemy dies.")]
    public PlayerStats playerStats;

    [Tooltip("World-space popup prefab used to show '+XP'. Usually DamagePopup.prefab.")]
    public DamagePopup xpPopupPrefab;

    [Header("Popup")]
    [Tooltip("Vertical offset above the enemy position for the XP popup.")]
    public float popupHeight = 2.0f;

    [Header("Tuning")]
    [Tooltip("Extra multiplier when tier = Advanced.")]
    public float advancedTierMultiplier = 1.25f;

    [Tooltip("Extra multiplier when tier = Elite.")]
    public float eliteTierMultiplier = 1.6f;

    [Tooltip("Extra multiplier when tier = Boss.")]
    public float bossTierMultiplier = 3.0f;

    [Tooltip("Additional bonus per special damage flag (Fire, Poison, etc.).")]
    public float specialFlagBonus = 0.10f;

    [SerializeField] private bool debugLog = false;

    private void Reset()
    {
        if (!stats) stats = GetComponentInParent<EnemyStats>();
        if (!health) health = GetComponentInParent<EnemyHealth>();
    }

    private void Awake()
    {
        if (!playerStats)
        {
#if UNITY_2023_1_OR_NEWER
            playerStats = FindFirstObjectByType<PlayerStats>();
#else
            playerStats = FindObjectOfType<PlayerStats>();
#endif
        }
    }

    private void OnEnable()
    {
        if (!stats) stats = GetComponentInParent<EnemyStats>();
        if (!health) health = GetComponentInParent<EnemyHealth>();

        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    private void HandleDeath(EnemyHealth _)
    {
        int xp = CalculateXpReward();
        if (xp <= 0)
            return;

        // Give XP to player
        if (playerStats != null)
        {
            playerStats.GainXp(xp);
            if (debugLog)
            {
                Debug.Log($"{name} died. Awarding {xp} XP to {playerStats.name}.", this);
            }
        }

        // Spawn XP popup
        if (xpPopupPrefab != null)
        {
            Vector3 pos = transform.position + Vector3.up * popupHeight;
            DamagePopup popup = Instantiate(xpPopupPrefab, pos, Quaternion.identity);
            popup.SetXp(xp);
        }
    }

    private int CalculateXpReward()
    {
        if (stats == null || stats.definition == null)
            return 0;

        EnemyDefinition def = stats.definition;

        // 1) Base XP from definition
        float xp = def.baseXpReward;

        // 2) Tier multipliers (definition field + our extra per-tier tuning)
        float tierMul = def.xpMultiplierByTier <= 0f ? 1f : def.xpMultiplierByTier;

        switch (stats.Tier)
        {
            case EnemyTier.Advanced:
                tierMul *= advancedTierMultiplier;
                break;
            case EnemyTier.Elite:
                tierMul *= eliteTierMultiplier;
                break;
            case EnemyTier.Boss:
                tierMul *= bossTierMultiplier;
                break;
            case EnemyTier.Normal:
            default:
                break;
        }

        xp *= tierMul;

        // 3) Level scaling relative to minLevel
        int levelOffset = stats.level - def.minLevel;
        if (levelOffset > 0)
        {
            // +10% per level above min for now (easy to tweak later)
            xp *= (1f + 0.10f * levelOffset);
        }

        // 4) Bonus for special damage flags (Fire / Poison / etc.)
        if (stats.SpecialDamage != SpecialDamageType.None)
        {
            int flags = CountBits((int)stats.SpecialDamage);
            xp *= (1f + specialFlagBonus * Mathf.Max(1, flags));
        }

        return Mathf.Max(1, Mathf.RoundToInt(xp));
    }

    private int CountBits(int value)
    {
        int count = 0;
        while (value != 0)
        {
            value &= (value - 1);
            count++;
        }
        return count;
    }
}
