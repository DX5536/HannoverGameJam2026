using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Central place for keyboard shortcuts (new Input System).
///
/// - Stop key (default E): raises <see cref="onStopKey"/> - wire it to
///   RouletteStopButton.StopActiveSpin(). Ignored while paused.
/// - Pause key (default Esc): toggles pause, shows/hides <see cref="pausePanel"/>,
///   and freezes time. Raises <see cref="onPause"/> / <see cref="onResume"/>.
///
/// The UnityEvents make it easy to reuse for menus later - just add listeners.
/// </summary>
public class KeyboardKeyManager : MonoBehaviour
{
    [Header("Stop (gameplay)")]
    [Tooltip("Key that triggers the roulette stop.")]
    [SerializeField] private Key stopKey = Key.E;

    [Tooltip("Fired when the stop key is pressed (wire RouletteStopButton.StopActiveSpin here). Ignored while paused.")]
    public UnityEvent onStopKey;

    [Header("Pause")]
    [Tooltip("Key that toggles pause.")]
    [SerializeField] private Key pauseKey = Key.Escape;

    [Tooltip("The pause popup to show/hide. Optional.")]
    [SerializeField] private GameObject pausePanel;

    [Tooltip("Freeze the game (Time.timeScale = 0) while paused.")]
    [SerializeField] private bool freezeTimeWhilePaused = true;

    public UnityEvent onPause;
    public UnityEvent onResume;

    /// <summary>True while the game is paused.</summary>
    public bool IsPaused { get; private set; }

    private void Start()
    {
        // Make sure the popup starts hidden and time is running.
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
        {
            return; // no keyboard connected
        }

        if (kb[pauseKey].wasPressedThisFrame)
        {
            TogglePause();
        }

        // Gameplay keys are ignored while paused.
        if (!IsPaused && kb[stopKey].wasPressedThisFrame)
        {
            onStopKey?.Invoke();
        }
    }

    // --- Pause control (also callable from UI buttons) -------------------

    public void TogglePause()
    {
        if (IsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        if (IsPaused)
        {
            return;
        }
        IsPaused = true;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
        if (freezeTimeWhilePaused)
        {
            Time.timeScale = 0f;
        }

        onPause?.Invoke();
    }

    public void Resume()
    {
        if (!IsPaused)
        {
            return;
        }
        IsPaused = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        if (freezeTimeWhilePaused)
        {
            Time.timeScale = 1f;
        }

        onResume?.Invoke();
    }

    private void OnDisable()
    {
        // Don't leave time frozen if this object is disabled while paused.
        if (IsPaused && freezeTimeWhilePaused)
        {
            Time.timeScale = 1f;
        }
    }
}
