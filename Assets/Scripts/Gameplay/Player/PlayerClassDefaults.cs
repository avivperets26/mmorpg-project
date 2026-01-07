public static class PlayerClassDefaults
{
    public static PlayerProgressionData GetDefaults(PlayerClass playerClass)
    {
        // Starting values requested for the current flow.
        // Later, per-class tuning can be expanded here.
        return playerClass switch
        {
            PlayerClass.Knight => new PlayerProgressionData
            {
                level = 1,
                strength = 0,
                dexterity = 0,
                vitality = 0,
                energy = 0,
                availableStatPoints = 10,
                currentXp = 0,
                coins = 0
            },
            _ => new PlayerProgressionData
            {
                level = 1,
                strength = 0,
                dexterity = 0,
                vitality = 0,
                energy = 0,
                availableStatPoints = 10,
                currentXp = 0,
                coins = 0
            }
        };
    }
}
