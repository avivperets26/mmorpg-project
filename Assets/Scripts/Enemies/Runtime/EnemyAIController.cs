// Assets/Scripts/Enemies/Runtime/EnemyAIController.cs
using UnityEngine;
using System.Collections.Generic;

namespace Game.Enemies
{
    /// <summary>
    /// Very simple state machine for Aggressive enemies:
    ///
    /// Idle:
    ///   - Waits until the player is within aggro radius
    ///
    /// Chasing:
    ///   - Move towards the player using EnemyStats.MoveSpeed
    ///   - If within attack range -> Attacking
    ///   - If player too far -> Idle (drops aggro for now)
    ///
    /// Attacking:
    ///   - Face the player
    ///   - Call EnemyCombatController.TryAttack() every frame
    ///   - If out of range -> Chasing
    ///
    /// Dead:
    ///   - No updates; set when EnemyHealth.OnDeath fires
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyAIController : MonoBehaviour
    {
        public enum State
        {
            Idle,
            Chasing,
            Attacking,
            Dead
        }

        [Header("Wiring")]
        public EnemyStats stats;
        public EnemyHealth health;
        public EnemyCombatController combat;

        [Tooltip("Animator controlling the enemy visuals.")]
        public Animator animator;

        [Tooltip("Optional: used for facing. If null, this.transform is used.")]
        public Transform modelRoot;

        [Tooltip("Player to focus. If null, will attempt to FindObjectOfType<PlayerStats>() at runtime.")]
        public PlayerStats targetPlayer;


        [Header("Behavior (Aggressive)")]
        [Tooltip("Radius around this enemy where it will aggro onto the player.")]
        public float aggroRadius = 8f;

        [Tooltip("If the player is farther than this while chasing, drop aggro.")]
        public float loseAggroRadius = 12f;

        [Tooltip("How close we allow getting to the player while chasing.")]
        public float stopDistanceBuffer = 0.1f;

        [Header("Idle Wander")]
        [Tooltip("Radius around the spawn point to wander while idle (0 = no wandering).")]
        public float wanderRadius = 3f;

        [Tooltip("Random seconds between idle wander picks (x=min, y=max).")]
        public Vector2 wanderInterval = new Vector2(2f, 5f);

        [Tooltip("Fraction of move speed to use while wandering.")]
        [Range(0.1f, 1f)]
        public float wanderMoveSpeedMultiplier = 0.6f;

        [Header("Crowd Awareness")]
        [Tooltip("Radius within which other living enemies will push this one sideways to keep spacing.")]
        public float separationRadius = 1.25f;

        [Tooltip("Strength of the sideways push when near other enemies.")]
        public float separationWeight = 1.1f;

        [Tooltip("Clamp separation influence so jitter is minimized.")]
        public float maxSeparationMagnitude = 2.0f;

        [Header("Debug")]
        [SerializeField] private State _state = State.Idle;

        private Transform _targetTransform;
        private float _moveSpeed = 2.0f;
        private static readonly List<EnemyAIController> ActiveEnemies = new List<EnemyAIController>();
        private Vector3 _spawnPosition;
        private Vector3 _wanderTarget;
        private bool _hasWanderTarget;
        private float _wanderCooldown;

        private void Reset()
        {
            if (!stats) stats = GetComponent<EnemyStats>();
            if (!health) health = GetComponent<EnemyHealth>();
            if (!combat) combat = GetComponent<EnemyCombatController>();
            if (!modelRoot) modelRoot = transform;
            if (!animator) animator = GetComponentInChildren<Animator>();

        }

        private void Awake()
        {
            if (!stats) stats = GetComponent<EnemyStats>();
            if (!health) health = GetComponent<EnemyHealth>();
            if (!combat) combat = GetComponent<EnemyCombatController>();
            if (!modelRoot) modelRoot = transform;
            if (!animator) animator = GetComponentInChildren<Animator>();

            if (!targetPlayer)
                targetPlayer = FindObjectOfType<PlayerStats>();

            if (targetPlayer)
                _targetTransform = targetPlayer.transform;

            _moveSpeed = stats != null && stats.MoveSpeed > 0f
                ? stats.MoveSpeed
                : 2.0f;

            _spawnPosition = transform.position;
            _wanderCooldown = Random.Range(wanderInterval.x, wanderInterval.y);
        }

        private void OnEnable()
        {
            if (health != null)
                health.OnDeath += OnDeath;

            if (!ActiveEnemies.Contains(this))
                ActiveEnemies.Add(this);

            ResetAnimatorToIdle();
        }

        private void OnDisable()
        {
            if (health != null)
                health.OnDeath -= OnDeath;

            ActiveEnemies.Remove(this);
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (health != null && health.IsDead)
            {
                _state = State.Dead;
            }

            switch (_state)
            {
                case State.Idle:
                    TickIdle();
                    break;
                case State.Chasing:
                    TickChasing();
                    break;
                case State.Attacking:
                    TickAttacking();
                    break;
                case State.Dead:
                    // Do nothing. Could add despawn logic here later.
                    break;
            }
        }
        private void TickIdle()
        {
            // Always force idle speed when idle
            if (animator) animator.SetFloat("MoveSpeed", 0f);

            if (!HasValidTarget())
            {
                _state = State.Idle;
                return;
            }

            float dist = GetTargetDistance();
            if (dist <= aggroRadius)
            {
                _state = State.Chasing;
            }

            UpdateIdleWander();
        }


        private void TickChasing()
        {
            if (!HasValidTarget())
            {
                _state = State.Idle;
                if (animator) animator.SetFloat("MoveSpeed", 0f);
                return;
            }

            float dist = GetTargetDistance();

            if (dist > loseAggroRadius)
            {
                _state = State.Idle;
                if (animator) animator.SetFloat("MoveSpeed", 0f);
                return;
            }

            float attackRange = combat != null ? combat.AttackRange : 2f;

            if (dist <= attackRange)
            {
                _state = State.Attacking;
                if (animator) animator.SetFloat("MoveSpeed", 0f);
                return;
            }

            // Move towards the player (simple planar move) with friendly spacing
            Vector3 dir = _targetTransform.position - transform.position;
            dir.y = 0f;
            float planarDist = dir.magnitude;
            Vector3 separation = CalculateSeparation();
            Vector3 desiredDir = dir.normalized + separation;
            desiredDir.y = 0f;

            if (planarDist > stopDistanceBuffer && desiredDir.sqrMagnitude > 0.0001f)
            {
                desiredDir.Normalize();
                float moveDistance = _moveSpeed * Time.deltaTime;
                Vector3 planarCurrent = new Vector3(transform.position.x, 0f, transform.position.z);
                Vector3 targetPlanar = new Vector3(_targetTransform.position.x, 0f, _targetTransform.position.z);
                Vector3 newPlanar = planarCurrent + desiredDir * moveDistance;

                // Keep a tiny gap from the player so we don't stack on top.
                float newDist = Vector3.Distance(newPlanar, targetPlanar);
                if (newDist < stopDistanceBuffer && (planarDist > 0f))
                {
                    Vector3 away = (newPlanar - targetPlanar);
                    if (away.sqrMagnitude < 0.0001f)
                        away = (planarCurrent - targetPlanar);
                    newPlanar = targetPlanar + away.normalized * stopDistanceBuffer;
                }

                transform.position = new Vector3(newPlanar.x, transform.position.y, newPlanar.z);

                // <- THIS is what should push us into Walking
                if (animator) animator.SetFloat("MoveSpeed", _moveSpeed);
            }
            else
            {
                // Close enough, no movement
                if (animator) animator.SetFloat("MoveSpeed", 0f);
            }

            FaceTarget();
        }

        private void TickAttacking()
        {
            if (!HasValidTarget())
            {
                _state = State.Idle;
                if (animator) animator.SetFloat("MoveSpeed", 0f);
                return;
            }

            float attackRange = combat != null ? combat.AttackRange : 2f;
            float dist = GetTargetDistance();

            if (dist > attackRange * 1.1f)
            {
                _state = State.Chasing;
                return;
            }

            // Standing still while attacking
            if (animator) animator.SetFloat("MoveSpeed", 0f);

            FaceTarget();

            if (combat != null)
            {
                combat.TryAttack(_targetTransform);
            }
        }

        private void OnDeath(EnemyHealth _)
        {
            _state = State.Dead;
            if (animator)
            {
                animator.SetBool("IsDead", true);
                animator.SetFloat("MoveSpeed", 0f);
            }
        }


        private bool HasValidTarget()
        {
            if (!targetPlayer) return false;
            if (!_targetTransform) _targetTransform = targetPlayer.transform;
            return _targetTransform != null;
        }

        private float GetTargetDistance()
        {
            if (!HasValidTarget()) return Mathf.Infinity;

            Vector3 a = transform.position;
            Vector3 b = _targetTransform.position;
            a.y = b.y = 0f;

            return Vector3.Distance(a, b);
        }

        private void FaceTarget()
        {
            if (!HasValidTarget()) return;

            Vector3 dir = _targetTransform.position - modelRoot.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                modelRoot.rotation = Quaternion.Lerp(modelRoot.rotation, targetRot, Time.deltaTime * 10f);
            }
        }

