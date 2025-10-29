using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InventoryUI : MonoBehaviour
{
    [Header("Wiring")]
    public GameObject rootPanel;
    public Button closeButton;

    [Header("Behavior (Legacy Only)")]
    public KeyCode toggleKey = KeyCode.I;
#if ENABLE_INPUT_SYSTEM
    [Header("Behavior (New Input System)")]
    [SerializeField] private Key toggleKeyNew = Key.I;
#endif

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && kb[toggleKeyNew].wasPressedThisFrame)
            Toggle();
#else
        if (Input.GetKeyDown(toggleKey))
            Toggle();
#endif
    }

    public void Toggle()
    {
        if (!rootPanel) return;
        rootPanel.SetActive(!rootPanel.activeSelf);
    }

    public void Show() { if (rootPanel) rootPanel.SetActive(true); }
    public void Hide() { if (rootPanel) rootPanel.SetActive(false); }
}
