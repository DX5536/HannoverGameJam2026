using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-boss turn-order + timing data for <see cref="BattleSequencer"/>. Make one asset
/// per boss (Create > ScriptableObjects > BossSequence) and drop it on the sequencer, so
/// swapping bosses is just swapping this asset.
/// </summary>
[CreateAssetMenu(fileName = "BossSequence", menuName = "ScriptableObjects/BossSequence")]
public class BossSequence_ScriptableObject : ScriptableObject
{
    [Header("Turn order")]
    [SerializeField] private BattleSequencer.TurnMode mode = BattleSequencer.TurnMode.PingPong;

    [Tooltip("PingPong / FixedList: who attacks on the very first turn.")]
    [SerializeField] private BattleSequencer.Turn firstAttacker = BattleSequencer.Turn.Player;

    [Tooltip("FixedList mode: the exact attacker order, e.g. Player, Enemy, Enemy, Enemy, Player, Enemy...")]
    [SerializeField] private List<BattleSequencer.Turn> customOrder = new List<BattleSequencer.Turn>
    {
        BattleSequencer.Turn.Player,
        BattleSequencer.Turn.Enemy,
    };

    [Tooltip("FixedList mode: what to do once the custom order has been played through - repeat it, fall into PingPong, fall into Random, or end the battle.")]
    [SerializeField] private BattleSequencer.AfterList afterList = BattleSequencer.AfterList.Repeat;

    [Tooltip("Random mode: chance (0-1) that a turn is the Enemy's. 0.5 = even.")]
    [Range(0f, 1f)]
    [SerializeField] private float randomEnemyChance = 0.5f;

    [Header("Timing")]
    [Tooltip("Seconds to wait after AdvanceTurn() before the next turn's event fires.")]
    [SerializeField] private float delayBetweenTurns = 0.5f;

    // --- Public properties ---
    public BattleSequencer.TurnMode Mode => mode;

    public BattleSequencer.Turn FirstAttacker => firstAttacker;

    public List<BattleSequencer.Turn> CustomOrder => customOrder;

    public BattleSequencer.AfterList AfterListBehaviour => afterList;

    public float RandomEnemyChance => randomEnemyChance;

    public float DelayBetweenTurns => delayBetweenTurns;
}