        private void ResetAnimatorToIdle()
        {
            if (!animator) return;

            // Clear any stale states/params so we always start from Idle visually.
            animator.ResetTrigger("Attack");
            animator.SetBool("IsDead", false);
            animator.SetFloat("MoveSpeed", 0f);
            animator.Play("Idle", 0, 0f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, aggroRadius);

            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, loseAggroRadius);

            if (combat != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, combat.AttackRange);
            }

            if (separationRadius > 0f)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, separationRadius);
            }
        }
#endif

        /// <summary>
        /// Returns a sideways push based on nearby living enemies to keep spacing.
        /// </summary>
        private Vector3 CalculateSeparation()
        {
            if (separationRadius <= 0f) return Vector3.zero;
            if (ActiveEnemies.Count <= 1) return Vector3.zero;

            Vector3 separation = Vector3.zero;
            int neighbors = 0;

            for (int i = 0; i < ActiveEnemies.Count; i++)
            {
                var other = ActiveEnemies[i];
                if (other == null || other == this) continue;
                if (!other.isActiveAndEnabled) continue;
                if (other.health != null && other.health.IsDead) continue;

                Vector3 offset = transform.position - other.transform.position;
                offset.y = 0f;
                float dist = offset.magnitude;

                if (dist <= 0.0001f || dist > separationRadius)
                    continue;

                float push = 1f - Mathf.Clamp01(dist / separationRadius);
                separation += offset.normalized * push;
                neighbors++;
            }

            if (neighbors > 0)
                separation /= neighbors;

            if (separation.sqrMagnitude > 0.0001f)
            {
                separation = Vector3.ClampMagnitude(separation * separationWeight, maxSeparationMagnitude);
                separation.y = 0f;
            }

            return separation;
        }

        /// <summary>
        /// Gentle wandering while idle to avoid statuesque mobs.
        /// Picks a random point near the spawn position, walks there, then rests.
        /// </summary>
        private void UpdateIdleWander()
        {
            if (wanderRadius <= 0f)
                return;

            if (_hasWanderTarget == false)
            {
                _wanderCooldown -= Time.deltaTime;
                if (_wanderCooldown <= 0f)
                {
                    PickNewWanderTarget();
                }
                else
                {
                    if (animator) animator.SetFloat("MoveSpeed", 0f);
                }
                return;
            }

            Vector3 toTarget = _wanderTarget - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist <= stopDistanceBuffer * 0.8f)
            {
                _hasWanderTarget = false;
                _wanderCooldown = Random.Range(wanderInterval.x, wanderInterval.y);
                if (animator) animator.SetFloat("MoveSpeed", 0f);
                return;
            }

            Vector3 dir = toTarget.normalized;
            float wanderSpeed = _moveSpeed * Mathf.Clamp01(wanderMoveSpeedMultiplier);
            Vector3 step = dir * (wanderSpeed * Time.deltaTime);
            if (step.magnitude > dist)
                step = dir * dist;

            transform.position += step;

            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                modelRoot.rotation = Quaternion.Lerp(modelRoot.rotation, targetRot, Time.deltaTime * 5f);
            }

            if (animator) animator.SetFloat("MoveSpeed", wanderSpeed);
        }

        private void PickNewWanderTarget()
        {
            // Protect against invalid interval config
            if (wanderInterval.y < wanderInterval.x)
                wanderInterval.y = wanderInterval.x + 0.1f;

            Vector2 offset2D = Random.insideUnitCircle * wanderRadius;
            _wanderTarget = _spawnPosition + new Vector3(offset2D.x, 0f, offset2D.y);
            _hasWanderTarget = true;
        }
    }
}
