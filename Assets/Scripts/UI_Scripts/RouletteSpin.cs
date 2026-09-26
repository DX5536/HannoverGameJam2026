using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Drives the roulette: a doughnut wheel with a spinning arrow in the middle.
///
/// - The wheel is rendered by <see cref="RouletteWheelGraphic"/>. Each segment's
///   colour is editable in the Inspector, and its size (share of the ring) either
///   comes from a manual weight or from the Rock/Paper/Scissors values on the
///   chosen stats asset (see <see cref="weightsFromStats"/>).
/// - The arrow is just a Transform that is rotated around Z. Which segment it points
///   at is worked out purely from its angle (no 2D collider), so the hit only ever
///   triggers when a method is called - never once per animation frame.
/// - Rotation speed = <see cref="baseRotationSpeed"/> + EnemyStats.spinSpeed - PlayerStats.slowdownSpeed.
///
/// Each wheel segment is a Rock / Paper / Scissors move. Every round BOTH sides get
/// a move: one side is a fast uncontrollable RandomSpin, the other is a spin the
/// player times with StopSpin. The move is simply the slice the arrow lands on, so
/// a wheel weighted by an enemy's preferences (e.g. 50% Rock) makes both the random
/// roll and the "where do I stop it" skill check follow those odds.
///
/// Typical flow (see the convenience methods):
///   Player attacks:  RandomRollPlayer()  -> PlayerStopsEnemyWheel() -> player hits StopSpin()
///   Enemy attacks:   RandomRollEnemy()   -> PlayerStopsOwnWheel()   -> player hits StopSpin()
/// The second (player-timed) spin is the one that resolves the round.
///
/// Resolving compares the attacker's move against the defender's, from the attacker's
/// point of view, to pick the physical outcome:
///     attacker wins  -> Hit     (defender loses 1 HP)
///     attacker ties  -> Block   (nobody loses HP)
///     attacker loses -> Counter (attacker loses 1 HP)
/// Each of AttackHit / AttackBlock / AttackCounter has its own UnityEvent, and
/// when any HP reaches 0, <see cref="onHpReachedZero"/> fires.
///
/// Both stats assets also record the round for inspection: CurrentSpinResult
/// ("Rock"/"Paper"/"Scissors") is written as each side's move lands, and
/// CurrentAction ("Win"/"Tie"/"Lose") is written when the round resolves.
/// </summary>
public class RouletteSpin : MonoBehaviour
{
    public enum Combatant { Player, Enemy }

    /// <summary>What a wheel segment represents. The physical result (hit/block/counter)
    /// is derived by comparing the attacker's move against the defender's.</summary>
    public enum Rps { Rock, Paper, Scissors }

    private enum AttackResult { Win, Tie, Lose }

    [System.Serializable]
    public struct Segment
    {
        [Tooltip("The Rock / Paper / Scissors move this slice represents.")]
        public Rps rps;

        [Tooltip("Segment colour on the doughnut (editable here - no sprite needed).")]
        public Color color;

        [Tooltip("Manual share of the wheel. Ignored when 'Weights From Stats' is on.")]
        [Min(0f)] public float weight;
    }

    [Header("Stats")]
    [Tooltip("Optional. Provides spinSpeed and (optionally) the RPS split + enemy HP.")]
    [SerializeField] private EnemyStats_ScriptableObject enemyStats;

    [Tooltip("Optional. Provides slowdownSpeed and (optionally) the RPS split + player HP.")]
    [SerializeField] private PlayerStats_ScriptableObject playerStats;

    [Header("Wheel")]
    [Tooltip("Segments in CLOCKWISE order starting from the top.")]
    [SerializeField]
    private Segment[] segments = new Segment[]
    {
        new Segment { rps = Rps.Rock,     color = new Color(0.90f, 0.20f, 0.25f), weight = 1f },
        new Segment { rps = Rps.Paper,    color = new Color(0.10f, 0.70f, 0.45f), weight = 1f },
        new Segment { rps = Rps.Scissors, color = new Color(0.30f, 0.20f, 0.95f), weight = 1f },
    };

    [Tooltip("If on, the segment weights are pulled from the Rock/Paper/Scissors values of 'Stats For Weights' (segment order = Rock, Paper, Scissors).")]
    [SerializeField] private bool weightsFromStats = false;

