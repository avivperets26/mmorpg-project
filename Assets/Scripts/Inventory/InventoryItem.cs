using System;
using UnityEngine;

[Serializable]
public class InventoryItem
{
    public ItemDefinition def;
    public int x;            // top-left cell X
    public int y;            // top-left cell Y
    public bool rotated;     // if true, Width/Height are swapped

    public int Width => rotated ? def.height : def.width;
    public int Height => rotated ? def.width : def.height;
}
