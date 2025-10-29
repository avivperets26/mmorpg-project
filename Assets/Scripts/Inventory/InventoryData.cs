using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public ItemDefinition def;
    public int x;        // top-left cell
    public int y;
    public bool rotated; // rotate 90° (optional, for later with 'R')
}

public class InventoryData
{
    public int width;
    public int height;
    public bool[,] occupied;
    public List<InventoryItem> items = new();

    public InventoryData(int w, int h)
    {
        width = w; height = h;
        occupied = new bool[w, h];
    }

    public bool CanPlace(ItemDefinition def, int px, int py, bool rotated)
    {
        int w = rotated ? def.height : def.width;
        int h = rotated ? def.width : def.height;
        if (px < 0 || py < 0 || px + w > width || py + h > height) return false;

        for (int x = px; x < px + w; x++)
            for (int y = py; y < py + h; y++)
                if (occupied[x, y]) return false;

        return true;
    }

    public bool Place(InventoryItem it)
    {
        if (!CanPlace(it.def, it.x, it.y, it.rotated)) return false;

        int w = it.rotated ? it.def.height : it.def.width;
        int h = it.rotated ? it.def.width : it.def.height;
        for (int x = it.x; x < it.x + w; x++)
            for (int y = it.y; y < it.y + h; y++)
                occupied[x, y] = true;

        items.Add(it);
        return true;
    }

    public void Remove(InventoryItem it)
    {
        int w = it.rotated ? it.def.height : it.def.width;
        int h = it.rotated ? it.def.width : it.def.height;
        for (int x = it.x; x < it.x + w; x++)
            for (int y = it.y; y < it.y + h; y++)
                occupied[x, y] = false;
        items.Remove(it);
    }
}