    [Tooltip("Which stats asset supplies the RPS split when 'Weights From Stats' is on.")]
    [SerializeField] private Combatant statsForWeights = Combatant.Enemy;

    [Tooltip("Optional. The doughnut renderer. If left empty it is looked up in children.")]
    [SerializeField] private RouletteWheelGraphic wheel;

    [Header("Arrow")]
    [Tooltip("The arrow transform. Rotated around Z; its art should point UP at 0 rotation.")]
    [SerializeField] private Transform arrow;

    [Tooltip("Standard rotation speed in degrees/second (360 = one full turn per second), before enemy/player modifiers.")]
    [SerializeField] private float baseRotationSpeed = 360f;

    [Tooltip("Rotation speed is never allowed below this (degrees/second).")]
    [SerializeField] private float minRotationSpeed = 0f;

    [Tooltip("Start spinning as soon as the object is enabled.")]
    [SerializeField] private bool spinOnStart = false;

    [Header("Random Spin")]
    [Tooltip("How long RandomSpin() spins for, in seconds.")]
    [SerializeField] private float randomSpinDuration = 0.3f;

    [Tooltip("How many full turns the arrow makes before settling on its random landing spot. More turns = faster blur.")]
    [SerializeField] private int randomSpinTurns = 3;

    [Tooltip("Easing for RandomSpin(). OutCubic/OutQuart give a roulette-like fast start that decelerates into the landing.")]
    [SerializeField] private Ease randomSpinEase = Ease.OutCubic;

    [Header("Attacker")]
    [Tooltip("Who is attacking THIS round. Set automatically by the convenience methods; also drives AttackCounter().")]
    [SerializeField] private Combatant currentAttacker = Combatant.Player;

    [Header("Events")]
    public UnityEvent onAttackHit;
    public UnityEvent onAttackBlock;
    public UnityEvent onAttackCounter;

    [Tooltip("Fires whenever a combatant's HP reaches 0.")]
    public UnityEvent onHpReachedZero;

    [Tooltip("Fires whenever a spin lands (random or player-stopped), after the move is recorded. Handy for chaining phases.")]
    public UnityEvent onSpinLanded;

    private bool isSpinning;
    private bool isRandomSpinning;
    private Tween randomSpinTween;

    // Which combatant the CURRENT spin sets a move for, and whether stopping it resolves the round.
    private Combatant moveTarget = Combatant.Enemy;
    private bool resolveAfterCurrentSpin;

    // Moves chosen this round.
    private Rps playerMove;
    private Rps enemyMove;
    private bool playerMoveSet;
    private bool enemyMoveSet;

    // --- Unity lifecycle -------------------------------------------------

    private void Awake()
    {
        if (wheel == null)
        {
            wheel = GetComponentInChildren<RouletteWheelGraphic>();
        }
        SyncWheel();
    }

    private void Start()
    {
        if (spinOnStart)
        {
            StartSpin();
        }
    }

    private void Update()
    {
        if (!isSpinning || arrow == null)
        {
            return;
        }

        // Z positive is counter-clockwise in Unity, so subtract to spin clockwise.
        arrow.Rotate(0f, 0f, -CurrentRotationSpeed() * Time.deltaTime);
    }

    private void OnDisable()
    {
        // Don't let a tween keep running against a disabled/destroyed arrow.
        randomSpinTween?.Kill();
        randomSpinTween = null;
        isRandomSpinning = false;
    }

    // --- Spin control ----------------------------------------------------

    /// <summary>Effective speed in degrees/second: base + enemy spin - player slowdown.</summary>
    public float CurrentRotationSpeed()
    {
        float enemySpin = enemyStats != null ? enemyStats.SpinSpeed : 0f;
        float playerSlow = playerStats != null ? playerStats.SlowdownSpeed : 0f;
        return Mathf.Max(minRotationSpeed, baseRotationSpeed + enemySpin - playerSlow);
    }

    // Internal: the player-timed spin is started via PlayerStopsEnemyWheel() /
    // PlayerStopsOwnWheel(), which configure the wheel first. Not called directly.
    private void StartSpin()
    {
        if (isRandomSpinning)
        {
            return; // a random spin is in progress and can't be interrupted
        }
        isSpinning = true;
    }

