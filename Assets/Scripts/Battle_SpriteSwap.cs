using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Swaps the enemy's *battle* sprite. Unlike <see cref="YarnCommand_SpriteSwap"/>
/// this is NOT registered with Yarn - call the public methods from Unity
/// (Animation Events, UnityEvents on buttons, other scripts, etc.).
///
/// The sprite is read from <see cref="EnemyStats_ScriptableObject.EnemyBattleSprite"/>.
/// Each method maps to a named state (IDLE / ATTACK / HIT) instead of an array
/// index. Array layout expected on the ScriptableObject: 0 = idle, 1 = attack, 2 = hit.
///
/// Assign either a <see cref="SpriteRenderer"/> (world space) or a UI
/// <see cref="Image"/> (canvas) as the display target - whichever is set is used.
/// </summary>
public class Battle_SpriteSwap : MonoBehaviour
{
    // Index of each state inside EnemyBattleSprite. Change here if the array order ever changes.
    private enum BattleState
    {
        IDLE = 0,
        ATTACK = 1,
        HIT = 2,
    }

    [Tooltip("The enemy stats asset that holds enemyBattleSprite.")]
    [SerializeField] private EnemyStats_ScriptableObject enemyStats;

    [Header("Display target (assign one)")]
    [Tooltip("Use this when the enemy is drawn with a SpriteRenderer in the world.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Use this when the enemy is a UI Image on a Canvas.")]
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
        if (enemyStats == null)
        {
            Debug.LogWarning($"{nameof(Battle_SpriteSwap)}: No EnemyStats assigned.", this);
            return;
        }

        Sprite[] sprites = enemyStats.EnemyBattleSprite;
        int index = (int)state;

        if (sprites == null || index < 0 || index >= sprites.Length)
        {
            Debug.LogWarning($"{nameof(Battle_SpriteSwap)}: enemyBattleSprite has no entry for {state} (index {index}).", this);
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
