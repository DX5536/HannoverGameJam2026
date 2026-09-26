using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Decides WHOSE turn it is and announces it via UnityEvents - it knows nothing about
/// RouletteSpin, DOTween Timelines or SFX. Wire those up to <see cref="onPlayerTurn"/>
/// and <see cref="onEnemyTurn"/> in the Inspector (like a DOTween Timeline callback).
///
/// A turn ends when something calls <see cref="AdvanceTurn"/> - e.g. a DOTween Timeline
/// end-callback, or RouletteSpin's resolve event. That schedules the next turn after an
/// optional delay, so async / player-input turns work without any polling.
///
/// Turn order comes from <see cref="mode"/>:
///   - PingPong : back-and-forth (first two bosses), e.g. Player, Enemy, Player, Enemy...
///   - FixedList: an authored order (final boss), e.g. Player, Enemy, Enemy, Enemy, Player...
///   - Random   : weighted-random each turn (future updates).
/// </summary>
public class BattleSequencer : MonoBehaviour
{
    public enum Turn { Player, Enemy }
    public enum TurnMode { PingPong, FixedList, Random }

    /// <summary>What FixedList mode does once the custom order has been played through.</summary>
    public enum AfterList { Repeat, PingPong, Random, EndBattle }

    [Header("Sequence source (optional)")]
    [Tooltip("Assign a BossSequence asset to drive the turn order + timing from data. When set, it overrides the inline fields below.")]
    [SerializeField] private BossSequence_ScriptableObject sequence;

    [Header("Turn order (used when no BossSequence is assigned)")]
    [SerializeField] private TurnMode mode = TurnMode.PingPong;

    [Tooltip("PingPong / FixedList: who attacks on the very first turn.")]
    [SerializeField] private Turn firstAttacker = Turn.Player;

    [Tooltip("FixedList mode: the exact attacker order, e.g. Player, Enemy, Enemy, Enemy, Player, Enemy...")]
    [SerializeField] private List<Turn> customOrder = new List<Turn> { Turn.Player, Turn.Enemy };

    [Tooltip("FixedList mode: what to do once the custom order has been played through - repeat it, fall into PingPong, fall into Random, or end the battle.")]
    [SerializeField] private AfterList afterList = AfterList.Repeat;

    [Tooltip("Random mode: chance (0-1) that a turn is the Enemy's. 0.5 = even.")]
    [Range(0f, 1f)]
    [SerializeField] private float randomEnemyChance = 0.5f;

    [Header("Timing")]
    [Tooltip("Seconds to wait after AdvanceTurn() before the next turn's event fires. You can also drive advancement purely from DOTween Timeline callbacks and leave this at 0.")]
    [SerializeField] private float delayBetweenTurns = 0.5f;

    [Tooltip("Fire the first turn automatically on Start().")]
    [SerializeField] private bool autoStart = false;

    [Header("Events (wire DOTween Timelines, RouletteSpin, SFX here)")]
    public UnityEvent onBattleStart;
    public UnityEvent onPlayerTurn;
    public UnityEvent onEnemyTurn;
    public UnityEvent onBattleEnd;

    private int turnIndex = -1;
    private bool battleRunning;
    private Tween pendingTurnTween;

    /// <summary>Who is attacking on the turn that is currently active.</summary>
    public Turn CurrentAttacker { get; private set; }

    /// <summary>0-based count of how many turns have fired this battle.</summary>
    public int TurnNumber => turnIndex;

    // Prefer the assigned BossSequence asset; fall back to the inline inspector fields.
    private TurnMode ActiveMode => sequence != null ? sequence.Mode : mode;
    private Turn ActiveFirstAttacker => sequence != null ? sequence.FirstAttacker : firstAttacker;
    private List<Turn> ActiveCustomOrder => sequence != null ? sequence.CustomOrder : customOrder;
    private AfterList ActiveAfterList => sequence != null ? sequence.AfterListBehaviour : afterList;
    private float ActiveRandomEnemyChance => sequence != null ? sequence.RandomEnemyChance : randomEnemyChance;
    private float ActiveDelayBetweenTurns => sequence != null ? sequence.DelayBetweenTurns : delayBetweenTurns;

