// Assets/Scripts/Gameplay/Player/PlayerController.cs
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
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
    private InputAction.CallbackContext pendingClickCtx; // kept if you want to inspect later

    // --- Combat / animation (purely visual for now) ---
    private bool isInCombat;

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
        // --- Handle deferred click-to-move after UI has updated ---
        if (pendingMoveClick)
        {
            pendingMoveClick = false;

            if (!IsPointerOverUI() && TryGetMouseGroundPoint(out var p))
            {
                clickTargetWorld = p;
                hasClickTarget = true;
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

        // ===== CLICK-TO-MOVE ONLY =====
        if (hasClickTarget)
        {
            bool sprintAppliedThisFrame = false;
            Vector3 toTarget = clickTargetWorld - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist <= stopDistance)
            {
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

    // ================= INPUT CALLBACKS =================

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

    // RMB / main ability (Action: AbilityAttack)
    public void OnAbilityAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // For now: purely visual combat mode
        isInCombat = true;
        combatTimer = combatFadeTime;

        if (animator)
        {
            // These parameter names must match the ones in KnightAnimatorController
            animator.SetBool("IsCombat", true);  // switch to combat Idle/Walk
            animator.SetTrigger("Attack");       // play Attack_01
        }

        Debug.Log("AbilityAttack triggered - playing attack animation (no combat logic yet).");
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
