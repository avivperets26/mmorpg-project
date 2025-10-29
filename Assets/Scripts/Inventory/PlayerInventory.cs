using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Grid Size")]
    public int gridWidth = 6;
    public int gridHeight = 6;

    public InventoryData Data { get; private set; }

    private void Awake()
    {
        Data = new InventoryData(gridWidth, gridHeight);
    }

    public bool TryAdd(ItemDefinition def)
    {
        // naive placement scan (top-left to bottom-right)
        for (int y = 0; y < Data.height; y++)
            for (int x = 0; x < Data.width; x++)
            {
                var candidate = new InventoryItem { def = def, x = x, y = y, rotated = false };
                if (Data.Place(candidate)) return true;
            }
        return false;
    }

    public void Remove(InventoryItem it) => Data.Remove(it);
}