    /// <summary>
    /// Player's "stop" button - wire this to the STOP input/button. Freezes the arrow,
    /// records the landed move for the current target, and resolves the round if this
    /// spin was set up to (i.e. it was the player-timed spin).
    /// </summary>
    public void StopSpin()
    {
        if (isRandomSpinning || !isSpinning)
        {
            return; // the player can't stop a random spin, and nothing to stop otherwise
        }
        isSpinning = false;
        OnSpinLanded();
    }

    // Internal: started via RandomRollPlayer() / RandomRollEnemy(), which configure the
    // wheel first. Not called directly.
    private void RandomSpin()
    {
        if (isRandomSpinning)
        {
            return;
        }
        isSpinning = false; // cancel any manual spin

        if (arrow == null)
        {
            OnSpinLanded(); // nothing to animate, resolve against whatever the wheel reports
            return;
        }

        isRandomSpinning = true;

        // Uniformly random landing angle; landing odds per move then match each arc's size.
        float landingAngle = Random.value * 360f;

        // Clockwise = negative Z. Add whole turns so it visibly whirls before settling.
        // Ending at this Z leaves PointerAngleClockwise() exactly on landingAngle.
        float targetZ = -(Mathf.Max(0, randomSpinTurns) * 360f + landingAngle);

        randomSpinTween?.Kill();
        randomSpinTween = arrow
            .DOLocalRotate(new Vector3(0f, 0f, targetZ), randomSpinDuration, RotateMode.FastBeyond360)
            .SetEase(randomSpinEase)
            .OnComplete(() =>
            {
                isRandomSpinning = false;
                randomSpinTween = null;
                OnSpinLanded();
            });
    }

    // --- Convenience methods for the game's two phases -------------------

    /// <summary>Player-attack step 1: roll the PLAYER's move at random (uncontrollable).</summary>
    public void RandomRollPlayer()
    {
        ConfigureSpin(movesFor: Combatant.Player, wheelOwner: Combatant.Player, resolveAfter: false);
        RandomSpin();
    }

    /// <summary>Enemy-attack step 1: roll the ENEMY's move at random (uncontrollable).</summary>
    public void RandomRollEnemy()
    {
        ConfigureSpin(movesFor: Combatant.Enemy, wheelOwner: Combatant.Enemy, resolveAfter: false);
        RandomSpin();
    }

    /// <summary>
    /// Player-attack step 2: player attacks, so spin the ENEMY's wheel (weighted by
    /// enemy preferences) for the player to StopSpin(). Resolves when stopped.
    /// </summary>
    public void PlayerAttackPhase_SpinEnemy()
    {
        currentAttacker = Combatant.Player;
        ConfigureSpin(movesFor: Combatant.Enemy, wheelOwner: Combatant.Enemy, resolveAfter: true);
        StartSpin();
    }

    /// <summary>
    /// Enemy-attack step 2: enemy attacks, so the player defends by spinning their OWN
    /// wheel and timing StopSpin() (aiming for a block or counter). Resolves when stopped.
    /// </summary>
    public void PlayerDefensePhase_SpinPlayer()
    {
        currentAttacker = Combatant.Enemy;
        ConfigureSpin(movesFor: Combatant.Player, wheelOwner: Combatant.Player, resolveAfter: true);
        StartSpin();
    }

    /// <summary>Sets who the next spin scores for, which side's preferences shape the wheel, and whether stopping resolves.</summary>
    private void ConfigureSpin(Combatant movesFor, Combatant wheelOwner, bool resolveAfter)
    {
        moveTarget = movesFor;
        statsForWeights = wheelOwner;
        weightsFromStats = true; // the wheel always reflects the owner's RPS split (even split when unset)
        resolveAfterCurrentSpin = resolveAfter;
        SyncWheel();
    }

    /// <summary>Runs after any spin lands: record the move, raise the event, and resolve if asked.</summary>
    private void OnSpinLanded()
    {
        RecordMove(moveTarget, PointedRps());
        onSpinLanded?.Invoke();

        if (resolveAfterCurrentSpin)
        {
            Resolve();
        }
    }

