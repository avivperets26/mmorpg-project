// Assets/Scripts/Gameplay/Combat/DummyTarget.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// Simple training dummy:
/// - Implements IDamageable
/// - Shakes when hit
/// - Spawns a floating damage popup
/// </summary>
[DisallowMultipleComponent]
public class DummyTarget : MonoBehaviour, IDamageable
{
    [Header("Visuals")]
    [Tooltip("Root transform that should shake. Defaults to this transform.")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float shakeDuration = 0.12f;
    [SerializeField] private float shakeStrength = 0.06f;

    [Header("Damage Popup")]
    [SerializeField] private DamagePopup popupPrefab;
    [SerializeField] private float popupSpawnOffsetY = 1.6f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private Coroutine _shakeRoutine;
    private Vector3 _defaultLocalPos;

    private void Awake()
    {
        if (!visualRoot)
            visualRoot = transform;

        _defaultLocalPos = visualRoot.localPosition;
    }

    public void TakeHit(int amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (amount <= 0) amount = 1;

        if (debugLog)
        {
            Debug.Log($"[DummyTarget] Hit for {amount} at {hitPoint}", this);
        }

        // Shake
        if (_shakeRoutine != null)
            StopCoroutine(_shakeRoutine);

        _shakeRoutine = StartCoroutine(ShakeRoutine());

        // Popup
        SpawnPopup(amount, hitPoint);
    }

    private IEnumerator ShakeRoutine()
    {
        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float damper = 1f - Mathf.Clamp01(t / shakeDuration);

            Vector3 offset = Random.insideUnitSphere * (shakeStrength * damper);
            offset.y *= 0.5f; // reduce vertical wobble

            visualRoot.localPosition = _defaultLocalPos + offset;

            yield return null;
        }

        visualRoot.localPosition = _defaultLocalPos;
        _shakeRoutine = null;
    }

    private void SpawnPopup(int amount, Vector3 hitPoint)
    {
        if (!popupPrefab) return;

        Vector3 spawnPos = hitPoint != Vector3.zero
            ? hitPoint + Vector3.up * 0.2f
            : transform.position + Vector3.up * popupSpawnOffsetY;

        DamagePopup popup = Instantiate(popupPrefab, spawnPos, Quaternion.identity);
        popup.SetValue(amount);
    }
}
