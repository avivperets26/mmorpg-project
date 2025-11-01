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
            // Warm
            return tier switch
            {
                ItemTier.Common => new Color(0.88f, 0.88f, 0.88f),  // light grey/white
                ItemTier.Magical => new Color(0.45f, 0.65f, 1.00f),  // blue
                ItemTier.Rare => new Color(1.00f, 0.85f, 0.35f),  // gold-yellow
                ItemTier.UltraRare => new Color(1.00f, 0.60f, 0.20f),  // orange
                ItemTier.Epic => new Color(0.70f, 0.40f, 0.95f),  // purple
                ItemTier.Legendary => new Color(0.35f, 1.00f, 0.45f),  // green (D2-style)
                ItemTier.Mythical => new Color(0.20f, 0.95f, 0.90f),  // turquoise
                ItemTier.Godlike => Color.white,                     // (animate/glow later)
                ItemTier.EventItem => Color.white,
                _ => Color.white
            };
        }
    }
}
