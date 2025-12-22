using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.VFX
{
    [DisallowMultipleComponent]
    public class CoinSplashVFXSpawner : MonoBehaviour
    {
        private sealed class CoroutineHost : MonoBehaviour { }

        private const string DefaultPrefabPath = "Assets/VFX/Coins/Splash/VFX_CoinSplash_Small.prefab";

        [Header("VFX")]
        [SerializeField] private ParticleSystem splashPrefab;

        [Min(0f)]
        public float delayBeforeSpawnPile = 0.15f;

        [Tooltip("Extra Y offset for the VFX spawn position.")]
        public float additionalYOffset = 0f;

        [Tooltip("Extra Y offset for the coin pile spawn position (use negative to lower).")]
        public float pileYOffset = 0f;

        [Tooltip("If true, adjusts the spawned pile so its bottom touches the ground position.")]
        public bool snapPileToGround = true;

        public void PlaySplashAndSpawnPile(Vector3 position, GameObject coinPilePrefab, Transform parent = null)
        {
            PlaySplashAndSpawnPile(position, coinPilePrefab, parent, null);
        }

        public void PlaySplashAndSpawnPile(
            Vector3 position,
            GameObject coinPilePrefab,
            Transform parent,
            System.Action<GameObject> onPileSpawned)
        {
            if (coinPilePrefab == null) return;
            var host = CreateHost();
            host.StartCoroutine(PlayRoutine(
                host,
                position,
                coinPilePrefab,
                parent,
                onPileSpawned,
                splashPrefab,
                delayBeforeSpawnPile,
                additionalYOffset,
                pileYOffset,
                snapPileToGround));
        }

        private static IEnumerator PlayRoutine(
            CoroutineHost host,
            Vector3 position,
            GameObject coinPilePrefab,
            Transform parent,
            System.Action<GameObject> onPileSpawned,
            ParticleSystem splashPrefab,
            float delayBeforeSpawnPile,
            float additionalYOffset,
            float pileYOffset,
            bool snapPileToGround)
        {
            Vector3 vfxPos = position + Vector3.up * additionalYOffset;

            if (splashPrefab != null)
            {
                ParticleSystem vfx = Instantiate(splashPrefab, vfxPos, Quaternion.identity);
                vfx.Play(true);

                float lifetime = GetSystemLifetime(vfx);
                if (vfx.main.stopAction != ParticleSystemStopAction.Destroy)
                {
                    Destroy(vfx.gameObject, lifetime);
                }
            }

            if (delayBeforeSpawnPile > 0f)
                yield return new WaitForSeconds(delayBeforeSpawnPile);

            Vector3 pilePos = position + Vector3.up * pileYOffset;
            GameObject pile = Instantiate(coinPilePrefab, pilePos, Quaternion.identity, parent);
            if (snapPileToGround)
                SnapPileToGround(pile, position.y + pileYOffset);
            onPileSpawned?.Invoke(pile);

            if (host != null)
                Destroy(host.gameObject);
        }

        private static CoroutineHost CreateHost()
        {
            var go = new GameObject("CoinSplashVFXRunner");
            return go.AddComponent<CoroutineHost>();
        }

        private static void SnapPileToGround(GameObject pile, float targetGroundY)
        {
            if (pile == null) return;

            var col = pile.GetComponentInChildren<Collider>();
            if (col != null)
            {
                float delta = targetGroundY - col.bounds.min.y;
                pile.transform.position += new Vector3(0f, delta, 0f);
                return;
            }

            var rend = pile.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                float delta = targetGroundY - rend.bounds.min.y;
                pile.transform.position += new Vector3(0f, delta, 0f);
            }
        }

        private static float GetSystemLifetime(ParticleSystem system)
        {
            var main = system.main;
            float lifetime = Mathf.Max(0.1f, main.duration);

            if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                lifetime += main.startLifetime.constantMax;
            else if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                lifetime += main.startLifetime.constant;
            else
                lifetime += main.startLifetime.constantMax;

            if (system.subEmitters.subEmittersCount > 0)
                lifetime += 0.5f;

            return lifetime;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (splashPrefab != null) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPrefabPath);
            if (prefab == null) return;

            var particle = prefab.GetComponent<ParticleSystem>();
            if (particle == null)
                particle = prefab.GetComponentInChildren<ParticleSystem>();

            if (particle != null)
            {
                splashPrefab = particle;
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
