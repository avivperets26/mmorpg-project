// Assets/Scripts/Enemies/Runtime/EnemyDebugDamage.cs
using UnityEngine;
using UnityEngine.InputSystem; // New Input System

namespace Game.Enemies
{
    /// <summary>
    /// Simple helper to test EnemyHealth: press a key to deal damage.
    /// Uses the new Input System (Keyboard.current) + debug logs.
    /// </summary>
    public class EnemyDebugDamage : MonoBehaviour
    {
        public EnemyHealth health;
        public float damagePerHit = 5f;

        // New Input System key enums (visible in Inspector)
        public Key damageKey = Key.Digit1;
        public Key resetKey = Key.Digit2;

        private void Reset()
        {
            if (!health) health = GetComponent<EnemyHealth>();
        }

        private void Update()
        {
            if (!health) return;

            var kb = Keyboard.current;
            if (kb == null)
            {
                // No keyboard detected (very rare on desktop, but just in case)
                return;
            }

            if (kb[damageKey].wasPressedThisFrame)
            {
                Debug.Log($"{name}: Damage key pressed ({damageKey}), dealing {damagePerHit} dmg.");
                health.TakeDamage(damagePerHit);
            }

            if (kb[resetKey].wasPressedThisFrame)
            {
                Debug.Log($"{name}: Reset key pressed ({resetKey}), resetting HP.");
                health.ResetHealth();
            }
        }
    }
}
