using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Bridges Yarn Spinner and the player's stats ScriptableObject.
///
/// Registers the following commands so they can be called from a .yarn file:
///     <<SetRockValue 0.6>>
///     <<SetPaperValue 0.6>>
///     <<SetScissorsValue 0.6>>
///     <<SetSlowDownSpeed 2.5>>
///
/// The three Rock/Paper/Scissors values always add up to 1 (100%). When you set
/// one of them, the remaining amount (1 - newValue) is shared between the other
/// two *proportionally to their current values*.
///
/// Example: starting at 1/3 each, calling <<SetScissorsValue 0.6>> leaves 0.4 to
/// split between rock and paper. Since they were equal, each becomes 0.2.
/// </summary>
public class PlayerValueEditorViaYarnSpinner : MonoBehaviour
{
    [Tooltip("The player stats asset this script reads from and writes to.")]
    [SerializeField] private PlayerStats_ScriptableObject playerStats;

    [Tooltip("Optional. If left empty, the first DialogueRunner in the scene is used.")]
    [SerializeField] private DialogueRunner dialogueRunner;

    private enum Channel { Rock, Paper, Scissors }

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
            Debug.LogError($"{nameof(PlayerValueEditorViaYarnSpinner)}: No DialogueRunner found. Yarn commands were not registered.", this);
            return;
        }

        dialogueRunner.AddCommandHandler<float>("SetRockValue", SetRockValue);
        dialogueRunner.AddCommandHandler<float>("SetPaperValue", SetPaperValue);
        dialogueRunner.AddCommandHandler<float>("SetScissorsValue", SetScissorsValue);
        dialogueRunner.AddCommandHandler<float>("SetSlowDownSpeed", SetSlowDownSpeed);
    }

    // --- Yarn commands ---------------------------------------------------

    /// <summary><c>&lt;&lt;SetRockValue 0.6&gt;&gt;</c></summary>
    public void SetRockValue(float value) => SetChannel(Channel.Rock, value);

    /// <summary><c>&lt;&lt;SetPaperValue 0.6&gt;&gt;</c></summary>
    public void SetPaperValue(float value) => SetChannel(Channel.Paper, value);

    /// <summary><c>&lt;&lt;SetScissorsValue 0.6&gt;&gt;</c></summary>
    public void SetScissorsValue(float value) => SetChannel(Channel.Scissors, value);

    /// <summary><c>&lt;&lt;SetSlowDownSpeed 2.5&gt;&gt;</c> (not part of the 100% balance).</summary>
    public void SetSlowDownSpeed(float value)
    {
        if (playerStats == null)
        {
            Debug.LogWarning($"{nameof(PlayerValueEditorViaYarnSpinner)}: No PlayerStats assigned.", this);
            return;
        }

        playerStats.SlowdownSpeed = value;
    }

    // --- Balancing logic -------------------------------------------------

    private void SetChannel(Channel channel, float newValue)
    {
        if (playerStats == null)
        {
            Debug.LogWarning($"{nameof(PlayerValueEditorViaYarnSpinner)}: No PlayerStats assigned.", this);
            return;
        }

        newValue = Mathf.Clamp01(newValue);
        float remaining = 1f - newValue;

        float rock = playerStats.PlayerRockValue;
        float paper = playerStats.PlayerPaperValue;
        float scissors = playerStats.PlayerScissorsValue;

        switch (channel)
        {
            case Channel.Rock:
                Distribute(remaining, ref paper, ref scissors); // uses their current ratio
                rock = newValue;
                break;
            case Channel.Paper:
                Distribute(remaining, ref rock, ref scissors);
                paper = newValue;
                break;
            case Channel.Scissors:
                Distribute(remaining, ref rock, ref paper);
                scissors = newValue;
                break;
        }

        playerStats.PlayerRockValue = rock;
        playerStats.PlayerPaperValue = paper;
        playerStats.PlayerScissorsValue = scissors;
    }

    /// <summary>
    /// Splits <paramref name="amount"/> between <paramref name="a"/> and <paramref name="b"/>
    /// proportionally to their current values. If both are 0, splits evenly.
    /// </summary>
    private static void Distribute(float amount, ref float a, ref float b)
    {
        float sum = a + b;

        if (sum <= Mathf.Epsilon)
        {
            a = amount * 0.5f;
            b = amount * 0.5f;
        }
        else
        {
            a = amount * (a / sum);
            b = amount * (b / sum);
        }
    }

    // --- Convenience -----------------------------------------------------

    /// <summary>Resets the three values back to an even 1/3 split (100% total).</summary>
    [ContextMenu("Reset RPS To Even Split")]
    public void ResetToEvenSplit()
    {
        if (playerStats == null) return;

        float third = 1f / 3f;
        playerStats.PlayerRockValue = third;
        playerStats.PlayerPaperValue = third;
        playerStats.PlayerScissorsValue = third;
    }
}
