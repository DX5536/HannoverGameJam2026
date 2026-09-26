using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Swaps a combatant's *battle* sprite. Unlike <see cref="YarnCommand_SpriteSwap"/>
/// this is NOT registered with Yarn - call the public methods from Unity
/// (Animation Events, UnityEvents on buttons, other scripts, etc.).
///
/// Works for the enemy or the player: assign ONE stats asset - either an
/// <see cref="EnemyStats_ScriptableObject"/> (reads enemyBattleSprite) or a
/// <see cref="PlayerStats_ScriptableObject"/> (reads playerBattleSprite). Put one of
/// these components on the enemy object and another on the player object.
///
/// Each method maps to a named state (IDLE / ATTACK / HIT) instead of an array
/// index. Array layout expected on either asset: 0 = idle, 1 = attack, 2 = hit.
///
/// Assign either a <see cref="SpriteRenderer"/> (world space) or a UI
/// <see cref="Image"/> (canvas) as the display target - whichever is set is used.
/// </summary>
public class Battle_SpriteSwap : MonoBehaviour
{
    // Index of each state inside the battle-sprite array. Change here if the order ever changes.
    private enum BattleState
    {
        IDLE = 0,
        ATTACK = 1,
        HIT = 2,
    }

    [Header("Stats (assign ONE)")]
    [Tooltip("Assign this when this component drives the ENEMY (reads enemyBattleSprite).")]
    [SerializeField] private EnemyStats_ScriptableObject enemyStats;

    [Tooltip("Assign this when this component drives the PLAYER (reads playerBattleSprite).")]
    [SerializeField] private PlayerStats_ScriptableObject playerStats;

    [Header("Display target (assign one)")]
    [Tooltip("Use this when the combatant is drawn with a SpriteRenderer in the world.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Use this when the combatant is a UI Image on a Canvas.")]
    [SerializeField] private Image uiImage;

    [Header("Events (hook up SFX / animation here)")]
    public UnityEvent onIdle;
    public UnityEvent onAttack;
    public UnityEvent onHit;

    // --- Public methods (call from Unity / UnityEvents) ------------------

    public void SpriteSwap_IDLE()
    {
        Apply(BattleState.IDLE);
        onIdle?.Invoke();
    }

    public void SpriteSwap_ATTACK()
    {
        Apply(BattleState.ATTACK);
        onAttack?.Invoke();
    }

    public void SpriteSwap_HIT()
    {
        Apply(BattleState.HIT);
        onHit?.Invoke();
    }

    // --- Internal --------------------------------------------------------

    private void Apply(BattleState state)
    {
        // Use whichever stats asset is assigned (player takes priority if both are set).
        Sprite[] sprites;
        string sourceName;
        if (playerStats != null)
        {
            sprites = playerStats.PlayerBattleSprite;
            sourceName = "playerBattleSprite";
        }
        else if (enemyStats != null)
        {
            sprites = enemyStats.EnemyBattleSprite;
            sourceName = "enemyBattleSprite";
        }
        else
        {
            Debug.LogWarning($"{nameof(Battle_SpriteSwap)}: No EnemyStats or PlayerStats assigned.", this);
            return;
        }

        int index = (int)state;

        if (sprites == null || index < 0 || index >= sprites.Length)
        {
            Debug.LogWarning($"{nameof(Battle_SpriteSwap)}: {sourceName} has no entry for {state} (index {index}).", this);
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
            Debug.LogWarning($"{nameof(Battle_SpriteSwap)}: No SpriteRenderer or UI Image assigned to display the sprite.", this);
        }
    }
}
