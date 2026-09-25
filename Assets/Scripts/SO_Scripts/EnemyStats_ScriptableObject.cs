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

    [Tooltip("0 = neutral, 1 = angry, 2 = sad")]
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
}
