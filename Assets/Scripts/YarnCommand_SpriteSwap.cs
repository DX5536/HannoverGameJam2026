using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Yarn.Unity;

/// <summary>
/// Swaps the enemy's *story* sprite from inside a Yarn script.
///
/// Registers three zero-argument Yarn commands:
///     &lt;&lt;SpriteSwap_IDLE&gt;&gt;
///     &lt;&lt;SpriteSwap_MAD&gt;&gt;
///     &lt;&lt;SpriteSwap_HAPPY&gt;&gt;
///
/// The sprite is read from <see cref="EnemyStats_ScriptableObject.EnemyStorySprite"/>.
/// Instead of remembering array indices, each command maps to a named state
/// (IDLE / MAD / HAPPY) that matches how the sprites are named in 2DSprites.
/// Array layout expected on the ScriptableObject: 0 = idle, 1 = mad, 2 = happy.
///
/// Assign either a <see cref="SpriteRenderer"/> (world space) or a UI
/// <see cref="Image"/> (canvas) as the display target - whichever is set is used.
/// </summary>
public class YarnCommand_SpriteSwap : MonoBehaviour
{
    // Index of each state inside EnemyStorySprite. Change here if the array order ever changes.
    private enum StoryState
    {
        IDLE = 0,
        MAD = 1,
        HAPPY = 2,
    }

    [Tooltip("The enemy stats asset that holds enemyStorySprite.")]
    [SerializeField] private EnemyStats_ScriptableObject enemyStats;

    [Header("Display target (assign one)")]
    [Tooltip("Use this when the enemy is drawn with a SpriteRenderer in the world.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Use this when the enemy portrait is a UI Image on a Canvas.")]
    [SerializeField] private Image uiImage;

    [Tooltip("Optional. If left empty, the first DialogueRunner in the scene is used.")]
    [SerializeField] private DialogueRunner dialogueRunner;

    [Header("Events (hook up SFX / animation here)")]
    public UnityEvent onIdle;
    public UnityEvent onMad;
    public UnityEvent onHappy;

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
            Debug.LogError($"{nameof(YarnCommand_SpriteSwap)}: No DialogueRunner found. Yarn commands were not registered.", this);
            return;
        }

        dialogueRunner.AddCommandHandler("SpriteSwap_IDLE", SpriteSwap_IDLE);
        dialogueRunner.AddCommandHandler("SpriteSwap_MAD", SpriteSwap_MAD);
        dialogueRunner.AddCommandHandler("SpriteSwap_HAPPY", SpriteSwap_HAPPY);
    }

    // --- Yarn commands ---------------------------------------------------

    /// <summary><c>&lt;&lt;SpriteSwap_IDLE&gt;&gt;</c></summary>
    public void SpriteSwap_IDLE()
    {
        Apply(StoryState.IDLE);
        onIdle?.Invoke();
    }

    /// <summary><c>&lt;&lt;SpriteSwap_MAD&gt;&gt;</c></summary>
    public void SpriteSwap_MAD()
    {
        Apply(StoryState.MAD);
        onMad?.Invoke();
    }

    /// <summary><c>&lt;&lt;SpriteSwap_HAPPY&gt;&gt;</c></summary>
    public void SpriteSwap_HAPPY()
    {
        Apply(StoryState.HAPPY);
        onHappy?.Invoke();
    }

    // --- Internal --------------------------------------------------------

    private void Apply(StoryState state)
    {
        if (enemyStats == null)
        {
            Debug.LogWarning($"{nameof(YarnCommand_SpriteSwap)}: No EnemyStats assigned.", this);
            return;
        }

        Sprite[] sprites = enemyStats.EnemyStorySprite;
        int index = (int)state;

        if (sprites == null || index < 0 || index >= sprites.Length)
        {
            Debug.LogWarning($"{nameof(YarnCommand_SpriteSwap)}: enemyStorySprite has no entry for {state} (index {index}).", this);
            return;
        }

        Sprite sprite = sprites[index];

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }

        if (uiImage != null)
        {
            uiImage.sprite = sprite;
        }

        if (spriteRenderer == null && uiImage == null)
        {
            Debug.LogWarning($"{nameof(YarnCommand_SpriteSwap)}: No SpriteRenderer or UI Image assigned to display the sprite.", this);
        }
    }
}
