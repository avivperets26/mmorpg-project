// Assets/Scripts/Inventory/PlayerInventory.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player-facing API for storing items in a grid and notifying UI.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("Grid Size")]
    public int gridWidth = 6;
    public int gridHeight = 6;

    public InventoryData Data { get; private set; }

    // Top-left placed instances
    private readonly List<InventoryItem> _items = new();
    public IReadOnlyList<InventoryItem> Items => _items;

    /// <summary>
    /// Raised after any add/remove so UI can refresh.
    /// </summary>
    public event Action Changed;

    private void Awake()
    {
        Data = new InventoryData(gridWidth, gridHeight);
    }

    /// <summary>
    /// Tries to place the item scanning left-to-right, top-to-bottom.
    /// </summary>
    public bool TryAdd(ItemDefinition def)
    {
        if (def == null)
        {
            Debug.LogWarning("[Inventory] TryAdd called with null ItemDefinition");
            return false;
        }

        Debug.Log($"[Inventory] Trying to add item: {def.displayName}");

        // Try both orientations (not rotated, then rotated) to maximize fit.
        for (int pass = 0; pass < 2; pass++)
        {
            bool rotated = pass == 1;

            for (int y = 0; y < Data.height; y++)
            {
                for (int x = 0; x < Data.width; x++)
                {
                    var candidate = new InventoryItem
                    {
                        def = def,
                        x = x,
                        y = y,
                        rotated = rotated
                    };

                    if (Data.Place(candidate))
                    {
                        _items.Add(candidate);
                        Debug.Log($"[Inventory] Added {def.displayName} at ({x},{y}) rot:{rotated}");
                        Changed?.Invoke();
                        return true;
                    }
                }
            }
        }

        Debug.LogWarning("[Inventory] No free space for item");
        return false;
    }

    /// <summary>
    /// Removes the specific placed instance.
    /// </summary>
    public void Remove(InventoryItem it)
    {
        if (it == null) return;

        Data.Remove(it);
        _items.Remove(it);
        Changed?.Invoke();
    }

    /// <summary>
    /// Convenience: removes the first placed instance of a given definition.
    /// </summary>
    public bool RemoveFirst(ItemDefinition def)
    {
        var idx = _items.FindIndex(i => i.def == def);
        if (idx < 0) return false;

        var it = _items[idx];
        Data.Remove(it);
        _items.RemoveAt(idx);
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Clears everything (useful for testing).
    /// </summary>
    public void ClearAll()
    {
        foreach (var it in _items)
            Data.Remove(it);
        _items.Clear();
        Changed?.Invoke();
    }
}
