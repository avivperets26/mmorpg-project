using UnityEngine;
using Game.Enemies;

/// <summary>
/// Handles basic enemy combat:
/// - Keeps attack range & cooldown
/// - On TryAttack, checks distance + facing + hit chance
/// - Deals damage to the player and spawns damage / miss popups
///
/// NOTE: Still "instant hit" for now. Later we can sync this with an
/// animation event or windup state.
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
    [Tooltip("Max distance to the player at which we even consider attacking.")]
    public float attackRange = 2.0f;

    [Tooltip("Seconds between attacks.")]
    public float attackInterval = 1.5f;

    [Tooltip("If EnemyStats is present, damage comes from stats.Damage. " +
             "These are only used as fallback/override.")]
    public int minDamageFallback = 1;
    public int maxDamageFallback = 4;

    [Header("Hit Detection")]
    [Tooltip("Radius of the hit sphere in front of the enemy.")]
    public float hitRadius = 0.75f;

    [Tooltip("How far in front of the enemy the hit sphere is placed.")]
    public float hitForwardOffset = 1.0f;

    [Tooltip("Minimum dot product to count the target as 'in front'. 1 = straight ahead, 0 = 90 degrees.")]
    [Range(0f, 1f)]
    public float minFrontDot = 0.2f;

    [Header("Hit Chance")]
    [Tooltip("Base chance to hit the player (0-1). We can later mix in player stats / evasion.")]
    [Range(0f, 1f)]
    public float baseHitChance = 0.9f;

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
    /// Returns true if an attack was actually performed
    /// (hit OR miss) and cooldown was consumed.
    /// </summary>
    public bool TryAttack(Transform targetTransform)
    {
        if (!enabled) return false;
        if (health == null || health.IsDead) return false;
        if (_cooldown > 0f) return false;
        if (!targetTransform) return false;

        // Basic distance gate (don't even try if too far).
        Vector3 toTarget = targetTransform.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;
        if (dist > attackRange)
            return false;

        // Consume cooldown - attack attempt happens now.
        _cooldown = attackInterval;

        // --- Facing check (is player roughly in front?) ---
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        Vector3 dirToTarget = toTarget.normalized;
        float dot = Vector3.Dot(forward.normalized, dirToTarget);
        bool isInFront = dot >= minFrontDot;

        // --- Hit chance (very simple for now) ---
        bool didHitRoll = Random.value <= baseHitChance;

        // --- Decide miss / hit BEFORE applying damage ---
        bool isHit = isInFront && didHitRoll;

        // Popup position (above player)
        Vector3 popupPos = targetTransform.position + Vector3.up * 1.7f;

        // If MISS -> show miss popup and exit.
        if (!isHit)
        {
            if (damagePopupPrefab)
            {
                DamagePopup missPopup = Object.Instantiate(damagePopupPrefab, popupPos, Quaternion.identity);
                missPopup.SetMiss();
            }

            // Later we can add block/parry effects here.
            return true;
        }

        // --- HIT branch ---

        // Compute damage
        int damage;
        if (stats != null && stats.Damage > 0f)
        {
            damage = Mathf.RoundToInt(stats.Damage);
        }
        else
        {
            damage = Random.Range(minDamageFallback, maxDamageFallback + 1);
        }

        // Find PlayerStats if not wired explicitly
        PlayerStats ps = playerStats;
        if (!ps)
            ps = targetTransform.GetComponentInParent<PlayerStats>();

        if (ps)
        {
            ps.TakeDamage(damage);

            if (damagePopupPrefab)
            {
                DamagePopup dmgPopup = Object.Instantiate(damagePopupPrefab, popupPos, Quaternion.identity);
                dmgPopup.SetPlayerDamage(damage); // red numbers
            }
        }

        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize hit sphere in Scene view
        Vector3 origin = transform.position + transform.forward * hitForwardOffset + Vector3.up * 1f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, hitRadius);
    }
#endif
}
