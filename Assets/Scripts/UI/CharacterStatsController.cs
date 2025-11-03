using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CharacterStatsController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private StatAllocationUI statUI;   // drag CharacterStatPanel root (has StatAllocationUI)
    [SerializeField] private Button closeButton;        // the top-right X button (optional)

    [Header("Behavior")]
    [SerializeField] private bool startHidden = true;
    [SerializeField] private Key toggleKey = Key.C;     // press C to open/close

    private void Awake()
    {
        if (closeButton != null)
        {
            // X should cancel (refund) and close, so points never get lost
            closeButton.onClick.AddListener(() =>
            {
                if (statUI) statUI.CancelAndCloseUI();
            });
        }
    }

    private void Start()
    {
        if (!statUI) return;

        if (startHidden) statUI.Close();
        else statUI.Open();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();
    }

    private void Update()
    {
        if (!statUI) return;

        if (Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            // statUI has Open/Close, so create a simple toggle:
            var root = statUI.gameObject; // CharacterStatPanel
            bool isActive = root.activeSelf; // after fade-in, this is true
            if (isActive) statUI.CancelAndCloseUI(); // closing should refund pending
            else statUI.Open();
        }
    }
}
