using UnityEngine;

/// <summary>
/// Lets a single STOP button stop whichever wheel is currently being player-spun.
///
/// There are two RouletteSpin managers (player wheel + enemy wheel), but only one is
/// ever player-spinning at a time. RouletteSpin.StopSpin() is a no-op unless that wheel
/// is actively being stopped, so calling it on both is safe - only the active one reacts.
///
/// Wire the STOP button's OnClick to <see cref="StopActiveSpin"/>.
/// </summary>
public class RouletteStopButton : MonoBehaviour
{
    [Tooltip("The player's wheel manager (Player_RouletteSpin_Manager).")]
    [SerializeField] private RouletteSpin playerRoulette;

    [Tooltip("The enemy's wheel manager (Enemy_RouletteSpin_Manager).")]
    [SerializeField] private RouletteSpin enemyRoulette;

    /// <summary>Stops whichever wheel is currently being player-spun (safe to call anytime).</summary>
    public void StopActiveSpin()
    {
        if (playerRoulette != null)
        {
            playerRoulette.StopSpin();
        }
        if (enemyRoulette != null)
        {
            enemyRoulette.StopSpin();
        }
    }
}
