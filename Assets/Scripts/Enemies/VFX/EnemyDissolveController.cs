// Assets/Scripts/Enemies/VFX/EnemyDissolveController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Listens to EnemyHealth.OnDeath and plays a dissolve animation
    /// using a Dissolve FX shader.
    ///
    /// Flow:
    /// - Wait for a small delay (so death anim can play)
    /// - Swap materials to a dissolve-capable template
    /// - Animate dissolve parameter 0 -> 1
    /// - Optionally Destroy() the enemy at the end
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyDissolveController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private EnemyHealth health;
        [SerializeField] private Animator animator;

        [Tooltip("Root containing the mesh renderers to dissolve. If null, uses this transform.")]
        [SerializeField] private Transform renderRoot;

        [Header("Dissolve Material")]
        [Tooltip("Material that uses a Dissolve FX shader. A unique instance is created per enemy.")]
        [SerializeField] private Material dissolveMaterialTemplate;

        [Tooltip("Shader property name that controls dissolve amount (usually something like _DissolveAmount).")]
        [SerializeField] private string dissolveAmountProperty = "_DissolveAmount";

        [Header("Timing")]
        [Tooltip("Delay (in seconds) after death before starting dissolve. Should be >= death anim length.")]
        [SerializeField] private float delayBeforeDissolve = 0.8f;

        [Tooltip("Duration (in seconds) of the dissolve animation.")]
        [SerializeField] private float dissolveDuration = 1.2f;

        [Header("Cleanup")]
        [Tooltip("Destroy the whole enemy GameObject when dissolve finishes.")]
        [SerializeField] private bool destroyOnFinish = true;

        [Tooltip("Extra delay before Destroy(), after dissolve completes.")]
        [SerializeField] private float destroyDelay = 0.1f;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<Material[]> _originalMaterials = new List<Material[]>();
        private readonly List<Material[]> _dissolveMaterials = new List<Material[]>();

        private int _dissolvePropId;
        private bool _isDissolving;

        // --------------------------------------------------------------------
        // Unity
        // --------------------------------------------------------------------

        private void Reset()
        {
            if (!health) health = GetComponent<EnemyHealth>();
            if (!animator) animator = GetComponentInChildren<Animator>();
            if (!renderRoot) renderRoot = transform;
        }

        private void Awake()
        {
            if (!health) health = GetComponent<EnemyHealth>();
            if (!animator) animator = GetComponentInChildren<Animator>();
            if (!renderRoot) renderRoot = transform;

            _dissolvePropId = Shader.PropertyToID(dissolveAmountProperty);
        }

        private void OnEnable()
        {
            if (health != null)
                health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            if (health != null)
                health.OnDeath -= HandleDeath;
        }

        // --------------------------------------------------------------------
        // Death -> dissolve
        // --------------------------------------------------------------------

        private void HandleDeath(EnemyHealth _)
        {
            if (_isDissolving) return;
            if (!isActiveAndEnabled) return;

            StartCoroutine(DissolveRoutine());
        }

        private IEnumerator DissolveRoutine()
        {
            _isDissolving = true;

            // Let the death animation play
            if (delayBeforeDissolve > 0f)
                yield return new WaitForSeconds(delayBeforeDissolve);

            CacheRenderers();
            CreateDissolveMaterials();
            SwapToDissolveMaterials();
            SetDissolveAmount(0f);

            float t = 0f;
            while (t < dissolveDuration)
            {
                t += Time.deltaTime;
                float normalized = Mathf.Clamp01(t / Mathf.Max(0.0001f, dissolveDuration));
                SetDissolveAmount(normalized);
                yield return null;
            }

            SetDissolveAmount(1f);

            if (destroyOnFinish)
            {
                if (destroyDelay > 0f)
                    yield return new WaitForSeconds(destroyDelay);

                Destroy(gameObject);
            }
        }

        // --------------------------------------------------------------------
        // Material / renderer handling
        // --------------------------------------------------------------------

        private void CacheRenderers()
        {
            _renderers.Clear();
            _originalMaterials.Clear();
            _dissolveMaterials.Clear();

            if (!renderRoot)
                renderRoot = transform;

            // You can restrict this to SkinnedMeshRenderer only if you like
            var renderers = renderRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
            foreach (var r in renderers)
            {
                // Skip things like particle systems if needed (optional check)
                if (r is ParticleSystemRenderer) continue;

                _renderers.Add(r);
                _originalMaterials.Add(r.sharedMaterials);
            }
        }

        private void CreateDissolveMaterials()
        {
            if (dissolveMaterialTemplate == null)
            {
                Debug.LogWarning($"[{nameof(EnemyDissolveController)}] No dissolveMaterialTemplate assigned on {name}. Dissolve is disabled.");
                return;
            }

            for (int i = 0; i < _renderers.Count; i++)
            {
                var originalSet = _originalMaterials[i];
                var newSet = new Material[originalSet.Length];

                for (int j = 0; j < originalSet.Length; j++)
                {
                    // Create a unique material instance per slot
                    var mat = new Material(dissolveMaterialTemplate);

                    // Optional: copy main texture from original
                    // Try "_BaseMap" first (URP), then mainTexture
                    if (originalSet[j] != null)
                    {
                        // Try URP _BaseMap first
                        var baseMap = originalSet[j].GetTexture("_BaseMap");
                        if (baseMap != null)
                        {
                            mat.SetTexture("_BaseMap", baseMap);
                            mat.SetTexture("_MainTex", baseMap);   // for this shader
                        }
                        else if (originalSet[j].mainTexture != null)
                        {
                            mat.mainTexture = originalSet[j].mainTexture;
                            mat.SetTexture("_MainTex", originalSet[j].mainTexture);
                        }
                    }

                    // Ensure initial dissolve is 0 (fully visible)
                    if (_dissolvePropId != 0)
                        mat.SetFloat(_dissolvePropId, 0f);

                    newSet[j] = mat;
                }

                _dissolveMaterials.Add(newSet);
            }
        }

        private void SwapToDissolveMaterials()
        {
            if (dissolveMaterialTemplate == null || _dissolveMaterials.Count == 0)
                return;

            for (int i = 0; i < _renderers.Count; i++)
            {
                _renderers[i].materials = _dissolveMaterials[i];
            }
        }

        private void SetDissolveAmount(float value)
        {
            if (_dissolveMaterials.Count == 0)
                return;

            for (int i = 0; i < _dissolveMaterials.Count; i++)
            {
                var matArray = _dissolveMaterials[i];
                for (int j = 0; j < matArray.Length; j++)
                {
                    var mat = matArray[j];
                    if (mat == null) continue;

                    if (_dissolvePropId != 0)
                        mat.SetFloat(_dissolvePropId, value);
                }
            }
        }
    }
}
