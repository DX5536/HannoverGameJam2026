using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

/// <summary>
/// Loads scenes from UI buttons or from Yarn (for a story -> battle -> story flow).
///
/// Yarn commands (registered if a DialogueRunner is in the scene):
///     &lt;&lt;LoadScene "BattleScene"&gt;&gt;
///     &lt;&lt;ReloadScene&gt;&gt;
///
/// The same methods are public, so buttons can call LoadScene(string) directly.
/// </summary>
public class SceneSwitcher : MonoBehaviour
{
    [Tooltip("Optional. If left empty, the first DialogueRunner in the scene is used (for the Yarn commands).")]
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
        if (dialogueRunner != null)
        {
            dialogueRunner.AddCommandHandler<string>("LoadScene", LoadScene);
            dialogueRunner.AddCommandHandler("ReloadScene", ReloadCurrentScene);
        }
    }

    // --- Public methods (UI buttons + Yarn) ------------------------------

    /// <summary>Loads a scene by name. <c>&lt;&lt;LoadScene "BattleScene"&gt;&gt;</c></summary>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"{nameof(SceneSwitcher)}: LoadScene was given an empty scene name.", this);
            return;
        }

        // Make sure time is running (in case we left via a paused screen).
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>Loads a scene by its build-settings index (handy for Next/Previous buttons).</summary>
    public void LoadSceneByIndex(int buildIndex)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(buildIndex);
    }

    /// <summary>Reloads the current scene. <c>&lt;&lt;ReloadScene&gt;&gt;</c></summary>
    public void ReloadCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
