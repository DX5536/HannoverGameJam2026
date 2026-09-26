using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "ScriptableObjects/EnemyStats")]
public class EnemyStats_ScriptableObject : ScriptableObject
{
    [Header("General")]
    [SerializeField] private string enemyName;

    [SerializeField] private int enemyHP;

    [TextArea(3, 10)]
    [SerializeField] private string enemyDescription;

    [Header("Sprites")]
    [Tooltip("0 = idle, 1 = attack, 2 = hit")]
    [SerializeField] private Sprite[] enemyBattleSprite;

    [Tooltip("0 = idle, 1 = mad, 2 = happy")]
    [SerializeField] private Sprite[] enemyStorySprite;

    [Header("Tips")]
    [SerializeField] private string[] enemyTips;

    [Header("Rock / Paper / Scissors (0 = 0%, 1 = 100%)")]
    [Range(0f, 1f)]
    [SerializeField] private float enemyRockValue;

    [Range(0f, 1f)]
    [SerializeField] private float enemyPaperValue;

    [Range(0f, 1f)]
    [SerializeField] private float enemyScissorsValue;

    [Header("Spin")]
    [SerializeField] private float spinSpeed;

    [Header("Runtime / Debug (set by RouletteSpin)")]
    [Tooltip("The Rock / Paper / Scissors this enemy rolled on the last spin.")]
    [SerializeField] private string currentSpinResult;

    [Tooltip("How the last spin went for this enemy: Win / Tie / Lose.")]
    [SerializeField] private string currentAction;

    // --- Public properties ---
    public string EnemyName => enemyName;

    public int EnemyHP
    {
        get => enemyHP;
        set => enemyHP = value;
    }

    public string EnemyDescription => enemyDescription;

    public Sprite[] EnemyBattleSprite => enemyBattleSprite;

    public Sprite[] EnemyStorySprite => enemyStorySprite;

    public string[] EnemyTips => enemyTips;

    public float EnemyRockValue
    {
        get => enemyRockValue;
        set => enemyRockValue = value;
    }

    public float EnemyPaperValue
    {
        get => enemyPaperValue;
        set => enemyPaperValue = value;
    }

    public float EnemyScissorsValue
    {
        get => enemyScissorsValue;
        set => enemyScissorsValue = value;
    }

    public float SpinSpeed
    {
        get => spinSpeed;
        set => spinSpeed = value;
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
