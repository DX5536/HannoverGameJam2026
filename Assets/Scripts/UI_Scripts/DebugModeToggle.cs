using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds a UI Toggle (checkbox) to <see cref="DebugManager"/>. Shows the saved state on
/// enable and flips debug mode when the box is checked/unchecked.
/// </summary>
[RequireComponent(typeof(Toggle))]
public class DebugModeToggle : MonoBehaviour
{
    [Tooltip("The checkbox. Auto-filled from this GameObject if left empty.")]
    [SerializeField] private Toggle toggle;

    private void Awake()
    {
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }
    }

    private void OnEnable()
    {
        if (DebugManager.Instance != null)
        {
            toggle.SetIsOnWithoutNotify(DebugManager.Instance.DebugMode);
        }
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDisable()
    {
        toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool on)
    {
        if (DebugManager.Instance != null)
        {
            DebugManager.Instance.SetDebugMode(on);
        }
        else
        {
            Debug.LogWarning($"{nameof(DebugModeToggle)}: No DebugManager in the scene.", this);
        }
    }
}
