using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventoryData
{
    public readonly int width;
    public readonly int height;

    // Occupancy map: null = empty; non-null = reference to occupying InventoryItem
    private readonly InventoryItem[,] _cells;

    public InventoryData(int w, int h)
    {
        width = Mathf.Max(1, w);
        height = Mathf.Max(1, h);
        _cells = new InventoryItem[width, height];
    }

    /// <summary>Checks bounds & empty cells for the item's rectangle at its (x,y).</summary>
    public bool CanPlace(InventoryItem it)
    {
        if (it == null || it.def == null) return false;

        // Bounds
        if (it.x < 0 || it.y < 0) return false;
        if (it.x + it.Width > width) return false;
        if (it.y + it.Height > height) return false;

        // Empty cells
        for (int yy = 0; yy < it.Height; yy++)
            for (int xx = 0; xx < it.Width; xx++)
                if (_cells[it.x + xx, it.y + yy] != null)
                    return false;

        return true;
    }

    /// <summary>Writes the item into the grid if possible.</summary>
    public bool Place(InventoryItem it)
    {
        if (!CanPlace(it)) return false;

        for (int yy = 0; yy < it.Height; yy++)
            for (int xx = 0; xx < it.Width; xx++)
                _cells[it.x + xx, it.y + yy] = it;

        return true;
    }

    /// <summary>
    /// Clears all cells that reference this item (by reference), regardless of
    /// the item's current x/y/size. This is safer during drags.
    /// </summary>
    public void Remove(InventoryItem it)
    {
        if (it == null) return;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (_cells[x, y] == it)
                    _cells[x, y] = null;
    }

    /// <summary>Same as Remove but returns true if anything was actually cleared.</summary>
    public bool TryRemove(InventoryItem it)
    {
        if (it == null) return false;

        int cleared = 0;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (_cells[x, y] == it)
                {
                    _cells[x, y] = null;
                    cleared++;
                }

        return cleared > 0;
    }


    /// <summary>Returns the item occupying a given cell, or null if empty.</summary>
    public InventoryItem GetAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return _cells[x, y];
    }

    /// <summary>Enumerates all cells occupied by a given item.</summary>
    public IEnumerable<Vector2Int> CellsOf(InventoryItem it)
    {
        if (it == null) yield break;
        for (int yy = 0; yy < it.Height; yy++)
            for (int xx = 0; xx < it.Width; xx++)
                yield return new Vector2Int(it.x + xx, it.y + yy);
    }
}
