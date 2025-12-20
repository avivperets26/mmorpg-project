using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class CoinCounterUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private TMP_Text coinsText;

    private void Awake()
    {
        if (!coinsText)
            coinsText = GetComponent<TMP_Text>();

        if (!wallet)
        {
#if UNITY_2023_1_OR_NEWER
            wallet = FindFirstObjectByType<PlayerWallet>();
#else
            wallet = FindObjectOfType<PlayerWallet>();
#endif
        }
    }

    private void OnEnable()
    {
        if (wallet != null)
            wallet.OnCoinsChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (wallet != null)
            wallet.OnCoinsChanged -= Refresh;
    }

    private void Refresh()
    {
        if (!wallet || !coinsText) return;
        coinsText.text = wallet.Coins.ToString();
    }
}