    private void Start()
    {
        if (autoStart)
        {
            StartBattle();
        }
    }

    private void OnDisable()
    {
        StopPending();
    }

    // --- Control ---------------------------------------------------------

    /// <summary>Resets to turn 0 and fires the first turn immediately.</summary>
    public void StartBattle()
    {
        StopPending();
        turnIndex = -1;
        battleRunning = true;
        onBattleStart?.Invoke();
        FireNextTurn(); // first turn is immediate; between-turn delays apply afterwards
    }

    /// <summary>
    /// Call when the current turn's actions are done (from a DOTween Timeline callback,
    /// RouletteSpin's resolve event, etc.). Schedules the next turn after the default delay.
    /// </summary>
    public void AdvanceTurn()
    {
        AdvanceTurnAfter(ActiveDelayBetweenTurns);
    }

    /// <summary>Same as <see cref="AdvanceTurn"/> but with an explicit delay (seconds).</summary>
    public void AdvanceTurnAfter(float delay)
    {
        if (!battleRunning)
        {
            return;
        }

        StopPending();

        if (delay <= 0f)
        {
            FireNextTurn();
        }
        else
        {
            pendingTurnTween = DOVirtual.DelayedCall(delay, FireNextTurn);
        }
    }

    /// <summary>Ends the battle (e.g. wire RouletteSpin.onHpReachedZero here) and stops any pending turn.</summary>
    public void StopBattle()
    {
        if (!battleRunning)
        {
            return;
        }

        battleRunning = false;
        StopPending();
        onBattleEnd?.Invoke();
    }

    // --- Internals -------------------------------------------------------

    private void FireNextTurn()
    {
        if (!battleRunning)
        {
            return;
        }

        turnIndex++;

        if (!TryGetAttacker(turnIndex, out Turn who))
        {
            StopBattle(); // ran off the end of a non-looping FixedList
            return;
        }

        CurrentAttacker = who;

        if (who == Turn.Player)
        {
            onPlayerTurn?.Invoke();
        }
        else
        {
            onEnemyTurn?.Invoke();
        }
    }

    private bool TryGetAttacker(int index, out Turn who)
    {
        switch (ActiveMode)
        {
            case TurnMode.PingPong:
                who = (index % 2 == 0) ? ActiveFirstAttacker : Opposite(ActiveFirstAttacker);
                return true;

            case TurnMode.FixedList:
                List<Turn> order = ActiveCustomOrder;
                if (order == null || order.Count == 0)
                {
                    who = ActiveFirstAttacker;
                    return false;
                }
                if (index < order.Count)
                {
                    who = order[index];
                    return true;
                }

                // The custom order has finished - decide what happens next.
                switch (ActiveAfterList)
                {
                    case AfterList.Repeat:
                        who = order[index % order.Count];
                        return true;

                    case AfterList.PingPong:
                        // Keep alternating, continuing on from the last entry (no doubled side at the seam).
                        int post = index - order.Count;
                        Turn last = order[order.Count - 1];
                        who = (post % 2 == 0) ? Opposite(last) : last;
                        return true;

                    case AfterList.Random:
                        who = (Random.value < ActiveRandomEnemyChance) ? Turn.Enemy : Turn.Player;
                        return true;

                    case AfterList.EndBattle:
                    default:
                        who = ActiveFirstAttacker;
                        return false;
                }

            case TurnMode.Random:
            default:
                who = (Random.value < ActiveRandomEnemyChance) ? Turn.Enemy : Turn.Player;
                return true;
        }
    }

    private static Turn Opposite(Turn t)
    {
        return t == Turn.Player ? Turn.Enemy : Turn.Player;
    }

    private void StopPending()
    {
        pendingTurnTween?.Kill();
        pendingTurnTween = null;
    }
}
