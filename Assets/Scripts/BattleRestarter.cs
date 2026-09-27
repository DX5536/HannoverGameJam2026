using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Restarts the battle in place - no scene reload, so your cutscene isn't replayed.
///
/// Wire <see cref="RestartBattle"/> to KeyboardKeyManager.onRestartKey (the R key that the
/// lose animation arms). It resets both combatants' HP, clears the resolver's game-over
/// state, raises <see cref="onRestart"/> (hook HP-bar refills / portrait resets / SFX),
/// then starts the battle sequencer again.
/// </summary>
public class BattleRestarter : MonoBehaviour
{
    [Header("Stats to reset to full HP")]
    [SerializeField] private PlayerStats_ScriptableObject playerStats;
    [SerializeField] private EnemyStats_ScriptableObject enemyStats;

    [Header("Battle systems")]
    [SerializeField] private BattleResolver battleResolver;
    [SerializeField] private BattleSequencer battleSequencer;

    [Tooltip("Fired after HP is reset and the battle state is cleared, but BEFORE the sequencer starts. Wire HPBar.InitializeBar(), sprite resets, restart SFX, etc.")]
    public UnityEvent onRestart;

    /// <summary>Resets HP + battle state and restarts the sequence, all in the current scene.</summary>
    public void RestartBattle()
    {
        // 1. Back to full health.
        if (playerStats != null) playerStats.ResetHP();
        if (enemyStats != null) enemyStats.ResetHP();

        // 2. Clear "someone won" so the sequencer won't immediately stop again.
        if (battleResolver != null) battleResolver.ResetBattle();

        // 3. Let the scene refresh (HP bars, portraits, SFX) before the first turn fires.
        onRestart?.Invoke();

        // 4. Kick off the battle again.
        if (battleSequencer != null)
        {
            battleSequencer.StartBattle();
        }
        else
        {
            Debug.LogWarning($"{nameof(BattleRestarter)}: No BattleSequencer assigned.", this);
        }
    }
}
