// Assets/Scripts/Gameplay/Combat/DamagePopup.cs
using UnityEngine;
using TMPro;

/// <summary>
/// World-space damage number:
/// - moves up
/// - faces the camera
/// - fades out
/// </summary>
public class DamagePopup : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private TextMeshPro text;

    [Header("Motion")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.4f;
    [SerializeField] private float randomHorizontalOffset = 0.2f;

    private float _timeAlive;
    private Color _baseColor;
    private Transform _cam;

    private void Awake()
    {
        if (!text)
            text = GetComponentInChildren<TextMeshPro>();

        if (text)
            _baseColor = text.color;

        // Camera might not exist yet in Awake, so we'll also check in Update.
        if (Camera.main)
            _cam = Camera.main.transform;
    }

    public void SetValue(int damage)
    {
        if (!text) return;

        text.text = damage.ToString();
        _baseColor = text.color;
        _timeAlive = 0f;

        // Slight random offset so multiple hits don't overlap perfectly
        Vector3 pos = transform.position;
        pos.x += Random.Range(-randomHorizontalOffset, randomHorizontalOffset);
        pos.z += Random.Range(-randomHorizontalOffset, randomHorizontalOffset);
        transform.position = pos;
    }

    private void Update()
    {
        // Lazy-grab camera if it appeared after Awake
        if (!_cam && Camera.main)
            _cam = Camera.main.transform;

        _timeAlive += Time.deltaTime;

        // Move upwards
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // Always face the camera
        if (_cam)
            transform.forward = _cam.forward;

        // Fade out near the end
        if (text)
        {
            float fadeStart = lifetime - fadeOutDuration;
            float t = Mathf.Clamp01((_timeAlive - fadeStart) / fadeOutDuration);

            Color c = _baseColor;
            c.a = 1f - t;
            text.color = c;
        }

        if (_timeAlive >= lifetime)
            Destroy(gameObject);
    }
}
