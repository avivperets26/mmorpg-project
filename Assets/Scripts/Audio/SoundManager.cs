using UnityEngine;

public static class SoundManager
{
    public static bool Enabled = true;

    public static void Play2D(string soundId)
    {
        if (!Enabled)
            return;

        // Hook for project-specific audio playback.
    }
}
