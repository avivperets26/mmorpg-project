// Assets/Scripts/Gameplay/Player/PlayerController.cs
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.EventSystems;

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

    // --- Combat / animation (visual) ---
    private bool isInCombat;

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

    // Attack cyc le cache
    private float lastAttackCycleDuration = 0f;

    // -----------------------------------------------------------------------

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        cc = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();

        if (groundMask.value == 0)
            groundMask = LayerMask.GetMask("Default");
    }

    void Update()
    {
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
                if (dmg != null)
                {
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

                    // Ground click stops any running attack loop.
                    StopAutoAttackLoop();
                }
            }
            else
            {
                // Missed everything; treat as cancel.
                StopAutoAttackLoop();
            }
        }

        ApplyGravity();

        // --- Shield stamina drain ---
        if (shieldHeld && stats != null)
        {
            float drain = shieldStaminaPerSecond * Time.deltaTime;
            bool ok = stats.TryConsumeStamina(drain);

            if (!ok)
            {
                shieldHeld = false;
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
            bool sprintAppliedThisFrame = false;
            Vector3 toTarget = clickTargetWorld - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            // Are we chasing something we want to auto-attack?
            bool hasPendingAttack = autoAttackOnArrival && pendingAttackTarget != null;

            // If we have a pending attack and we're close enough, stop and attack.
            if (hasPendingAttack && dist <= attackRange * 0.9f)
            {
                hasClickTarget = false;

                // Face the target *instantly* so we never swing in the wrong direction
                Vector3 faceDir = pendingAttackTarget is Component c
                    ? (c.transform.position - transform.position)
                    : toTarget;
                faceDir.y = 0f;

                if (faceDir.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(faceDir.normalized);

                lastAttackFaceDir = faceDir.normalized;
                currentAttackTarget = pendingAttackTarget;

                // Start looping auto attacks (first swing fires immediately)
                StartAutoAttackLoop();

                autoAttackOnArrival = false;
                pendingAttackTarget = null;

                // No movement this frame
                motion = Vector3.zero;
            }
            else if (dist <= stopDistance)
            {
                // Normal arrival with no pending attack
                hasClickTarget = false;
            }
            else
            {
                float effectiveMoveSpeed = moveSpeed;

                // 🔹 Sprint: hold Left Ctrl while moving
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
                motion = desiredDir * effectiveMoveSpeed;
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
        else
        {
            if (sprintApplying && sprintHeld)
            {
                Debug.Log("Sprint held but no click target; boost not applied this frame.");
            }
            sprintApplying = false;
        }

        // Apply movement
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

    // Auto attack trigger (animation + delayed hit)
    private void TriggerAutoAttack()
    {
        // Enter combat pose
        isInCombat = true;
        combatTimer = combatFadeTime;

        if (animator)
        {
            animator.SetBool("IsCombat", true);  // stay in combat idle/walk
            // Force the attack animation immediately so we don't wait on long state exit times.
            if (!string.IsNullOrEmpty(attackAnimationState))
            {
                animator.CrossFadeInFixedTime(
                    attackAnimationState,
                    Mathf.Max(0f, attackCrossfade),
                    0,
                    0f);
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

    private bool TryGetDamageableFromMouse(out IDamageable dmg, out Vector3 hitPoint)
    {
        dmg = null;
        hitPoint = default;

        var cam = Camera.main;
        if (!cam) return false;

        Ray ray = cam.ScreenPointToRay(mouseScreenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, faceMouseMaxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            dmg = hit.collider.GetComponentInParent<IDamageable>();
            hitPoint = hit.point;
            return dmg != null;
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

        if (attackLoopRoutine != null)
        {
            StopCoroutine(attackLoopRoutine);
            attackLoopRoutine = null;
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
        // Require at least the fallback so we never get the short "first repeat" burst.
        float minDuration = Mathf.Max(attackRepeatFallback, attackHitDelay + attackCrossfade);

        if (animator && !string.IsNullOrEmpty(attackAnimationState))
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(attackAnimationState))
            {
                float animDuration = info.length / Mathf.Max(animator.speed, 0.0001f);
                lastAttackCycleDuration = Mathf.Max(animDuration, minDuration);
                return lastAttackCycleDuration;
            }
        }

        // If we can't read the state yet (e.g., first frame after crossfade), stick to the last known duration.
        if (lastAttackCycleDuration > 0.0001f)
            return Mathf.Max(lastAttackCycleDuration, minDuration);

        return minDuration;
    }

    // 1) Stop the loop when the target is gone / out of range
    private bool IsTargetValid()
    {
        if (!(currentAttackTarget is Component c)) return false;

        // Destroyed or disabled
        if (c == null || !c.gameObject.activeInHierarchy) return false;

        // Optional: stop if too far
        Vector3 toTarget = c.transform.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;
        return dist <= attackRange * 1.1f; // small slack
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

    // 2) Optional: allow a clean “cancel attack” input (Action: AttackCancel)
    // Bind this in your Input Actions (e.g. Escape or some key).
    public void OnAttackCancel(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        StopAutoAttackLoop();
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
                shieldHeld = true;
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
}
