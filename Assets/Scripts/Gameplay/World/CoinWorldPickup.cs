using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class CoinWorldPickup : MonoBehaviour, IInteractable
{
    [Header("Gold (rolled once on spawn)")]
    [Min(0)][SerializeField] private int minGold = 1;
    [Min(1)][SerializeField] private int maxGold = 10;

    [Header("Pickup")]
    [Min(0.1f)][SerializeField] private float pickupRadius = 2.0f;

    [Header("UI (optional)")]
    [SerializeField] private TMP_Text label;

    public Transform Transform => transform;
    public float MaxUseDistance => pickupRadius;

    private int _amount;
    private bool _rolled;
    private bool _consumed;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        RollOnce();
    }
    public void SetRange(int min, int max)
    {
        minGold = Mathf.Max(0, min);
        maxGold = Mathf.Max(minGold, max);

        // Force re-roll with the new range
        _rolled = false;
        RollOnce();
    }

    private void RollOnce()
    {
        if (_rolled) return;

        int min = Mathf.Max(0, minGold);
        int max = Mathf.Max(min, maxGold);

        _amount = Random.Range(min, max + 1);
        _rolled = true;

        if (label)
            label.text = $"{_amount} Gold";
    }

    public void Interact(GameObject interactor)
    {
        if (_consumed) return;

        var wallet =
            interactor.GetComponent<PlayerWallet>() ??
            interactor.GetComponentInParent<PlayerWallet>() ??
#if UNITY_2023_1_OR_NEWER
            FindFirstObjectByType<PlayerWallet>();
#else
            FindObjectOfType<PlayerWallet>();
#endif

        if (!wallet)
        {
            Debug.LogWarning("[CoinPickup] No PlayerWallet found!");
            return;
        }

        wallet.AddCoins(_amount);

        _consumed = true;
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.25f);
        Gizmos.DrawSphere(transform.position, pickupRadius);
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 1f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
#endif
}
