using System;
using UnityEngine;

[Serializable]
public class InventoryItem
{
    public ItemDefinition def;
    public int x;            // top-left cell X
    public int y;            // top-left cell Y
    public bool rotated;     // if true, Width/Height are swapped

    // Stack count (1 for non-stackables)
    public int quantity = 1;

    public int Width => rotated ? def.height : def.width;
    public int Height => rotated ? def.width : def.height;
}