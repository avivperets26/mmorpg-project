using System;

namespace Game.Enemies
{
    [Flags]
    public enum SpecialDamageType
    {
        None = 0,
        Fire = 1 << 0,
        Freeze = 1 << 1,
        Poison = 1 << 2,
        Bleed = 1 << 3,
        Decay = 1 << 4
        // add more later easily
    }
}
