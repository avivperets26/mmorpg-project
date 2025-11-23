// Assets/Scripts/Gameplay/Items/WorldItem.cs
using System.Collections;
using UnityEngine;
using Game.Items;
using Game.Items.Definitions;

[DisallowMultipleComponent]
public class WorldItem : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private ItemDefinition itemDefinition;
    [SerializeField] private int stackCount;

    [Header("Animation")]
    [SerializeField] private float jumpDuration = 0.45f;
    [SerializeField] private float jumpHeight = 0.9f;
    [SerializeField] private float spinSpeed = 720f;    // for the flip in the air

    [Header("Grounding")]
    [Tooltip("Layer(s) considered ground for final landing alignment.")]
    [SerializeField] private LayerMask groundMask = -1;   // default: Everything

    private bool _hasLanded;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Init(ItemDefinition def, int stack)
    {
        itemDefinition = def;
        stackCount = Mathf.Max(1, stack);

        // If there’s an ItemWorldPickup script, let it update its label/color.
        var pickup = GetComponent<ItemWorldPickup>();
        if (pickup != null)
        {
            // method to add on ItemWorldPickup – see section 2 below
            pickup.SetRuntimeDefinition(def, stackCount);
        }
    }

    /// <summary>
    /// Plays a little jump+flip from "from" towards "to".
    /// We internally adjust "to" so the collider rests on the ground, even on slopes.
    /// </summary>
    public void PlaySpawnAnimation(Vector3 from, Vector3 to)
    {
        // Disable physics while we drive the motion by script
        if (_rb)
        {
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        // Recompute the *true* landing point:
        // 1) raycast down to the ground
        // 2) add collider half-height so the bottom touches the surface
        Vector3 target = ComputeGroundedLandingPoint(to);

        transform.position = from;
        StartCoroutine(JumpRoutine(from, target));
    }

    private Vector3 ComputeGroundedLandingPoint(Vector3 rawTo)
    {
        // Step 1 – find terrain/ground below the target point
        Vector3 groundPoint = rawTo;
        Ray downRay = new Ray(rawTo + Vector3.up * 2f, Vector3.down);
        if (Physics.Raycast(downRay, out var groundHit, 5f, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundPoint = groundHit.point;
        }

        // Step 2 – offset by collider half height so we don't sink
        float halfHeight = 0f;
        var col = GetComponent<Collider>();
        if (col != null)
        {
            // bounds.extents is in world space and already accounts for scale
            halfHeight = col.bounds.extents.y;
        }

        return groundPoint + Vector3.up * halfHeight;
    }

    private IEnumerator JumpRoutine(Vector3 from, Vector3 to)
    {
        // Prefab’s pose is our “resting on the ground” orientation
        Quaternion restRot = transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, jumpDuration);
            float clampedT = Mathf.Clamp01(t);

            // simple 0→1→0 parabola
            float arc = 4f * clampedT * (1f - clampedT);

            // move along the arc
            Vector3 pos = Vector3.Lerp(from, to, clampedT);
            pos.y += arc * jumpHeight;
            transform.position = pos;

            // one flip in the air (feels ARPG-ish)
            float flipAngle = clampedT * 360f;
            transform.rotation = restRot * Quaternion.AngleAxis(flipAngle, Vector3.up);

            yield return null;
        }

        // Final landing – snap to exact target, in prefab’s resting pose
        transform.position = to;
        transform.rotation = restRot;
        _hasLanded = true;

        // Re-enable physics but without an extra bounce
        if (_rb)
        {
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    // later: pickup logic here
}
