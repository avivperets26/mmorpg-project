// Assets/Scripts/Enemies/Runtime/EnemyAIController.cs
using UnityEngine;

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

        [Header("Debug")]
        [SerializeField] private State _state = State.Idle;

        private Transform _targetTransform;
        private float _moveSpeed = 2.0f;

        private void Reset()
        {
            if (!stats) stats = GetComponent<EnemyStats>();
            if (!health) health = GetComponent<EnemyHealth>();
            if (!combat) combat = GetComponent<EnemyCombatController>();
            if (!modelRoot) modelRoot = transform;
        }

        private void Awake()
        {
            if (!stats) stats = GetComponent<EnemyStats>();
            if (!health) health = GetComponent<EnemyHealth>();
            if (!combat) combat = GetComponent<EnemyCombatController>();
            if (!modelRoot) modelRoot = transform;

            if (!targetPlayer)
                targetPlayer = FindObjectOfType<PlayerStats>();

            if (targetPlayer)
                _targetTransform = targetPlayer.transform;

            _moveSpeed = stats != null && stats.MoveSpeed > 0f
                ? stats.MoveSpeed
                : 2.0f;
        }

        private void OnEnable()
        {
            if (health != null)
                health.OnDeath += OnDeath;
        }

        private void OnDisable()
        {
            if (health != null)
                health.OnDeath -= OnDeath;
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
            if (!HasValidTarget()) return;

            float dist = GetTargetDistance();
            if (dist <= aggroRadius)
            {
                _state = State.Chasing;
            }
        }

        private void TickChasing()
        {
            if (!HasValidTarget())
            {
                _state = State.Idle;
                return;
            }

            float dist = GetTargetDistance();

            if (dist > loseAggroRadius)
            {
                // Player ran far away; drop aggro for now.
                _state = State.Idle;
                return;
            }

            float attackRange = combat != null ? combat.AttackRange : 2f;

            if (dist <= attackRange)
            {
                _state = State.Attacking;
                return;
            }

            // Move towards the player (simple planar move)
            Vector3 dir = _targetTransform.position - transform.position;
            dir.y = 0f;
            float planarDist = dir.magnitude;

            if (planarDist > stopDistanceBuffer)
            {
                Vector3 step = dir.normalized * (_moveSpeed * Time.deltaTime);
                if (step.magnitude > planarDist)
                    step = dir.normalized * planarDist;

                transform.position += step;
            }

            FaceTarget();
        }

        private void TickAttacking()
        {
            if (!HasValidTarget())
            {
                _state = State.Idle;
                return;
            }

            float attackRange = combat != null ? combat.AttackRange : 2f;
            float dist = GetTargetDistance();

            if (dist > attackRange * 1.1f)
            {
                _state = State.Chasing;
                return;
            }

            FaceTarget();

            // Fire attacks via combat controller
            if (combat != null)
            {
                combat.TryAttack(_targetTransform);
            }
        }

        private void OnDeath(EnemyHealth _)
        {
            _state = State.Dead;
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
        }
#endif
    }
}
