using UnityEngine;

namespace Game.Items
{
    public static class RarityRules
    {
        // Multipliers applied to base stats (Damage/Defense/etc.). EventItem skips scaling.
        public static float GetStatMultiplier(ItemTier tier) => tier switch
        {
            ItemTier.Common => 1.00f,
            ItemTier.Magical => 1.05f,
            ItemTier.Rare => 1.10f,
            ItemTier.UltraRare => 1.15f,
            ItemTier.Epic => 1.20f,
            ItemTier.Legendary => 1.25f,
            ItemTier.Mythical => 1.32f,
            ItemTier.Godlike => 1.40f,
            ItemTier.EventItem => 1.00f,
            _ => 1f
        };

        public static Color GetLabelColor(ItemTier tier)
        {
            // tweak to match your palette
            return tier switch
            {
                ItemTier.Common => Color.grey,
                ItemTier.Magical => new Color(0.35f, 0.55f, 1f),      // Blue
                ItemTier.Rare => new Color(1f, 0.9f, 0.3f),         // Yellow
                ItemTier.UltraRare => new Color(1f, 0.55f, 0.15f),       // Orange
                ItemTier.Epic => new Color(0.7f, 0.35f, 0.9f),      // Purple
                ItemTier.Legendary => new Color(0.35f, 1f, 0.35f),       // Green
                ItemTier.Mythical => new Color(0.2f, 0.95f, 0.9f),      // Turquoise
                ItemTier.Godlike => new Color(1f, 1f, 1f),             // will animate
                ItemTier.EventItem => Color.white,
                _ => Color.white
            };
        }
    }
}
