using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "ScriptableObjects/PlayerStats")]
public class PlayerStats_ScriptableObject : ScriptableObject
{
    [Header("General")]
    [SerializeField] private string playerName;

    [SerializeField] private int playerHP;

    [TextArea(3, 10)]
    [SerializeField] private string playerDescription;

    [Header("Sprites")]
    [Tooltip("0 = idle, 1 = attack, 2 = hit")]
    [SerializeField] private Sprite[] playerBattleSprite;

    [Tooltip("0 = neutral, 1 = angry, 2 = sad")]
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

    // --- Public properties ---
    public string PlayerName => playerName;

    public int PlayerHP
    {
        get => playerHP;
        set => playerHP = value;
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
}
