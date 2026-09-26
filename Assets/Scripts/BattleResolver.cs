using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Owns the battle rules for the two-wheel setup: it starts each phase's spins, waits
/// for BOTH wheels to land a move, then compares Rock/Paper/Scissors and applies HP.
///
/// The two <see cref="RouletteSpin"/> wheels are purely visual move-producers; all the
/// win/tie/lose logic, HP changes and outcome events live here.
///
/// Phase methods (wire these to BattleSequencer's onPlayerTurn / onEnemyTurn):
///   PlayerAttackPhase() - player attacks: player wheel random-rolls, enemy wheel is player-stopped.
///   EnemyAttackPhase()  - enemy attacks: enemy wheel random-rolls, player wheel is player-stopped.
/// The single STOP button stops whichever wheel is player-spinning (see RouletteStopButton);
/// when both wheels have landed, the round resolves automatically.
///
/// Outcome (from the attacker's point of view):
///   attacker wins  -> Hit     (defender loses 1 HP)   -> onAttackHit
///   attacker ties  -> Block   (nobody loses HP)       -> onAttackBlock
///   attacker loses -> Counter (attacker loses 1 HP)   -> onAttackCounter
/// When any HP reaches 0, onHpReachedZero fires.
/// </summary>
public class BattleResolver : MonoBehaviour
{
    private enum AttackResult { Win, Tie, Lose }

    [Header("Wheels")]
    [SerializeField] private RouletteSpin playerRoulette;
    [SerializeField] private RouletteSpin enemyRoulette;

    [Header("Stats")]
    [SerializeField] private PlayerStats_ScriptableObject playerStats;
    [SerializeField] private EnemyStats_ScriptableObject enemyStats;

    [Header("Attacker")]
    [Tooltip("Who is attacking THIS round. Set automatically by the phase methods.")]
    [SerializeField] private RouletteSpin.Combatant currentAttacker = RouletteSpin.Combatant.Player;

    [Header("Outcome events (any attacker - shared SFX / HP bar)")]
    [Tooltip("Attacker landed a hit (defender lost HP). Fires no matter who attacked.")]
    public UnityEvent onAttackHit;
    [Tooltip("Attack was blocked (nobody lost HP). Fires no matter who attacked.")]
    public UnityEvent onAttackBlock;
    [Tooltip("Attacker was countered (attacker lost HP). Fires no matter who attacked.")]
    public UnityEvent onAttackCounter;

    [Header("Outcome events (PLAYER attacking - wire player's anims)")]
    public UnityEvent onPlayerAttackHit;
    public UnityEvent onPlayerAttackBlock;
    public UnityEvent onPlayerAttackCounter;

    [Header("Outcome events (ENEMY attacking - wire enemy's anims)")]
    public UnityEvent onEnemyAttackHit;
    public UnityEvent onEnemyAttackBlock;
    public UnityEvent onEnemyAttackCounter;

    [Header("Other events")]
    [Tooltip("Fires whenever a combatant's HP reaches 0 (either side). Good for shared 'fight over' reactions.")]
    public UnityEvent onHpReachedZero;

    [Tooltip("Player won: the ENEMY's HP hit 0. Wire the player-win animation here.")]
    public UnityEvent onPlayerWin;

    [Tooltip("Enemy won: the PLAYER's HP hit 0. Wire the enemy-win animation here.")]
    public UnityEvent onEnemyWin;

    [Tooltip("Debug: fired by DebugInstantWin().")]
    public UnityEvent onInstantWin_DEBUG;

    [Tooltip("Debug: fired by DebugInstantLose().")]
    public UnityEvent onInstantLose_DEBUG;

    /// <summary>True once a combatant has been defeated this battle.</summary>
    public bool BattleOver { get; private set; }

    /// <summary>Who won (valid only when <see cref="BattleOver"/> is true).</summary>
    public RouletteSpin.Combatant Winner { get; private set; }

    private void OnEnable()
    {
        if (playerRoulette != null) playerRoulette.Landed += OnWheelLanded;
        if (enemyRoulette != null) enemyRoulette.Landed += OnWheelLanded;
    }

    private void OnDisable()
    {
        if (playerRoulette != null) playerRoulette.Landed -= OnWheelLanded;
        if (enemyRoulette != null) enemyRoulette.Landed -= OnWheelLanded;
    }

    // --- Phases (wire to BattleSequencer turn events) --------------------

    /// <summary>Player attacks: player wheel random-rolls, enemy wheel is player-stopped.</summary>
    public void PlayerAttackPhase()
    {
        currentAttacker = RouletteSpin.Combatant.Player;
        ResetRound();
        if (playerRoulette != null) playerRoulette.RandomSpin();
        if (enemyRoulette != null) enemyRoulette.StartSpin();
    }

    /// <summary>Enemy attacks: enemy wheel random-rolls, player wheel is player-stopped.</summary>
    public void EnemyAttackPhase()
    {
        currentAttacker = RouletteSpin.Combatant.Enemy;
        ResetRound();
        if (enemyRoulette != null) enemyRoulette.RandomSpin();
        if (playerRoulette != null) playerRoulette.StartSpin();
    }

    private void ResetRound()
    {
        if (playerRoulette != null) playerRoulette.ResetMove();
        if (enemyRoulette != null) enemyRoulette.ResetMove();
    }

    // --- Resolution ------------------------------------------------------

    private void OnWheelLanded(RouletteSpin _)
    {
        if (playerRoulette == null || enemyRoulette == null)
        {
            return;
        }
        if (!playerRoulette.HasMove || !enemyRoulette.HasMove)
        {
            return; // still waiting on the other wheel
        }

        Resolve();
    }

