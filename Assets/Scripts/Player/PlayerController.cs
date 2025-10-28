using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

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

    [Header("Mouse & Click-to-Move")]
    public LayerMask groundMask;             // Set to your Ground layer(s) only
    public float stopDistance = 0.15f;       // Distance to consider arrival
    public float faceMouseMaxDistance = 100f;
    private Animator animator;

    // Components / state
    private CharacterController cc;
    private PlayerStats stats;

    // Inputs / motion
    private Vector2 moveInput;               // WASD vector
    private Vector3 velocity;                // vertical (gravity)
    private bool isDodging = false;
    private bool dodgeOnCooldown = false;

    // Click-to-move
    private Vector2 mouseScreenPos;          // from Point action
    private bool hasClickTarget = false;
    private Vector3 clickTargetWorld;

    // For directional dodge
    private Vector3 lastDesiredDir = Vector3.zero;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        cc = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();

        // Sensible default if mask is unassigned
        if (groundMask.value == 0)
            groundMask = LayerMask.GetMask("Default");
    }

    void Update()
    {
        ApplyGravity();

        if (isDodging)
        {
            cc.Move(velocity * Time.deltaTime);
            return;
        }

        bool hasKeyboardInput = moveInput.sqrMagnitude > 0.0001f;

        // If keyboard is active, cancel click-to-move so they don't fight
        if (hasKeyboardInput) { hasClickTarget = false; }

        // --- Cache camera basis BEFORE any rotation this frame ---
        var cam = Camera.main;
        Vector3 camFwd = Vector3.forward;
        Vector3 camRight = Vector3.right;
        if (cam)
        {
            camFwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            camRight = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
        }

        Vector3 desiredDir = Vector3.zero;
        Vector3 motion = Vector3.zero;

        // Keyboard movement
        if (hasKeyboardInput)
        {
            float effectiveMoveSpeed = stats ? stats.GetEffectiveMoveSpeed() : moveSpeed;
            desiredDir = (camFwd * moveInput.y + camRight * moveInput.x).normalized;
            motion = desiredDir * effectiveMoveSpeed;
        }
        else if (hasClickTarget)
        {
            Vector3 toTarget = clickTargetWorld - transform.position; toTarget.y = 0f;
            float dist = toTarget.magnitude;
            if (dist <= stopDistance) { hasClickTarget = false; }
            else { desiredDir = toTarget / Mathf.Max(dist, 0.0001f); motion = desiredDir * moveSpeed; }
        }

        // Apply movement
        cc.Move((motion + new Vector3(0f, velocity.y, 0f)) * Time.deltaTime);
        float horizontalSpeed = new Vector3(motion.x, 0f, motion.z).magnitude;
        if (animator) animator.SetFloat("Speed", horizontalSpeed);

        // Cache lastDesiredDir for dodge
        if (desiredDir.sqrMagnitude > 0.0001f) lastDesiredDir = desiredDir;
        else if (hasClickTarget)
        {
            Vector3 toTarget = clickTargetWorld - transform.position; toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f) lastDesiredDir = toTarget.normalized;
        }

        // -------- Rotation rules (fixes circular drift) --------
        bool strafingOnly = Mathf.Abs(moveInput.x) > 0.1f && Mathf.Abs(moveInput.y) < 0.1f;

        if (hasKeyboardInput)
        {
            // Rotate only if there is forward/back input; do NOT rotate on pure A/D strafe
            if (!strafingOnly && desiredDir.sqrMagnitude > 0.0001f)
                RotateTowards(desiredDir);
        }
        else
        {
            // Only face mouse when you're actually in click-to-move mode
            if (hasClickTarget)
            {
                if (TryGetMouseGroundPoint(out var mousePoint))
                {
                    Vector3 faceDir = mousePoint - transform.position; faceDir.y = 0f;
                    if (faceDir.sqrMagnitude > 0.0001f) RotateTowards(faceDir.normalized);
                }
            }
            // else: no rotation while idle (prevents spin/chasing)
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

        // Only hit ground layers; ignore triggers
        if (Physics.Raycast(ray, out RaycastHit hit, faceMouseMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            // Safety: ignore if we somehow hit our own colliders
            if (hit.collider && hit.collider.transform.IsChildOf(transform))
                return false;

            hitPoint = hit.point;
            return true;
        }
        return false;
    }

    void ApplyGravity()
    {
        // Small downward bias when grounded to keep contact
        if (cc.isGrounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
    }

    // ================= INPUT CALLBACKS =================

    // WASD vector (Action: Move, Value/Vector2 with 2D Vector composite)
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (ctx.canceled) moveInput = Vector2.zero;
        else if (ctx.started || ctx.performed) moveInput = ctx.ReadValue<Vector2>();
    }

    // Mouse position (Action: Point, Pass Through, Vector2 -> Mouse/position)
    public void OnPoint(InputAction.CallbackContext ctx)
    {
        if (ctx.performed || ctx.started)
            mouseScreenPos = ctx.ReadValue<Vector2>();
    }

    // Right-click (or your chosen button) to set a move target once
    // (Action: MoveClick, Button -> Mouse/rightButton)
    public void OnMoveClick(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) // fire once when pressed
        {
            if (TryGetMouseGroundPoint(out var p))
            {
                clickTargetWorld = p;
                hasClickTarget = true;
            }
        }
        // Releasing the button does not cancel movement; player will walk to target.
        // If you want to cancel on release: if (ctx.canceled) hasClickTarget = false;
    }

    // Directional dodge in lastDesiredDir (or forward if none)
    public void OnDodge(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || isDodging || dodgeOnCooldown) return;
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

    public void OnPrimaryAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        Debug.Log("Primary Attack!");
    }

    public void OnEmoteWheel(InputAction.CallbackContext ctx)
    {
        if (ctx.started) Debug.Log("Emote Wheel Open");
        if (ctx.canceled) Debug.Log("Emote Wheel Close");
    }
}