    private void RecordMove(Combatant who, Rps move)
    {
        if (who == Combatant.Player)
        {
            playerMove = move;
            playerMoveSet = true;
        }
        else
        {
            enemyMove = move;
            enemyMoveSet = true;
        }

        SetSpinResult(who, move); // visible on the stats asset immediately
    }

    // Clears the moves so the next round starts fresh (called automatically after each resolve).
    private void ResetRound()
    {
        playerMoveSet = false;
        enemyMoveSet = false;
    }

    /// <summary>Angle the arrow currently points at, measured clockwise from the top (0-360).</summary>
    public float PointerAngleClockwise()
    {
        if (arrow == null)
        {
            return 0f;
        }
        return Mathf.Repeat(-arrow.localEulerAngles.z, 360f);
    }

    /// <summary>The Rock / Paper / Scissors move the arrow is currently over.</summary>
    public Rps PointedRps()
    {
        float pointer = PointerAngleClockwise();
        float total = TotalWeight();

        if (segments == null || segments.Length == 0 || total <= Mathf.Epsilon)
        {
            return Rps.Rock;
        }

        float acc = 0f;
        for (int i = 0; i < segments.Length; i++)
        {
            float sweep = (EffectiveWeight(i) / total) * 360f;
            if (pointer < acc + sweep || i == segments.Length - 1)
            {
                return segments[i].rps;
            }
            acc += sweep;
        }
        return segments[segments.Length - 1].rps;
    }

    /// <summary>
    /// Compares the two moves recorded this round (from the current attacker's point
    /// of view), records the Win/Tie/Lose action on both assets, fires the matching
    /// Hit / Block / Counter outcome, then clears the round.
    /// </summary>
    public void Resolve()
    {
        if (!playerMoveSet || !enemyMoveSet)
        {
            Debug.LogWarning($"{nameof(RouletteSpin)}: Resolve() needs BOTH moves set first (do a random roll, then a player-stopped spin).", this);
            return;
        }

        Combatant defender = Opponent(currentAttacker);
        AttackResult attackerResult = Compare(MoveOf(currentAttacker), MoveOf(defender));

        // Record the outcome from each side's own point of view.
        SetAction(currentAttacker, attackerResult);
        SetAction(defender, Invert(attackerResult));

        switch (attackerResult)
        {
            case AttackResult.Win:
                // Attacker won the RPS -> the defender takes the hit.
                AttackHit(defender.ToString());
                break;
            case AttackResult.Lose:
                AttackCounter();
                break;
            case AttackResult.Tie:
            default:
                AttackBlock();
                break;
        }

        ResetRound();
    }

    private Rps MoveOf(Combatant who)
    {
        return who == Combatant.Player ? playerMove : enemyMove;
    }

    // --- Rock / Paper / Scissors -----------------------------------------

