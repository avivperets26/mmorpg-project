// Assets/Scripts/Gameplay/Combat/DamagePopup.cs
using UnityEngine;
using TMPro;

/// <summary>
/// World-space floating text:
/// - moves up
/// - faces the camera
/// - fades out
///
/// NOTE:
/// - SetValue(int) is kept for backward compatibility (dummy, etc.).
/// - New helpers:
///     SetEnemyDamage(int)
///     SetPlayerDamage(int)
///     SetMiss()
///     SetXp(int)
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

    [Header("Colors")]
    [Tooltip("Default color for damage done to enemies (player attacks).")]
    [SerializeField] private Color enemyDamageColor = Color.white;

    [Tooltip("Color for damage taken by the player (enemy attacks).")]
    [SerializeField] private Color playerDamageColor = new Color(1f, 0.25f, 0.25f); // red

    [Tooltip("Color for 'Miss' popups.")]
    [SerializeField] private Color missColor = new Color(0.7f, 0.7f, 0.7f);        // grey

    [Tooltip("Color for '+XP' popups.")]
    [SerializeField] private Color xpColor = new Color(0.95f, 0.87f, 0.3f);        // golden-ish

    private float _timeAlive;
    private Color _baseColor;
    private Transform _cam;

    private void Awake()
    {
        if (!text)
            text = GetComponentInChildren<TextMeshPro>();

        if (text)
        {
            _baseColor = text.color;

            // If enemyDamageColor is not set in Inspector, default to current text color.
            if (enemyDamageColor.a <= 0f)
                enemyDamageColor = text.color;
        }

        // Camera might not exist yet in Awake, so we'll also check in Update.
        if (Camera.main)
            _cam = Camera.main.transform;
    }

    // --------------------------------------------------------------------
    // Public API
    // --------------------------------------------------------------------

    /// <summary>
    /// Backwards compatible: used by DummyTarget etc.
    /// Interpreted as "damage dealt to enemy".
    /// </summary>
    public void SetValue(int damage)
    {
        SetEnemyDamage(damage);
    }

    /// <summary>Damage dealt to an enemy (player hitting dummy / mob).</summary>
    public void SetEnemyDamage(int damage)
    {
        Setup(damage.ToString(), enemyDamageColor);
    }

    /// <summary>Damage taken by the player (enemy hits player).</summary>
    public void SetPlayerDamage(int damage)
    {
        Setup(damage.ToString(), playerDamageColor);
    }

    /// <summary>Enemy attack missed the player.</summary>
    public void SetMiss()
    {
        Setup("Miss", missColor);
    }

    /// <summary>XP gained from killing an enemy.</summary>
    public void SetXp(int xpAmount)
    {
        Setup($"+{xpAmount} XP", xpColor);
    }

    // Common internal setup
    private void Setup(string value, Color color)
    {
        if (!text) return;

        text.text = value;
        text.color = color;
        _baseColor = text.color;
        _timeAlive = 0f;

        // Slight random offset so multiple popups don't overlap perfectly
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
