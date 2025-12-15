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
    [SerializeField] private float spinSpeed = 720f;


    [Header("Grounding")]
    [SerializeField] private LayerMask groundMask = -1;

    [Tooltip("Auto snap to ground when spawned/scene-loaded (for manually placed pickups).")]
    [SerializeField] private bool snapToGroundOnEnable = true;

    [Tooltip("How much to float above ground (good for grass).")]
    [SerializeField] private float hoverHeight = 0.05f;

    private bool _hasLanded;
    private Rigidbody _rb;
    private float _bottomOffset;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _bottomOffset = ComputeBottomOffset();
    }

    private void OnEnable()
    {
        // If it's a scene-placed item and no animation has played, snap it.
        if (snapToGroundOnEnable && !_hasLanded)
        {
            transform.position = ComputeGroundedLandingPoint(transform.position) + Vector3.up * hoverHeight;
        }
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
        if (_rb)
        {
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        Vector3 target = ComputeGroundedLandingPoint(to) + Vector3.up * hoverHeight;

        transform.position = from;
        StartCoroutine(JumpRoutine(from, target));
    }

    private Vector3 ComputeGroundedLandingPoint(Vector3 rawTo)
    {
        Vector3 groundPoint = rawTo;
        Ray downRay = new Ray(rawTo + Vector3.up * 2f, Vector3.down);

        if (Physics.Raycast(downRay, out var groundHit, 20f, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundPoint = groundHit.point;
        }

        return groundPoint + Vector3.up * _bottomOffset;
    }

    private IEnumerator JumpRoutine(Vector3 from, Vector3 to)
    {
        Quaternion restRot = transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, jumpDuration);
            float clampedT = Mathf.Clamp01(t);

            float arc = 4f * clampedT * (1f - clampedT);

            Vector3 pos = Vector3.Lerp(from, to, clampedT);
            pos.y += arc * jumpHeight;
            transform.position = pos;

            float flipAngle = clampedT * 360f;
            transform.rotation = restRot * Quaternion.AngleAxis(flipAngle, Vector3.up);

            yield return null;
        }

        transform.position = to;
        transform.rotation = restRot;
        _hasLanded = true;

        if (_rb)
        {
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    private float ComputeBottomOffset()
    {
        var col = GetComponent<Collider>();
        if (col == null) return 0f;

        float bottom = col.bounds.min.y;
        float pivotY = transform.position.y;
        return Mathf.Max(0f, pivotY - bottom);
    }

    // later: pickup logic here
}
