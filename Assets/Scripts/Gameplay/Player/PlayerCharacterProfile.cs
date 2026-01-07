using System;

[Serializable]
public class PlayerCharacterProfile
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public string profileId;
    public string playerName;
    public PlayerClass playerClass;
    public byte[] customizationBytes;
    public PlayerProgressionData progression;

    public static PlayerCharacterProfile CreateNew(
        string name,
        PlayerClass playerClass,
        byte[] customizationBytes)
    {
        return new PlayerCharacterProfile
        {
            version = CurrentVersion,
            profileId = Guid.NewGuid().ToString("N"),
            playerName = name,
            playerClass = playerClass,
            customizationBytes = customizationBytes,
            progression = PlayerClassDefaults.GetDefaults(playerClass)
        };
    }
}

[Serializable]
public struct PlayerProgressionData
{
    public int level;
    public int strength;
    public int dexterity;
    public int vitality;
    public int energy;
    public int availableStatPoints;
    public int currentXp;
    public int coins;
}
