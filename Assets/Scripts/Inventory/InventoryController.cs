// Assets/Scripts/Inventory/InventoryController.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InventoryController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private GameObject rootPanel; // e.g., InventoryPanel GameObject
    [SerializeField] private Button closeButton;   // your X button

    [Header("Behavior")]
    [SerializeField] private bool startHidden = true;
    [SerializeField] private Key toggleKey = Key.I; // New Input System key

    private void Awake()
    {
        if (startHidden && rootPanel != null)
            rootPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);
    }

    private void Update()
    {
        // Toggle with keyboard
        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
            Toggle();
    }

    public void Show()
    {
        if (rootPanel != null) rootPanel.SetActive(true);
    }

    public void Hide()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    public void Toggle()
    {
        if (rootPanel != null) rootPanel.SetActive(!rootPanel.activeSelf);
    }
}