    private void Resolve()
    {
        RouletteSpin.Rps playerMove = playerRoulette.LastMove;
        RouletteSpin.Rps enemyMove = enemyRoulette.LastMove;

        // Record each side's result from its own point of view.
        AttackResult playerResult = Compare(playerMove, enemyMove);
        SetAction(RouletteSpin.Combatant.Player, playerResult);
        SetAction(RouletteSpin.Combatant.Enemy, Invert(playerResult));

        // Outcome from the attacker's point of view.
        RouletteSpin.Combatant defender = Opponent(currentAttacker);
        AttackResult attackerResult = currentAttacker == RouletteSpin.Combatant.Player
            ? playerResult
            : Invert(playerResult);

        bool playerAttacking = currentAttacker == RouletteSpin.Combatant.Player;

        switch (attackerResult)
        {
            case AttackResult.Win:
                ModifyHp(defender, -1); // defender takes the hit
                onAttackHit?.Invoke();
                (playerAttacking ? onPlayerAttackHit : onEnemyAttackHit)?.Invoke();
                break;
            case AttackResult.Lose:
                ModifyHp(currentAttacker, -1); // attacker gets countered
                onAttackCounter?.Invoke();
                (playerAttacking ? onPlayerAttackCounter : onEnemyAttackCounter)?.Invoke();
                break;
            case AttackResult.Tie:
            default:
                onAttackBlock?.Invoke();
                (playerAttacking ? onPlayerAttackBlock : onEnemyAttackBlock)?.Invoke();
                break;
        }

        ResetRound();
    }

    // --- Rock / Paper / Scissors -----------------------------------------

    /// <summary>Compares two moves from <paramref name="a"/>'s point of view.</summary>
    private static AttackResult Compare(RouletteSpin.Rps a, RouletteSpin.Rps b)
    {
        if (a == b)
        {
            return AttackResult.Tie;
        }

        bool aWins =
            (a == RouletteSpin.Rps.Rock && b == RouletteSpin.Rps.Scissors) ||
            (a == RouletteSpin.Rps.Paper && b == RouletteSpin.Rps.Rock) ||
            (a == RouletteSpin.Rps.Scissors && b == RouletteSpin.Rps.Paper);

        return aWins ? AttackResult.Win : AttackResult.Lose;
    }

    private static AttackResult Invert(AttackResult result)
    {
        switch (result)
        {
            case AttackResult.Win: return AttackResult.Lose;
            case AttackResult.Lose: return AttackResult.Win;
            default: return AttackResult.Tie;
        }
    }

    private static RouletteSpin.Combatant Opponent(RouletteSpin.Combatant c)
    {
        return c == RouletteSpin.Combatant.Player ? RouletteSpin.Combatant.Enemy : RouletteSpin.Combatant.Player;
    }

    private void SetAction(RouletteSpin.Combatant who, AttackResult result)
    {
        string value = result.ToString(); // "Win" / "Tie" / "Lose"
        if (who == RouletteSpin.Combatant.Player && playerStats != null) playerStats.CurrentAction = value;
        if (who == RouletteSpin.Combatant.Enemy && enemyStats != null) enemyStats.CurrentAction = value;
    }

    // --- HP --------------------------------------------------------------

    private void ModifyHp(RouletteSpin.Combatant who, int delta)
    {
        switch (who)
        {
            case RouletteSpin.Combatant.Player:
                if (playerStats == null)
                {
                    Debug.LogWarning($"{nameof(BattleResolver)}: No PlayerStats assigned.", this);
                    return;
                }
                playerStats.CurrentHP += delta;
                if (playerStats.CurrentHP <= 0)
                {
                    playerStats.CurrentHP = 0;
                    HandleDefeat(RouletteSpin.Combatant.Player);
                }
                break;

            case RouletteSpin.Combatant.Enemy:
                if (enemyStats == null)
                {
                    Debug.LogWarning($"{nameof(BattleResolver)}: No EnemyStats assigned.", this);
                    return;
                }
                enemyStats.CurrentHP += delta;
                if (enemyStats.CurrentHP <= 0)
                {
                    enemyStats.CurrentHP = 0;
                    HandleDefeat(RouletteSpin.Combatant.Enemy);
                }
                break;
        }
    }

    /// <summary>Records the winner and fires the shared + winner-specific events.</summary>
    private void HandleDefeat(RouletteSpin.Combatant loser)
    {
        BattleOver = true;
        Winner = Opponent(loser);

        onHpReachedZero?.Invoke();

        if (Winner == RouletteSpin.Combatant.Player)
        {
            onPlayerWin?.Invoke();
        }
        else
        {
            onEnemyWin?.Invoke();
        }
    }

    // --- Debug -----------------------------------------------------------

    /// <summary>DEBUG: instantly wins by dropping the enemy's HP to 0.</summary>
    [ContextMenu("Debug/Instant Win")]
    public void DebugInstantWin()
    {
        if (enemyStats != null)
        {
            enemyStats.CurrentHP = 0;
            HandleDefeat(RouletteSpin.Combatant.Enemy); // player wins
        }
        else
        {
            Debug.LogWarning($"{nameof(BattleResolver)}: DebugInstantWin has no EnemyStats assigned.", this);
        }

        onInstantWin_DEBUG?.Invoke();
    }

    /// <summary>DEBUG: instantly loses by dropping the player's HP to 0.</summary>
    [ContextMenu("Debug/Instant Lose")]
    public void DebugInstantLose()
    {
        if (playerStats != null)
        {
            playerStats.CurrentHP = 0;
            HandleDefeat(RouletteSpin.Combatant.Player); // enemy wins
        }
        else
        {
            Debug.LogWarning($"{nameof(BattleResolver)}: DebugInstantLose has no PlayerStats assigned.", this);
        }

        onInstantLose_DEBUG?.Invoke();
    }
}