    /// <summary>Compares two moves from <paramref name="a"/>'s point of view.</summary>
    private static AttackResult Compare(Rps a, Rps b)
    {
        if (a == b)
        {
            return AttackResult.Tie;
        }

        bool aWins =
            (a == Rps.Rock && b == Rps.Scissors) ||
            (a == Rps.Paper && b == Rps.Rock) ||
            (a == Rps.Scissors && b == Rps.Paper);

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

    private void SetSpinResult(Combatant who, Rps move)
    {
        string value = move.ToString();
        if (who == Combatant.Player && playerStats != null) playerStats.CurrentSpinResult = value;
        if (who == Combatant.Enemy && enemyStats != null) enemyStats.CurrentSpinResult = value;
    }

    private void SetAction(Combatant who, AttackResult result)
    {
        string value = result.ToString(); // "Win" / "Tie" / "Lose"
        if (who == Combatant.Player && playerStats != null) playerStats.CurrentAction = value;
        if (who == Combatant.Enemy && enemyStats != null) enemyStats.CurrentAction = value;
    }

    // --- Attack outcomes (each raises a UnityEvent) ----------------------

    /// <summary>The given target loses 1 HP. Accepts "player" or "enemy".</summary>
    public void AttackHit(string target)
    {
        if (TryParseCombatant(target, out Combatant t))
        {
            // Whoever isn't the target is the attacker for a follow-up counter.
            currentAttacker = Opponent(t);
            ModifyHp(t, -1);
        }
        else
        {
            Debug.LogWarning($"{nameof(RouletteSpin)}: AttackHit could not parse target '{target}'. Use \"player\" or \"enemy\".", this);
        }

        onAttackHit?.Invoke();
    }

    /// <summary>Nobody loses HP.</summary>
    public void AttackBlock()
    {
        onAttackBlock?.Invoke();
    }

    /// <summary>The current attacker loses 1 HP.</summary>
    public void AttackCounter()
    {
        ModifyHp(currentAttacker, -1);
        onAttackCounter?.Invoke();
    }

    // --- HP helpers ------------------------------------------------------

    private void ModifyHp(Combatant who, int delta)
    {
        switch (who)
        {
            case Combatant.Player:
                if (playerStats == null)
                {
                    Debug.LogWarning($"{nameof(RouletteSpin)}: No PlayerStats assigned.", this);
                    return;
                }
                playerStats.PlayerHP += delta;
                if (playerStats.PlayerHP <= 0)
                {
                    playerStats.PlayerHP = 0;
                    onHpReachedZero?.Invoke();
                }
                break;

            case Combatant.Enemy:
                if (enemyStats == null)
                {
                    Debug.LogWarning($"{nameof(RouletteSpin)}: No EnemyStats assigned.", this);
                    return;
                }
                enemyStats.EnemyHP += delta;
                if (enemyStats.EnemyHP <= 0)
                {
                    enemyStats.EnemyHP = 0;
                    onHpReachedZero?.Invoke();
                }
                break;
        }
    }

    private static Combatant Opponent(Combatant c)
    {
        return c == Combatant.Player ? Combatant.Enemy : Combatant.Player;
    }

    private static bool TryParseCombatant(string value, out Combatant result)
    {
        if (!string.IsNullOrEmpty(value))
        {
            string v = value.Trim().ToLowerInvariant();
            if (v == "player") { result = Combatant.Player; return true; }
            if (v == "enemy") { result = Combatant.Enemy; return true; }
        }
        result = Combatant.Player;
        return false;
    }

    // --- Wheel weights / colours ----------------------------------------

    private float TotalWeight()
    {
        float total = 0f;
        for (int i = 0; i < segments.Length; i++)
        {
            total += EffectiveWeight(i);
        }
        return total;
    }

    private float EffectiveWeight(int index)
    {
        if (!weightsFromStats)
        {
            return Mathf.Max(0f, segments[index].weight);
        }

        // Each segment's size comes from the matching RPS value on the chosen stats asset.
        float statValue = GetStatRpsValue(statsForWeights, segments[index].rps);
        if (statValue < 0f)
        {
            return Mathf.Max(0f, segments[index].weight); // stats missing -> fall back to manual
        }
        return Mathf.Max(0f, statValue);
    }

    /// <summary>The RPS stat value for a combatant, or -1 if that stats asset is not assigned.</summary>
    private float GetStatRpsValue(Combatant who, Rps move)
    {
        if (who == Combatant.Enemy && enemyStats != null)
        {
            switch (move)
            {
                case Rps.Rock: return enemyStats.EnemyRockValue;
                case Rps.Paper: return enemyStats.EnemyPaperValue;
                case Rps.Scissors: return enemyStats.EnemyScissorsValue;
            }
        }
        if (who == Combatant.Player && playerStats != null)
        {
            switch (move)
            {
                case Rps.Rock: return playerStats.PlayerRockValue;
                case Rps.Paper: return playerStats.PlayerPaperValue;
                case Rps.Scissors: return playerStats.PlayerScissorsValue;
            }
        }
        return -1f;
    }

    // Pushes the current colours + weights into the doughnut renderer (called when a spin is configured).
    private void SyncWheel()
    {
        if (wheel == null || segments == null)
        {
            return;
        }

        var slices = new List<RouletteWheelGraphic.Slice>(segments.Length);
        for (int i = 0; i < segments.Length; i++)
        {
            slices.Add(new RouletteWheelGraphic.Slice
            {
                color = segments[i].color,
                weight = EffectiveWeight(i),
            });
        }
        wheel.SetSlices(slices);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (wheel == null)
        {
            wheel = GetComponentInChildren<RouletteWheelGraphic>();
        }
        SyncWheel();
    }
#endif
}
