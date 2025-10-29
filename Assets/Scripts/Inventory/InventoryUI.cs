using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InventoryUI : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("The whole inventory panel (the Image with your big background).")]
    public GameObject rootPanel;

    [Tooltip("Optional: your X button. If assigned, it auto-wires to Hide().")]
    public Button closeButton;

    [Header("Behavior")]
    public KeyCode toggleKey = KeyCode.I;

    void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        // Start closed
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    void Update()
    {
        // Support both old & new input systems
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
                    Toggle();
#endif
        if (Input.GetKeyDown(toggleKey))
            Toggle();
    }

    public void Toggle()
    {
        if (!rootPanel) return;
        rootPanel.SetActive(!rootPanel.activeSelf);
    }

    public void Show()
    {
        if (!rootPanel) return;
        rootPanel.SetActive(true);
    }

    public void Hide()
    {
        if (!rootPanel) return;
        rootPanel.SetActive(false);
    }
}
