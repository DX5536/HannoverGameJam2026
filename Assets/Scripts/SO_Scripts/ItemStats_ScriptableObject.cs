using UnityEngine;

[CreateAssetMenu(fileName = "ItemStats", menuName = "ScriptableObjects/ItemStats")]
public class ItemStats_ScriptableObject : ScriptableObject
{
    [Header("General")]
    [SerializeField] private string itemName;

    [TextArea(3, 10)]
    [SerializeField] private string itemDescription;

    [Header("Sprite")]
    [SerializeField] private Sprite itemSprite;

    [Header("Rock / Paper / Scissors (0 = 0%, 1 = 100%)")]
    [Range(0f, 1f)]
    [SerializeField] private float itemRockValue;

    [Range(0f, 1f)]
    [SerializeField] private float itemPaperValue;

    [Range(0f, 1f)]
    [SerializeField] private float itemScissorsValue;

    [Header("Spin")]
    [SerializeField] private float spinSpeedBonus;

    // --- Public properties ---
    public string ItemName => itemName;

    public string ItemDescription => itemDescription;

    public Sprite ItemSprite => itemSprite;

    public float ItemRockValue
    {
        get => itemRockValue;
        set => itemRockValue = value;
    }

    public float ItemPaperValue
    {
        get => itemPaperValue;
        set => itemPaperValue = value;
    }

    public float ItemScissorsValue
    {
        get => itemScissorsValue;
        set => itemScissorsValue = value;
    }

    public float SpinSpeedBonus
    {
        get => spinSpeedBonus;
        set => spinSpeedBonus = value;
    }
}
