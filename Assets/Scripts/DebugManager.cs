using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent debug-mode switch. When ON, every GameObject tagged "DEBUG" is enabled;
/// when OFF, they're disabled. Re-applies automatically on every scene load, and the
/// setting is remembered across scenes and sessions (PlayerPrefs).
///
/// IMPORTANT: author your DEBUG objects ACTIVE in the scene. Unity can only find active
/// objects by tag, so this manager turns them OFF when debug is off - it can't find ones
/// that start disabled to turn them back on.
///
/// Wire a checkbox to <see cref="SetDebugMode"/> (see DebugModeToggle).
/// </summary>
public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance { get; private set; }

    [Tooltip("Tag used to mark debug-only objects.")]
    [SerializeField] private string debugTag = "DEBUG";

    private const string PrefKey = "debug_mode";

    /// <summary>Whether debug mode is currently on.</summary>
    public bool DebugMode { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        DebugMode = PlayerPrefs.GetInt(PrefKey, 0) == 1;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ApplyDebugState(); // apply to the scene we started in
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyDebugState();
    }

    /// <summary>Turns debug mode on/off, saves it, and applies it to the current scene.</summary>
    public void SetDebugMode(bool on)
    {
        DebugMode = on;
        PlayerPrefs.SetInt(PrefKey, on ? 1 : 0);
        ApplyDebugState();
    }

    /// <summary>Enables/disables all active objects tagged <see cref="debugTag"/> per <see cref="DebugMode"/>.</summary>
    public void ApplyDebugState()
    {
        GameObject[] tagged;
        try
        {
            tagged = GameObject.FindGameObjectsWithTag(debugTag);
        }
        catch (UnityException)
        {
            Debug.LogWarning($"{nameof(DebugManager)}: Tag '{debugTag}' is not defined. Add it in the Tag Manager.", this);
            return;
        }

        foreach (GameObject go in tagged)
        {
            go.SetActive(DebugMode);
        }
    }
}
