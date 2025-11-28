// Assets/Scripts/Enemies/Runtime/EnemyCombatController.cs
using UnityEngine;
using Game.Enemies;

/// <summary>
/// Handles basic enemy combat:
/// - Keeps attack range & cooldown
/// - On TryAttack, checks distance and cooldown
/// - Deals damage to the player and spawns a damage popup
///
/// For now this is very simple (no animation timing). In Step 6
/// we can add windup, hit frames, miss chance, etc.
/// </summary>
[DisallowMultipleComponent]
public class EnemyCombatController : MonoBehaviour
{
    [Header("Wiring")]
    public EnemyStats stats;
    public EnemyHealth health;
    public PlayerStats playerStats;         // target player to damage
    public DamagePopup damagePopupPrefab;   // same prefab we use everywhere

    [Header("Attack Settings")]
    [Tooltip("Max distance to the player to attempt an attack.")]
    public float attackRange = 2.0f;

    [Tooltip("Seconds between attacks.")]
    public float attackInterval = 1.5f;

    [Tooltip("If EnemyStats is present, damage comes from stats.Damage. " +
             "These are only used as fallback/override.")]
    public int minDamageFallback = 1;
    public int maxDamageFallback = 4;

    private float _cooldown;

    public float AttackRange => attackRange;

    private void Reset()
    {
        if (!stats) stats = GetComponent<EnemyStats>();
        if (!health) health = GetComponent<EnemyHealth>();
    }

    private void Awake()
    {
        if (!stats) stats = GetComponent<EnemyStats>();
        if (!health) health = GetComponent<EnemyHealth>();

        // If not set explicitly, default from EnemyStats (baseAttackSpeed)
        if (stats != null && attackInterval <= 0f)
        {
            attackInterval = stats.AttackSpeed;
        }

        if (minDamageFallback > maxDamageFallback)
            maxDamageFallback = minDamageFallback;
    }

    private void Update()
    {
        if (_cooldown > 0f)
            _cooldown -= Time.deltaTime;
    }

    /// <summary>
    /// Called by EnemyAIController when in Attacking state.
    /// Returns true if an attack was actually performed.
    /// </summary>
    public bool TryAttack(Transform targetTransform)
    {
        if (!enabled) return false;
        if (health == null || health.IsDead) return false;
        if (_cooldown > 0f) return false;
        if (!targetTransform) return false;

        // Distance check (planar)
        Vector3 diff = targetTransform.position - transform.position;
        diff.y = 0f;
        float dist = diff.magnitude;
        if (dist > attackRange)
            return false;

        _cooldown = attackInterval;

        // --- Compute damage ---
        int damage;
        if (stats != null && stats.Damage > 0f)
        {
            damage = Mathf.RoundToInt(stats.Damage);
        }
        else
        {
            damage = Random.Range(minDamageFallback, maxDamageFallback + 1);
        }

        // --- Apply to player ---
        PlayerStats ps = playerStats;
        if (!ps)
            ps = targetTransform.GetComponentInParent<PlayerStats>();

        if (ps)
        {
            ps.TakeDamage(damage);

            // Damage popup above player (red numbers)
            if (damagePopupPrefab)
            {
                Vector3 hitPos = targetTransform.position + Vector3.up * 1.7f;
                DamagePopup popup = Instantiate(damagePopupPrefab, hitPos, Quaternion.identity);
                popup.SetPlayerDamage(damage);
            }
        }

        // Step 6: we’ll hook animation triggers, miss chance, etc., here.
        return true;
    }
}
