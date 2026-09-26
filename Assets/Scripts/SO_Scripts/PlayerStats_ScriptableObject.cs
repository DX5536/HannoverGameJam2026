using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "ScriptableObjects/PlayerStats")]
public class PlayerStats_ScriptableObject : ScriptableObject
{
    [Header("General")]
    [SerializeField] private string playerName;

    [Tooltip("Maximum (full) HP.")]
    [SerializeField] private int maxHP = 5;

    [Tooltip("Current HP. Automatically reset to Max HP on load and via ResetHP().")]
    [SerializeField] private int currentHP = 5;

    [TextArea(3, 10)]
    [SerializeField] private string playerDescription;

    [Header("Sprites")]
    [Tooltip("0 = idle, 1 = attack, 2 = hit")]
    [SerializeField] private Sprite[] playerBattleSprite;

    [Tooltip("0 = idle, 1 = mad, 2 = happy")]
    [SerializeField] private Sprite[] playerStorySprite;

    [Header("Rock / Paper / Scissors (0 = 0%, 1 = 100%)")]
    [Range(0f, 1f)]
    [SerializeField] private float playerRockValue;

    [Range(0f, 1f)]
    [SerializeField] private float playerPaperValue;

    [Range(0f, 1f)]
    [SerializeField] private float playerScissorsValue;

    [Header("Spin")]
    [SerializeField] private float slowdownSpeed;

    [Header("Runtime / Debug (set by RouletteSpin)")]
    [Tooltip("The Rock / Paper / Scissors this player rolled on the last spin.")]
    [SerializeField] private string currentSpinResult;

    [Tooltip("How the last spin went for this player: Win / Tie / Lose.")]
    [SerializeField] private string currentAction;

    private void OnEnable()
    {
        // Start each play session / load at full health so battles never inherit a stale value.
        ResetHP();
    }

    /// <summary>Restores current HP to full (max). Call at the start of a battle.</summary>
    public void ResetHP()
    {
        currentHP = maxHP;
    }

    // --- Public properties ---
    public string PlayerName => playerName;

    public int MaxHP => maxHP;

    public int CurrentHP
    {
        get => currentHP;
        set => currentHP = value;
    }

    public string PlayerDescription => playerDescription;

    public Sprite[] PlayerBattleSprite => playerBattleSprite;

    public Sprite[] PlayerStorySprite => playerStorySprite;

    public float PlayerRockValue
    {
        get => playerRockValue;
        set => playerRockValue = value;
    }

    public float PlayerPaperValue
    {
        get => playerPaperValue;
        set => playerPaperValue = value;
    }

    public float PlayerScissorsValue
    {
        get => playerScissorsValue;
        set => playerScissorsValue = value;
    }

    public float SlowdownSpeed
    {
        get => slowdownSpeed;
        set => slowdownSpeed = value;
    }

    public string CurrentSpinResult
    {
        get => currentSpinResult;
        set => currentSpinResult = value;
    }

    public string CurrentAction
    {
        get => currentAction;
        set => currentAction = value;
    }
}
