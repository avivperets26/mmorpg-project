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

    /// <summary>Clears the item's cells from the grid (if present).</summary>
    public void Remove(InventoryItem it)
    {
        if (it == null) return;

        // Only clear cells that actually reference this item
        for (int yy = 0; yy < it.Height; yy++)
            for (int xx = 0; xx < it.Width; xx++)
            {
                int gx = it.x + xx;
                int gy = it.y + yy;
                if (gx >= 0 && gx < width && gy >= 0 && gy < height)
                {
                    if (_cells[gx, gy] == it)
                        _cells[gx, gy] = null;
                }
            }
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
