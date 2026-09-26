using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Exposes the <see cref="AudioManager"/> music controls to Yarn Spinner.
///
/// Registers these zero-argument commands so they can be called from a .yarn file:
///     &lt;&lt;PlayStandardBGM&gt;&gt;
///     &lt;&lt;PlayCombatBGM&gt;&gt;
///     &lt;&lt;StopBGM&gt;&gt;
///
/// Put one of these in each scene that has a DialogueRunner (AudioManager itself can
/// persist across scenes; the command registration must live with the per-scene runner).
/// </summary>
public class YarnCommand_Audio : MonoBehaviour
{
    [Tooltip("Optional. If left empty, the first DialogueRunner in the scene is used.")]
    [SerializeField] private DialogueRunner dialogueRunner;

    private void Awake()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        }
    }

    private void Start()
    {
        if (dialogueRunner == null)
        {
            Debug.LogError($"{nameof(YarnCommand_Audio)}: No DialogueRunner found. Yarn commands were not registered.", this);
            return;
        }

        dialogueRunner.AddCommandHandler("PlayStandardBGM", PlayStandardBGM);
        dialogueRunner.AddCommandHandler("PlayCombatBGM", PlayCombatBGM);
        dialogueRunner.AddCommandHandler("StopBGM", StopBGM);
    }

    // --- Yarn commands (also callable from UnityEvents) ------------------

    /// <summary><c>&lt;&lt;PlayStandardBGM&gt;&gt;</c></summary>
    public void PlayStandardBGM()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayStandardBGM();
        }
        else
        {
            Debug.LogWarning($"{nameof(YarnCommand_Audio)}: No AudioManager in the scene.", this);
        }
    }

    /// <summary><c>&lt;&lt;PlayCombatBGM&gt;&gt;</c></summary>
    public void PlayCombatBGM()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCombatBGM();
        }
        else
        {
            Debug.LogWarning($"{nameof(YarnCommand_Audio)}: No AudioManager in the scene.", this);
        }
    }

    /// <summary><c>&lt;&lt;StopBGM&gt;&gt;</c></summary>
    public void StopBGM()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }
        else
        {
            Debug.LogWarning($"{nameof(YarnCommand_Audio)}: No AudioManager in the scene.", this);
        }
    }
}
