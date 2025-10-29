// Assets/Scripts/Inventory/PlayerInventory.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Grid Size")]
    public int gridWidth = 6;
    public int gridHeight = 6;

    public InventoryData Data { get; private set; }

    // NEW: simple list of placed items (top-left cell of each)
    private readonly List<InventoryItem> _items = new();
    public IReadOnlyList<InventoryItem> Items => _items;

    // NEW: notify UI to refresh after mutations
    public event Action Changed;

    private void Awake()
    {
        Data = new InventoryData(gridWidth, gridHeight);
    }

    public bool TryAdd(ItemDefinition def)
    {
        Debug.Log($"[Inventory] Trying to add item: {def.displayName}");

        for (int y = 0; y < Data.height; y++)
            for (int x = 0; x < Data.width; x++)
            {
                var candidate = new InventoryItem { def = def, x = x, y = y, rotated = false };
                if (Data.Place(candidate))
                {
                    _items.Add(candidate);
                    Changed?.Invoke();          // <— tell UI to refresh
                    return true;
                }
            }

        return false;
    }

    public void Remove(InventoryItem it)
    {
        Data.Remove(it);
        _items.Remove(it);
        Changed?.Invoke();                  // <— tell UI to refresh
    }
}
