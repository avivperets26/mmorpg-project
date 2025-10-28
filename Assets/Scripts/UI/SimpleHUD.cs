using UnityEngine;
using UnityEngine.UI;

public class SimpleHUD : MonoBehaviour
{
    public PlayerStats target;
    public Text label; // add a UI Text (or TextMeshProUGUI and adjust type)

    void Update()
    {
        if (!target || !label) return;
        label.text = $"Speed: {target.GetEffectiveMoveSpeed():0.0}  Boots: {(target.BootsEquipped ? "Yes" : "No")}";
    }
}
