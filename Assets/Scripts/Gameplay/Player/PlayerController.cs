// Assets/Scripts/Gameplay/Player/PlayerController.cs
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.EventSystems;
using Game.Enemies;
using Game.Equipment;
using Game.Items;
using System.Linq;


[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Combat (Auto Attack)")]
    [Tooltip("Distance in front of the player we can hit with an auto attack.")]
    public float attackRange = 2.5f;

    [Tooltip("Radius of the 'hit volume' in front of the player.")]
    public float attackRadius = 0.7f;

    [Tooltip("Layers that can be hit by auto attacks. Usually Default + Enemies + Interactable.")]
    public LayerMask attackMask = ~0; // everything by default

    [Tooltip("Fallback damage if we don't have weapon stats yet.")]
    public int fallbackAttackDamage = 10;

    [Tooltip("Animator state name for the basic attack.")]
    public string attackAnimationState = "Attack_01";

    [Tooltip("Crossfade duration when forcing the attack animation (keeps it snappy).")]
    public float attackCrossfade = 0.05f;

    [Tooltip("Delay from animation start to when the hit is applied.")]
    public float attackHitDelay = 0.2f;

    [Tooltip("Fallback seconds between repeated auto attacks (if the animator length can't be read).")]
    public float attackRepeatFallback = 0.9f;

    [Header("Animator States")]
    [Tooltip("Animator state for basic locomotion (idle/walk/run blend tree). Set this to your locomotion state name.")]
    public string locomotionState = "Locomotion"; // <-- set in Inspector to your actual locomotion state

    [System.Serializable]
    private struct AnimatorStanceConfig
    {
        public string idleState;
        public string attackState;
        public string[] attackCombo;
    }

    [Header("Combat Stances (Animator States)")]
    [SerializeField]
    private AnimatorStanceConfig unarmedStance = new AnimatorStanceConfig
    {
        idleState = "UH_Idle",
        attackState = "UH_Atk_Combo",
        attackCombo = null
    };

    [SerializeField]
    private AnimatorStanceConfig oneHandStance = new AnimatorStanceConfig
    {
        idleState = "1H_Idle",
        attackState = "1H_Atk_Combo",
        attackCombo = null
    };

    [SerializeField]
    private AnimatorStanceConfig oneHandShieldStance = new AnimatorStanceConfig
    {
        idleState = "1HS_Idle",
        attackState = "1HS_Atk_R",
        attackCombo = new[] { "1HS_Atk_R", "1HS_Atk_L", "1HS_Atk_Heavy" }
    };

    [SerializeField]
    private AnimatorStanceConfig twoHandStance = new AnimatorStanceConfig
    {
        idleState = "2H_Idle",
        attackState = "2H_Atk_Combo",
        attackCombo = null
    };

    [SerializeField]
    private AnimatorStanceConfig dualWieldStance = new AnimatorStanceConfig
    {
        idleState = "DW_Idle",
        attackState = "DW_Atk_Combo",
        attackCombo = null
    };

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f;
    public float gravity = -20f;
    public float dodgeSpeed = 12f;
    public float dodgeDuration = 0.25f;
    public float dodgeCooldown = 0.6f;

    [Header("Rotation")]
    public bool rotateWhileStrafing = true; // kept for future WASD support if needed

    [Header("Camera")]
    public Transform cameraPivot; // assign an empty child of the player

    [Header("Mouse & Click-to-Move")]
    public LayerMask groundMask;             // Set to your Ground layer(s) only
    public float stopDistance = 0.15f;       // Distance to consider arrival
    public float faceMouseMaxDistance = 100f;

    [Header("Stamina / Shield / Sprint")]
    [Tooltip("Stamina drained per second while shield is held up.")]
    public float shieldStaminaPerSecond = 25f;

    [Tooltip("Minimum stamina required to raise the shield.")]
    public float minStaminaToRaiseShield = 10f;

    [Tooltip("Flat stamina cost for a dodge roll.")]
    public float dodgeStaminaCost = 25f;

    [Tooltip("Stamina drained per second while sprinting.")]
    public float sprintStaminaPerSecond = 20f;

    [Tooltip("Speed multiplier while sprinting.")]
    public float sprintSpeedMultiplier = 1.5f;

    [Tooltip("Minimum stamina required to *start* sprint.")]
    public float minStaminaToSprint = 5f;

    [SerializeField] private float combatFadeTime = 5f;
    private float combatTimer;
    private Animator animator;

    // Components / state
    private CharacterController cc;
    private PlayerStats stats;
    [SerializeField] private EquipmentController equipment;

    // Motion
    private Vector3 velocity; // vertical (gravity)
    private bool isDodging = false;
    private bool dodgeOnCooldown = false;

    // Shield & sprint state
    private bool shieldHeld = false;
    private bool sprintHeld = false;
    private bool sprintApplying = false;

    // Click-to-move
    private Vector2 mouseScreenPos; // from Point action
    private bool hasClickTarget = false;
    private Vector3 clickTargetWorld;

    // For directional dodge
    private Vector3 lastDesiredDir = Vector3.zero;

    // === Deferred click-to-move handling ===
    private bool pendingMoveClick;
    private InputAction.CallbackContext pendingClickCtx; // (currently unused, but kept)

    // Hover selection
    private EnemyTargetInteractable hoveredEnemy;

    // --- Combat / animation (visual) ---
    private bool isInCombat;
    private enum WeaponStance { Unarmed, OneHand, OneHandShield, TwoHand, DualWield }
    private WeaponStance currentStance = WeaponStance.Unarmed;

    // Auto-attack click targeting
    private IDamageable pendingAttackTarget;
    private Vector3 pendingAttackHitPoint;
    private bool autoAttackOnArrival;

    // Auto-attack loop
    private Coroutine attackLoopRoutine;
    private bool attackLoopActive;
    private bool attackSwingInProgress;
    private IDamageable currentAttackTarget;
    private Vector3 lastAttackFaceDir;

    // Attack cycle cache
    private float lastAttackCycleDuration = 0f;
    private float attackInputLockTimer = 0f;
    private int attackComboIndex = 0;
    private string lastAttackStateName;
    private string lastAttackStatePath;

    // -----------------------------------------------------------------------

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        cc = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();

        if (equipment != null)
        {
            equipment.HasShieldChanged += OnHasShieldChanged;
            equipment.EquippedChanged += OnEquipmentChanged;
        }

        if (groundMask.value == 0)
            groundMask = LayerMask.GetMask("Default");
    }

    private void Start()
    {
        RefreshAnimatorStance(forceCrossfade: true);
    }

    void Update()
    {
        UpdateHoverTarget();

        // --- Handle deferred click-to-move / click-to-attack after UI has updated ---
        if (pendingMoveClick)
        {
            pendingMoveClick = false;

            if (IsPointerOverUI())
                return;

            var cam = Camera.main;
            if (!cam) return;

            Ray ray = cam.ScreenPointToRay(mouseScreenPos);

            // We raycast against everything and then decide what it means.
            if (Physics.Raycast(ray, out RaycastHit hit, faceMouseMaxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                // 1) Did we click something damageable?
                IDamageable dmg = hit.collider.GetComponentInParent<IDamageable>();
                if (dmg != null && !IsTargetDead(dmg))
                {
                    // NEW: if it’s an enemy, fire the selection event
                    var enemyInteractable = hit.collider.GetComponentInParent<EnemyTargetInteractable>();
                    if (enemyInteractable != null)
                    {
                        enemyInteractable.Interact(gameObject); // shows TargetInfoUI
                    }

                    if (attackInputLockTimer > 0f && currentAttackTarget == dmg)
                        return; // ignore spam on same target while swing is locked

                    pendingAttackTarget = dmg;
                    pendingAttackHitPoint = hit.point;
                    autoAttackOnArrival = true;

                    // Move towards the clicked point on the XZ plane
                    Vector3 dest = hit.point;
                    dest.y = transform.position.y;

                    clickTargetWorld = dest;
                    hasClickTarget = true;
                }
                // 2) Otherwise, is this valid ground? -> normal move click.
                else if (((1 << hit.collider.gameObject.layer) & groundMask) != 0)
                {
                    clickTargetWorld = hit.point;
                    hasClickTarget = true;

                    autoAttackOnArrival = false;
                    pendingAttackTarget = null;

                    // NEW: clear current enemy selection when we click ground
                    EnemyTargetInteractable.ClearSelection();

                    // Ground click stops any running attack loop AND cancels attack anim.
                    CancelCurrentAttack(resetCombatPose: false);
                }
            }
            else
            {
                // Missed everything; treat as cancel.
                EnemyTargetInteractable.ClearSelection();   // NEW: also clear here
                CancelCurrentAttack(resetCombatPose: false);
            }
        }

        ApplyGravity();

        if (attackInputLockTimer > 0f)
            attackInputLockTimer = Mathf.Max(0f, attackInputLockTimer - Time.deltaTime);

        // --- Shield stamina drain ---
        if (shieldHeld && stats != null)
        {
            float drain = shieldStaminaPerSecond * Time.deltaTime;
            bool ok = stats.TryConsumeStamina(drain);

            if (!ok)
            {
                shieldHeld = false;
                if (animator)
                    animator.SetBool("IsBlocking", false);
                Debug.Log("Shield DOWN (stamina exhausted)");
            }
        }

        // While dodging, ignore regular movement
        if (isDodging)
        {
            cc.Move(velocity * Time.deltaTime);
            return;
        }

        Vector3 desiredDir = Vector3.zero;
        Vector3 motion = Vector3.zero;

        // ===== CLICK-TO-MOVE (with optional auto-attack on arrival) =====
        if (hasClickTarget)
        {
            if (autoAttackOnArrival && pendingAttackTarget != null && IsTargetDead(pendingAttackTarget))
            {
                hasClickTarget = false;
                autoAttackOnArrival = false;
                pendingAttackTarget = null;
                motion = Vector3.zero;
            }
            else
            {
                bool sprintAppliedThisFrame = false;
                Vector3 toTarget = clickTargetWorld - transform.position;
                toTarget.y = 0f;
                float dist = toTarget.magnitude;

                bool hasPendingAttack = autoAttackOnArrival && pendingAttackTarget != null && !IsTargetDead(pendingAttackTarget);

                if (hasPendingAttack && dist <= attackRange * 0.9f)
                {
                    hasClickTarget = false;

                    Vector3 faceDir = pendingAttackTarget is Component c
                        ? (c.transform.position - transform.position)
                        : toTarget;
                    faceDir.y = 0f;

                    if (faceDir.sqrMagnitude > 0.0001f)
                        transform.rotation = Quaternion.LookRotation(faceDir.normalized);

                    lastAttackFaceDir = faceDir.normalized;
                    currentAttackTarget = pendingAttackTarget;

                    StartAutoAttackLoop();

                    autoAttackOnArrival = false;
                    pendingAttackTarget = null;

                    motion = Vector3.zero;
                }
                else if (dist <= stopDistance || IsTargetDead(pendingAttackTarget))
                {
                    hasClickTarget = false;
                    motion = Vector3.zero;
                    autoAttackOnArrival = false;
                    pendingAttackTarget = null;
                }
                else
                {
                    float effectiveMoveSpeed = moveSpeed;

                    if (sprintHeld && stats != null)
                    {
                        float sprintCost = sprintStaminaPerSecond * Time.deltaTime;

                        if (stats.TryConsumeStamina(sprintCost))
                        {
                            effectiveMoveSpeed *= sprintSpeedMultiplier;
                            sprintAppliedThisFrame = true;
                        }
                        else
                        {
                            sprintHeld = false;
                            Debug.Log("Sprint stopped (stamina exhausted)");
                        }
                    }

                    desiredDir = toTarget / Mathf.Max(dist, 0.0001f);
                    float maxStep = effectiveMoveSpeed * Time.deltaTime;
                    float targetStep = Mathf.Max(0f, dist - stopDistance);
                    float step = Mathf.Min(maxStep, targetStep);
                    motion = desiredDir * (step / Mathf.Max(Time.deltaTime, 0.0001f));
                }

                if (sprintAppliedThisFrame && !sprintApplying)
                {
                    Debug.Log($"Sprint applied (x{sprintSpeedMultiplier:0.00}, stamina now {stats?.CurrentStamina.ToString("F1") ?? "n/a"})");
                }
                else if (!sprintAppliedThisFrame && sprintApplying && sprintHeld)
                {
                    float planarSpeed = new Vector2(motion.x, motion.z).magnitude;
                    Debug.Log($"Sprint input held but boost not applied (hasClickTarget={hasClickTarget}, planarSpeed={planarSpeed:0.00})");
                }

                sprintApplying = sprintAppliedThisFrame;
            }
        }
        else
        {
            if (sprintApplying && sprintHeld)
            {
                Debug.Log("Sprint held but no click target; boost not applied this frame.");
            }
            sprintApplying = false;
        }

        // Apply movement
        if (shieldHeld)
        {
            Vector2 planarMove = new Vector2(motion.x, motion.z);
            if (planarMove.sqrMagnitude > 0.0001f)
            {
                shieldHeld = false;
                if (animator)
                    animator.SetBool("IsBlocking", false);
            }
        }

        cc.Move((motion + new Vector3(0f, velocity.y, 0f)) * Time.deltaTime);

        float horizontalSpeed = new Vector3(motion.x, 0f, motion.z).magnitude;
        if (animator) animator.SetFloat("Speed", horizontalSpeed);

        // Cache lastDesiredDir for dodge
        if (desiredDir.sqrMagnitude > 0.0001f)
        {
            lastDesiredDir = desiredDir;
        }
        else if (hasClickTarget)
        {
            Vector3 toTarget = clickTargetWorld - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
                lastDesiredDir = toTarget.normalized;
        }

        // -------- Rotation (click-to-move) --------
        if (hasClickTarget)
        {
            Vector3 toTarget = clickTargetWorld - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                RotateTowards(toTarget.normalized);
            }
            else if (TryGetMouseGroundPoint(out var mousePoint))
            {
                Vector3 faceDir = mousePoint - transform.position;
                faceDir.y = 0f;
                if (faceDir.sqrMagnitude > 0.0001f)
                    RotateTowards(faceDir.normalized);
            }
        }

        // --- simple auto-exit combat pose ---
        if (isInCombat)
        {
            combatTimer -= Time.deltaTime;
            if (combatTimer <= 0f)
            {
                isInCombat = false;
                if (animator)
                {
                    animator.SetBool("IsCombat", false); // back to Safe_Idle / Safe_Walk
                }
            }
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
    private int GetAnimatorLayerForCurrentStance()
    {
        // All stances live on base layer (index 0) in this controller.
        return 0;
    }

    private string GetAnimatorStatePath(WeaponStance stance, string stateShortName)
    {
        string subMachine = stance switch
        {
            WeaponStance.Unarmed => "Combat_Unarmed",
            WeaponStance.OneHand => "Combat_1H",
            WeaponStance.OneHandShield => "Combat_1H_Shield",
            WeaponStance.TwoHand => "Combat_2H",
            WeaponStance.DualWield => "Combat_Duel_Weapon",
            _ => "Combat_1H"
        };

        return $"Base Layer.{subMachine}.{stateShortName}";
    }

    private void UpdateHoverTarget()
    {
        // Do not hover when UI is blocking input
        if (IsPointerOverUI())
        {
            if (hoveredEnemy != null)
            {
                hoveredEnemy = null;
                EnemyTargetInteractable.SetHover(null);
            }
            return;
        }

        var cam = Camera.main;
        if (!cam)
        {
            if (hoveredEnemy != null)
            {
                hoveredEnemy = null;
                EnemyTargetInteractable.SetHover(null);
            }
            return;
        }

        Ray ray = cam.ScreenPointToRay(mouseScreenPos);
        EnemyTargetInteractable nextHover = null;

        if (Physics.Raycast(ray, out RaycastHit hit, faceMouseMaxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            var candidate = hit.collider.GetComponentInParent<EnemyTargetInteractable>();
            if (candidate != null)
            {
                // Skip dead enemies
                if (candidate.health == null || !candidate.health.IsDead)
                {
                    nextHover = candidate;
                }
            }
        }

        if (nextHover != hoveredEnemy)
        {
            hoveredEnemy = nextHover;
            EnemyTargetInteractable.SetHover(nextHover);
        }
    }

    private AnimatorStanceConfig GetAnimatorConfig(WeaponStance stance)
    {
        return stance switch
        {
            WeaponStance.Unarmed => unarmedStance,
            WeaponStance.OneHand => oneHandStance,
            WeaponStance.OneHandShield => oneHandShieldStance,
            WeaponStance.TwoHand => twoHandStance,
            WeaponStance.DualWield => dualWieldStance,
            _ => oneHandStance
        };
    }

    private WeaponStance DetermineStanceFromEquipment()
    {
        if (!equipment) return WeaponStance.Unarmed;

        ItemDefinition right = equipment.GetEquipped(EquipmentSlot.RightHand);
        ItemDefinition left = equipment.GetEquipped(EquipmentSlot.LeftHand);

        bool shieldEquipped = IsShieldItem(left);
        bool rightTwoHanded = IsTwoHandedWeapon(right);

        if (rightTwoHanded)
            return WeaponStance.TwoHand;

        bool rightOneHand = IsOneHandedWeapon(right);
        bool leftOneHand = IsOneHandedWeapon(left);

        if (shieldEquipped && rightOneHand)
            return WeaponStance.OneHandShield;

        if (rightOneHand && leftOneHand)
            return WeaponStance.DualWield;

        if (rightOneHand || leftOneHand)
            return WeaponStance.OneHand;

        return WeaponStance.Unarmed;
    }

    private void RefreshAnimatorStance(bool forceCrossfade = false)
    {
        WeaponStance next = DetermineStanceFromEquipment();
        bool stanceChanged = forceCrossfade || next != currentStance;
        currentStance = next;
        attackComboIndex = 0;

        AnimatorStanceConfig config = GetAnimatorConfig(next);
        locomotionState = config.idleState;
        attackAnimationState = config.attackState;
        lastAttackStateName = attackAnimationState;   // short name
        lastAttackStatePath = GetAnimatorStatePath(currentStance, lastAttackStateName);

        if (!animator)
            return;

        bool hasShield = next == WeaponStance.OneHandShield;
        animator.SetBool("HasShield", hasShield);

        if (stanceChanged && !string.IsNullOrEmpty(locomotionState))
        {
            // we crossfade directly to the idle state on base layer (0)
            animator.CrossFadeInFixedTime(GetAnimatorStatePath(currentStance, locomotionState), 0.1f, 0);
        }
    }


    private void ResetAttackCombo()
    {
        attackComboIndex = 0;
        lastAttackStateName = attackAnimationState;
        lastAttackStatePath = GetAnimatorStatePath(currentStance, lastAttackStateName);
    }

    private void StopMovementImmediately()
    {
        hasClickTarget = false;
        autoAttackOnArrival = false;
        pendingAttackTarget = null;
        if (animator) animator.SetFloat("Speed", 0f);
    }

    private void ClearAttackParam()
    {
        if (!animator) return;

        var attackParam = animator.parameters.FirstOrDefault(p => p.name == "Attack");
        switch (attackParam.type)
        {
            case AnimatorControllerParameterType.Trigger:
                animator.ResetTrigger("Attack");
                break;
            case AnimatorControllerParameterType.Bool:
                animator.SetBool("Attack", false);
                break;
        }
    }


    private static bool IsOneHandedWeapon(ItemDefinition def) =>
        def != null && def.category == ItemCategory.Weapon && def.grip == WeaponGrip.OneHanded;

    private static bool IsTwoHandedWeapon(ItemDefinition def) =>
        def != null && def.category == ItemCategory.Weapon && def.grip == WeaponGrip.TwoHanded;

    private static bool IsShieldItem(ItemDefinition def) =>
        def != null && (def.category == ItemCategory.Shield || def.subtype == ItemSubtype.Shield);

    // Auto attack trigger (animation + delayed hit)
    private void TriggerAutoAttack()
    {
        if (IsTargetDead(currentAttackTarget))
        {
            StopAutoAttackLoop();
            return;
        }

        // Enter combat pose
        isInCombat = true;
        combatTimer = combatFadeTime;
        attackInputLockTimer = Mathf.Max(attackInputLockTimer, GetAttackCycleDuration());

        if (animator)
        {
            animator.SetBool("IsCombat", true);  // stay in combat idle/walk

            string stateName = GetNextAttackStateName();   // e.g. "1HS_Atk_Combo"
            lastAttackStateName = stateName;

            if (!string.IsNullOrEmpty(stateName))
            {
                int layer = GetAnimatorLayerForCurrentStance(); // 0
                int hash = Animator.StringToHash(stateName);

                bool has = animator.HasState(layer, hash);
                Debug.Log($"[Attack] stance={currentStance} stateName={stateName} layer={layer} hasState={has}");

                if (has)
                {
                    animator.CrossFadeInFixedTime(
                        stateName,                         // SHORT NAME ONLY
                        Mathf.Max(0f, attackCrossfade),
                        layer,
                        0f);
                    ClearAttackParam(); // drop trigger/bool immediately
                }
                else
                {
                    Debug.LogWarning($"Attack state '{stateName}' not found on layer {layer}, using Attack trigger instead");
                    animator.SetTrigger("Attack");
                }
            }
            else
            {
                animator.SetTrigger("Attack"); // fallback to trigger
            }
        }

        // Apply the actual hit slightly after the swing starts
        StartCoroutine(PerformAutoAttackAfterDelay(attackHitDelay));
    }


    // Rotates the player to face either a clicked damageable or the clicked point.
    private void FaceMouseClickDirection()
    {
        var cam = Camera.main;
        if (!cam) return;

        Ray ray = cam.ScreenPointToRay(mouseScreenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, faceMouseMaxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            Vector3 dir;

            // Prefer the transform of an IDamageable (dummy, enemy, etc.)
            IDamageable dmg = hit.collider.GetComponentInParent<IDamageable>();
            if (dmg is Component c)
            {
                dir = c.transform.position - transform.position;
            }
            else
            {
                dir = hit.point - transform.position;
            }

            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                lastAttackFaceDir = dir.normalized;
                RotateTowards(dir.normalized);
            }
        }
    }

    void RotateTowards(Vector3 worldDir)
    {
        if (worldDir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(worldDir);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );
    }

    bool TryGetMouseGroundPoint(out Vector3 hitPoint)
    {
        hitPoint = default;
        var cam = Camera.main;
        if (!cam) return false;

        Ray ray = cam.ScreenPointToRay(mouseScreenPos);

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                faceMouseMaxDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            // Ignore hits on the player itself
            if (hit.collider && hit.collider.transform.IsChildOf(transform))
                return false;

            hitPoint = hit.point;
            return true;
        }
        return false;
    }

    void ApplyGravity()
    {
        if (cc.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
    }

    private IEnumerator PerformAutoAttackAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        DoAutoAttackHit();
    }

    /// <summary>
    /// Performs a short spherecast in front of the player and deals damage to the first IDamageable hit.
    /// </summary>
    private void DoAutoAttackHit()
    {
        if (!enabled) return;
        if (IsTargetDead(currentAttackTarget))
        {
            StopAutoAttackLoop();
            return;
        }

        // --- Decide damage ---
        int dmg = fallbackAttackDamage;

        if (stats != null && stats.equipDamageMax > 0)
        {
            int min = Mathf.Max(1, stats.equipDamageMin);
            int max = Mathf.Max(min, stats.equipDamageMax);
            dmg = Random.Range(min, max + 1); // max is exclusive
        }

        // --- Origin & direction ---
        // Cast from around the player's chest height so we actually intersect low dummies / enemies.
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 dir = transform.forward;

        if (Physics.SphereCast(
                origin,
                attackRadius,
                dir,
                out RaycastHit hit,
                attackRange,
                attackMask,
                QueryTriggerInteraction.Ignore))
        {
            // Ignore hitting ourselves
            if (hit.collider && hit.collider.transform.IsChildOf(transform))
                return;

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeHit(dmg, hit.point, hit.normal);
                Debug.Log($"Auto attack hit {hit.collider.name} for {dmg} dmg.", hit.collider);
            }
        }
        else
        {
            Debug.Log("Auto attack swing hit nothing.");
        }
    }

    // -----------------------------------------------------------------------
    // Auto-attack helpers / loop
    // -----------------------------------------------------------------------

    private bool IsInAttackState()
    {
        if (!animator) return false;

        string stateName = !string.IsNullOrEmpty(lastAttackStateName)
            ? lastAttackStateName
            : attackAnimationState;

        if (string.IsNullOrEmpty(stateName)) return false;

        int layer = GetAnimatorLayerForCurrentStance();
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);
        return info.IsName(stateName);   // short name
    }




    private bool TryGetDamageableFromMouse(out IDamageable dmg, out Vector3 hitPoint)
    {
        dmg = null;
        hitPoint = default;

        var cam = Camera.main;
        if (!cam) return false;

        Ray ray = cam.ScreenPointToRay(mouseScreenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, faceMouseMaxDistance, ~0,
                QueryTriggerInteraction.Ignore))
        {
            var maybeDmg = hit.collider.GetComponentInParent<IDamageable>();
            if (maybeDmg != null && !IsTargetDead(maybeDmg))
            {
                dmg = maybeDmg;
                hitPoint = hit.point;
                return true;
            }
        }

        return false;
    }

    private void StartAutoAttackLoop()
    {
        attackLoopActive = true;

        if (attackLoopRoutine == null)
            attackLoopRoutine = StartCoroutine(AutoAttackLoop());
    }

    private void StopAutoAttackLoop()
    {
        attackLoopActive = false;
        attackSwingInProgress = false;
        currentAttackTarget = null;
        ResetAttackCombo();
        ClearAttackParam();

        if (attackLoopRoutine != null)
        {
            StopCoroutine(attackLoopRoutine);
            attackLoopRoutine = null;
        }
    }
    private void CancelCurrentAttack(bool resetCombatPose = false)
    {
        // Were we actually in an attack loop / state?
        bool wasLooping = attackLoopActive || attackSwingInProgress;
        bool wasInAttackState = IsInAttackState();

        // Always stop the loop + clear target
        StopAutoAttackLoop();
        ResetAttackCombo();
        ClearAttackParam();

        // Only force a crossfade if we were mid-attack
        if (animator && (wasLooping || wasInAttackState))
        {
            if (!string.IsNullOrEmpty(locomotionState))
            {
                // Just use the short state name on base layer (0)
                animator.CrossFadeInFixedTime(locomotionState, 0.1f, 0);
            }
        }

        if (resetCombatPose && animator)
        {
            isInCombat = false;
            animator.SetBool("IsCombat", false);
        }
    }

    private void FaceCurrentAttackTarget()
    {
        if (currentAttackTarget is Component c)
        {
            Vector3 dir = c.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                lastAttackFaceDir = dir.normalized;
                RotateTowards(dir.normalized);
                return;
            }
        }

        if (lastAttackFaceDir.sqrMagnitude > 0.0001f)
            RotateTowards(lastAttackFaceDir);
    }

    private float GetAttackCycleDuration()
    {
        float minDuration = Mathf.Max(attackRepeatFallback, attackHitDelay + attackCrossfade);
        string stateToCheck = !string.IsNullOrEmpty(lastAttackStateName)
            ? lastAttackStateName
            : attackAnimationState;

        if (animator && !string.IsNullOrEmpty(stateToCheck))
        {
            int layer = GetAnimatorLayerForCurrentStance();
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);
            if (info.IsName(stateToCheck))
            {
                float animDuration = info.length / Mathf.Max(animator.speed, 0.0001f);
                lastAttackCycleDuration = Mathf.Max(animDuration, minDuration);
                return lastAttackCycleDuration;
            }
        }

        if (lastAttackCycleDuration > 0.0001f)
            return Mathf.Max(lastAttackCycleDuration, minDuration);

        return minDuration;
    }

    // Stop the loop when the target is gone / out of range
    private bool IsTargetValid()
    {
        if (!(currentAttackTarget is Component c)) return false;

        if (IsTargetDead(currentAttackTarget))
            return false;

        // Destroyed or disabled
        if (c == null || !c.gameObject.activeInHierarchy) return false;

        // Optional: stop if too far
        Vector3 toTarget = c.transform.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;
        return dist <= attackRange * 1.1f; // small slack
    }

    private bool IsTargetDead(IDamageable dmg)
    {
        if (dmg is EnemyHealth eh)
            return eh.IsDead;

        return false;
    }

    private IEnumerator AutoAttackLoop()
    {
        while (attackLoopActive)
        {
            // No valid target? Stop auto attack.
            if (!IsTargetValid())
            {
                StopAutoAttackLoop();
                yield break;
            }

            if (!attackSwingInProgress)
            {
                attackSwingInProgress = true;
                FaceCurrentAttackTarget();
                TriggerAutoAttack();
                yield return new WaitForSeconds(GetAttackCycleDuration());
                attackSwingInProgress = false;
            }
            else
            {
                yield return null;
            }
        }

        attackLoopRoutine = null;
    }

    // -----------------------------------------------------------------------
    // INPUT CALLBACKS
    // -----------------------------------------------------------------------

    // We keep OnMove in case we ever re-enable WASD, but it does nothing now.
    public void OnMove(InputAction.CallbackContext ctx)
    {
        // Intentionally unused (no WASD movement).
    }

    public void OnPoint(InputAction.CallbackContext ctx)
    {
        if (ctx.performed || ctx.started)
            mouseScreenPos = ctx.ReadValue<Vector2>();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;
        }

        return false;
    }

    // LMB: click-to-move (Action: MoveClick)
    public void OnMoveClick(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            // IMPORTANT: if Alt is held, we *don't* treat this as a move click.
            if (Keyboard.current != null && Keyboard.current.leftAltKey.isPressed)
                return;

            pendingMoveClick = true;
            pendingClickCtx = ctx;
        }
    }


    // Space: directional dodge
    public void OnDodge(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || isDodging || dodgeOnCooldown)
            return;

        if (stats != null && !stats.TryConsumeStamina(dodgeStaminaCost))
        {
            Debug.Log("Not enough stamina to dodge.");
            return;
        }

        StartCoroutine(DodgeRoutine());
    }

    IEnumerator DodgeRoutine()
    {
        isDodging = true;
        dodgeOnCooldown = true;

        Vector3 dodgeDir = lastDesiredDir.sqrMagnitude > 0.0001f
            ? lastDesiredDir
            : new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        float t = 0f;
        while (t < dodgeDuration)
        {
            Vector3 horiz = dodgeDir * dodgeSpeed;
            cc.Move((horiz + new Vector3(0f, velocity.y, 0f)) * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }

        isDodging = false;
        yield return new WaitForSeconds(dodgeCooldown);
        dodgeOnCooldown = false;
    }

    // Alt + LMB: basic auto attack in place (Action: BasicAttack)
    public void OnBasicAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // Require Alt to be held for the in-place attack override.
        if (Keyboard.current == null || !Keyboard.current.leftAltKey.isPressed)
            return;

        bool hasDmg = TryGetDamageableFromMouse(out var dmg, out var hitPoint);
        if (hasDmg)
        {
            if (attackInputLockTimer > 0f && currentAttackTarget == dmg)
                return; // ignore spam on same target mid-swing

            // Small safety tweak when retargeting:
            // only update target if we actually clicked a damageable.
            Vector3 dir = hitPoint - transform.position;
            dir.y = 0f;
            lastAttackFaceDir = dir.normalized;
            currentAttackTarget = dmg;
        }

        FaceMouseClickDirection();

        // Only start / continue loop if we have a valid target.
        if (hasDmg || currentAttackTarget != null)
        {
            StartAutoAttackLoop();
        }
    }

    // RMB / future special ability (Action: AbilityAttack)
    public void OnAbilityAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        bool hasDmg = TryGetDamageableFromMouse(out var dmg, out var hitPoint);
        if (hasDmg)
        {
            if (attackInputLockTimer > 0f && currentAttackTarget == dmg)
                return; // ignore spam on same target mid-swing

            Vector3 dir = hitPoint - transform.position;
            dir.y = 0f;
            lastAttackFaceDir = dir.normalized;
            currentAttackTarget = dmg;
        }

        FaceMouseClickDirection();

        // Only start / continue loop if we have a valid target.
        if (hasDmg || currentAttackTarget != null)
        {
            StartAutoAttackLoop();
        }
    }

    // Clean “cancel attack” input (Action: AttackCancel)
    public void OnAttackCancel(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // Here we also drop combat pose if you want
        CancelCurrentAttack(resetCombatPose: true);
    }


    public void OnEmoteWheel(InputAction.CallbackContext ctx)
    {
        if (UiInputGuard.IsBlocked) return;

        if (ctx.started) Debug.Log("Emote Wheel Open");
        if (ctx.canceled) Debug.Log("Emote Wheel Close");
    }

    // Left Shift: Shield up / down (Action: Shield)
    public void OnShield(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            if (stats != null && stats.CurrentStamina >= minStaminaToRaiseShield)
            {
                StopMovementImmediately(); // halt walking the moment shield is raised
                shieldHeld = true;

                // Enter combat stance when raising shield
                isInCombat = true;
                combatTimer = combatFadeTime;
                if (animator)
                    animator.SetBool("IsCombat", true);

                Debug.Log("Shield UP");
            }
            else
            {
                shieldHeld = false;
                Debug.Log("Cannot raise shield - not enough stamina.");
            }
        }
        else if (ctx.canceled)
        {
            if (shieldHeld)
                Debug.Log("Shield DOWN (released).");

            shieldHeld = false;
        }

        // Drive animator "IsBlocking" flag from shieldHeld
        if (animator)
            animator.SetBool("IsBlocking", shieldHeld);
    }


    // R: Parry (Action: Parry)
    public void OnParry(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        if (!shieldHeld)
        {
            Debug.Log("Parry attempted, but shield is not up.");
            return;
        }

        if (animator)
            animator.SetTrigger("ParryTrigger");

        Debug.Log("Parry performed - TODO: start perfect block window.");
    }


    // Left Ctrl: Sprint hold (Action: Sprint)
    public void OnSprint(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            Debug.Log($"OnSprint started (stamina={stats?.CurrentStamina.ToString("F1") ?? "n/a"}, minRequired={minStaminaToSprint})");
            if (stats != null && stats.CurrentStamina >= minStaminaToSprint)
            {
                sprintHeld = true;
                Debug.Log("Sprint HELD");
            }
            else
            {
                sprintHeld = false;
                Debug.Log("Not enough stamina to start sprint.");
            }
        }
        else if (ctx.canceled)
        {
            Debug.Log("OnSprint canceled input.");
            if (sprintHeld)
                Debug.Log("Sprint released.");

            sprintHeld = false;
        }
    }

    // Q/W/E: Potion slots (UsePotion1/2/3)
    public void OnUsePotion1(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        Debug.Log("Use Potion Slot 1 (Q) - TODO: hook to consumable system.");
    }

    public void OnUsePotion2(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        Debug.Log("Use Potion Slot 2 (W) - TODO: hook to consumable system.");
    }

    public void OnUsePotion3(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        Debug.Log("Use Potion Slot 3 (E) - TODO: hook to consumable system.");
    }

    private void OnEquipmentChanged()
    {
        RefreshAnimatorStance();
    }

    private void OnDestroy()
    {
        if (equipment != null)
        {
            equipment.HasShieldChanged -= OnHasShieldChanged;
            equipment.EquippedChanged -= OnEquipmentChanged;
        }
    }

    private void OnHasShieldChanged(bool hasShield)
    {
        if (animator)
            animator.SetBool("HasShield", hasShield);

        RefreshAnimatorStance();
    }

    private string GetNextAttackStateName()
    {
        AnimatorStanceConfig config = GetAnimatorConfig(currentStance);

        // Always return the single combo clip for the stance
        if (!string.IsNullOrEmpty(config.attackState))
            return config.attackState;

        return attackAnimationState;   // fallback (but in practice should never be used)
    }

}
