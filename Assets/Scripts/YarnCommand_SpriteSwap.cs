using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

/// <summary>
/// Swaps a combatant's *story* sprite from inside a Yarn script.
///
/// Registers three zero-argument Yarn commands:
///     &lt;&lt;StorySpriteSwap_IDLE&gt;&gt;
///     &lt;&lt;StorySpriteSwap_MAD&gt;&gt;
///     &lt;&lt;StorySpriteSwap_HAPPY&gt;&gt;
///
/// Works for the enemy or the player: assign ONE stats asset - either an
/// <see cref="EnemyStats_ScriptableObject"/> (reads enemyStorySprite) or a
/// <see cref="PlayerStats_ScriptableObject"/> (reads playerStorySprite).
/// Instead of remembering array indices, each command maps to a named state
/// (IDLE / MAD / HAPPY) that matches how the sprites are named in 2DSprites.
/// Array layout expected on either asset: 0 = idle, 1 = mad, 2 = happy.
///
/// Assign either a <see cref="SpriteRenderer"/> (world space) or a UI
/// <see cref="Image"/> (canvas) as the display target - whichever is set is used.
/// </summary>
public class YarnCommand_SpriteSwap : MonoBehaviour
{
    // Index of each state inside the story-sprite array. Change here if the order ever changes.
    private enum StoryState
    {
        IDLE = 0,
        MAD = 1,
        HAPPY = 2,
    }

    [Header("Stats (assign ONE)")]
    [Tooltip("Assign this when this component drives the ENEMY portrait (reads enemyStorySprite).")]
    [SerializeField] private EnemyStats_ScriptableObject enemyStats;

    [Tooltip("Assign this when this component drives the PLAYER portrait (reads playerStorySprite).")]
    [SerializeField] private PlayerStats_ScriptableObject playerStats;

    [Header("Display target (assign one)")]
    [Tooltip("Use this when the portrait is drawn with a SpriteRenderer in the world.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Use this when the portrait is a UI Image on a Canvas.")]
    [SerializeField] private Image uiImage;

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
            Debug.LogError($"{nameof(YarnCommand_SpriteSwap)}: No DialogueRunner found. Yarn commands were not registered.", this);
            return;
        }

        dialogueRunner.AddCommandHandler("StorySpriteSwap_IDLE", StorySpriteSwap_IDLE);
        dialogueRunner.AddCommandHandler("StorySpriteSwap_MAD", StorySpriteSwap_MAD);
        dialogueRunner.AddCommandHandler("StorySpriteSwap_HAPPY", StorySpriteSwap_HAPPY);
    }

    // --- Yarn commands ---------------------------------------------------

    /// <summary><c>&lt;&lt;StorySpriteSwap_IDLE&gt;&gt;</c></summary>
    public void StorySpriteSwap_IDLE() => Apply(StoryState.IDLE);

    /// <summary><c>&lt;&lt;StorySpriteSwap_MAD&gt;&gt;</c></summary>
    public void StorySpriteSwap_MAD() => Apply(StoryState.MAD);

    /// <summary><c>&lt;&lt;StorySpriteSwap_HAPPY&gt;&gt;</c></summary>
    public void StorySpriteSwap_HAPPY() => Apply(StoryState.HAPPY);

    // --- Internal --------------------------------------------------------

    private void Apply(StoryState state)
    {
        // Use whichever stats asset is assigned (player takes priority if both are set).
        Sprite[] sprites;
        string sourceName;
        if (playerStats != null)
        {
            sprites = playerStats.PlayerStorySprite;
            sourceName = "playerStorySprite";
        }
        else if (enemyStats != null)
        {
            sprites = enemyStats.EnemyStorySprite;
            sourceName = "enemyStorySprite";
        }
        else
        {
            Debug.LogWarning($"{nameof(YarnCommand_SpriteSwap)}: No EnemyStats or PlayerStats assigned.", this);
            return;
        }

        int index = (int)state;

        if (sprites == null || index < 0 || index >= sprites.Length)
        {
            Debug.LogWarning($"{nameof(YarnCommand_SpriteSwap)}: {sourceName} has no entry for {state} (index {index}).", this);
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
