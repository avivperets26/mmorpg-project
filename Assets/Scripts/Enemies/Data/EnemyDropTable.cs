using System.Collections.Generic;
using UnityEngine;
using Game.Items;

namespace Game.Enemies
{
    [CreateAssetMenu(
        fileName = "EnemyDropTable",
        menuName = "Game/Enemies/Drop Table",
        order = 1)]
    public class EnemyDropTable : ScriptableObject
    {
        [System.Serializable]
        public class DropEntry
        {
            public ItemDefinition item;
            [Range(0f, 1f)]
            public float dropChance = 0.2f;
            public Vector2Int quantityRange = new Vector2Int(1, 1);
        }

        [Header("Coins")]
        public Vector2Int coinsRange = new Vector2Int(1, 5);

        [Header("Item Drops")]
        public List<DropEntry> drops = new List<DropEntry>();
    }
}
