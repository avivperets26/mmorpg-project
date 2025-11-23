// Assets\Scripts\Gameplay\Items\ItemDropManager.cs
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class ItemDropManager : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform worldRoot;       // e.g. "Interactable Objects"
    [SerializeField] private WorldItem worldItemPrefab; // generic fallback (cube debug)

    [Header("Layers")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask worldItemMask;   // layer for dropped items

    [Header("Placement")]
    [SerializeField] private float searchRadius = 1.2f; // how far from hit point we can offset
    [SerializeField] private float checkRadius = 0.35f; // collision radius for overlap
    [SerializeField] private int maxPlacementTries = 10;
    [SerializeField] private float spawnHeightOffset = 0.5f; // where jump starts

    private void Reset()
    {
        mainCamera = Camera.main;
        worldRoot = GameObject.Find("Interactable Objects")?.transform;
    }

    /// <summary>
    /// Called from the inventory drag controller when the drag ends over the world.
    /// Drops the given item stack into the world. Returns true on success.
    /// </summary>
    public bool TryDropItemFromScreenPos(
        ItemDefinition def,
        int stack,
        Vector2 screenPos)
    {
        if (def == null || stack <= 0) return false;

        // do not drop if pointer is still over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        if (mainCamera == null)
            mainCamera = Camera.main;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit, 500f, groundMask, QueryTriggerInteraction.Ignore))
            return false;

        Vector3 targetCenter = hit.point;
        if (!FindFreeSpot(targetCenter, out var finalPos))
        {
            // even if we couldn't find a free spot, we can still use the hit point
            finalPos = targetCenter;
        }

        // spawn a bit above the ground and let the WorldItem animate down
        Vector3 from = finalPos + Vector3.up * spawnHeightOffset;

        // 🔑 Choose prefab:
        //  1) per-item worldPrefab on the ItemDefinition (your nice pickup prefab)
        //  2) fallback generic worldItemPrefab (debug cube)
        GameObject prefabToSpawn = null;

        if (def.worldPrefab != null)
        {
            prefabToSpawn = def.worldPrefab;
        }
        else if (worldItemPrefab != null)
        {
            prefabToSpawn = worldItemPrefab.gameObject;
        }

        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[ItemDropManager] No world prefab available for drop (ItemDefinition.worldPrefab is null and worldItemPrefab is not set).");
            return false;
        }

        // Instantiate under worldRoot (Interactable Objects) if assigned
        var parent = worldRoot != null ? worldRoot : null;
        GameObject go = Instantiate(prefabToSpawn, parent);

        // Ensure there is a WorldItem component to drive animation + metadata
        var worldItem = go.GetComponent<WorldItem>();
        if (worldItem == null)
        {
            worldItem = go.AddComponent<WorldItem>();
        }

        worldItem.Init(def, stack);
        worldItem.PlaySpawnAnimation(from, finalPos);

        return true;
    }

    private bool FindFreeSpot(Vector3 center, out Vector3 result)
    {
        // first try the exact center
        if (!Physics.CheckSphere(center, checkRadius, worldItemMask, QueryTriggerInteraction.Ignore))
        {
            result = center;
            return true;
        }

        // then search nearby in a disc
        for (int i = 0; i < maxPlacementTries; i++)
        {
            Vector2 circle = Random.insideUnitCircle * searchRadius;
            Vector3 candidate = center + new Vector3(circle.x, 0f, circle.y);

            if (!Physics.CheckSphere(candidate, checkRadius, worldItemMask, QueryTriggerInteraction.Ignore))
            {
                result = candidate;
                return true;
            }
        }

        // failed to find anything free
        result = center;
        return false;
    }
}
