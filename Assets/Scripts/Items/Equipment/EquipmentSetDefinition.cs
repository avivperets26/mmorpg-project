using System.Collections.Generic;
using UnityEngine;

namespace Game.Items
{
    // Define a set and its tier bonuses
    [CreateAssetMenu(menuName = "MMO/Items/Equipment Set", fileName = "EquipmentSet")]
    public class EquipmentSetDefinition : ScriptableObject
    {
        public string setId;             // stable id (e.g., "guardian_set")
        public string displayName;       // "Guardian of the North"
        [TextArea] public string lore;

        [Tooltip("Optional: reference to every item in the set for validation/filtering.")]
        public List<EquipmentItemDefinition> members;

        [Tooltip("Bonuses unlocked by #pieces equipped")]
        public List<SetBonus> bonuses;
    }

    [System.Serializable]
    public class SetBonus
    {
        public int piecesRequired;           // e.g., 2, 4
        [TextArea] public string summary;    // "Gain +10% crit"
        public EquipmentStats bonusStats;    // flat stats to add
        // add proc/effect ids later if needed
    }
}
