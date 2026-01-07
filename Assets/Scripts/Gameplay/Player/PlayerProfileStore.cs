public static class PlayerProfileStore
{
    private static PlayerCharacterProfile _active;

    public static bool HasActiveProfile => _active != null;

    public static PlayerCharacterProfile Active => _active;

    public static bool TryGetActive(out PlayerCharacterProfile profile)
    {
        profile = _active;
        return profile != null;
    }

    public static void SetActive(PlayerCharacterProfile profile)
    {
        _active = profile;
    }

    public static void Clear()
    {
        _active = null;
    }
}
