// Assets/Scripts/Gameplay/Player/PlayerWallet.cs
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerWallet : MonoBehaviour
{
    [Header("Coins")]
    [Min(0)][SerializeField] private int coins;

    public int Coins => coins;

    public event Action OnCoinsChanged;

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins += amount;
        OnCoinsChanged?.Invoke();
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (coins < amount) return false;

        coins -= amount;
        OnCoinsChanged?.Invoke();
        return true;
    }

    public void SetCoins(int amount)
    {
        coins = Mathf.Max(0, amount);
        OnCoinsChanged?.Invoke();
    }
}
