using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a Unity UI Slider as an HP bar for the player or the enemy.
///
/// - The slider's max value is the assigned stats asset's MaxHP; the fill is its
///   CurrentHP. Assign ONE: an EnemyStats or a PlayerStats.
/// - Call <see cref="UpdateHPBar"/> from anywhere after changing HP; it tweens the
///   slider from its current value to the stats asset's current HP.
///
/// No UnityEvents by design.
/// </summary>
public class HPBar : MonoBehaviour
{
    [Tooltip("The slider to drive. Auto-filled from this GameObject if left empty.")]
    [SerializeField] private Slider slider;

    [Header("Stats (assign ONE)")]
    [Tooltip("Assign this when the bar tracks the ENEMY (reads its MaxHP / CurrentHP).")]
    [SerializeField] private EnemyStats_ScriptableObject enemyStats;

    [Tooltip("Assign this when the bar tracks the PLAYER (reads its MaxHP / CurrentHP).")]
    [SerializeField] private PlayerStats_ScriptableObject playerStats;

    [Header("Tween")]
    [Tooltip("How long the bar takes to slide to the new value, in seconds.")]
    [SerializeField] private float tweenDuration = 0.3f;

    [Tooltip("Easing for the bar tween.")]
    [SerializeField] private Ease tweenEase = Ease.OutQuad;

    [SerializeField]
    private TextMeshProUGUI hpText;

    private Tween tween;

    private void Awake()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }
    }

    private void Start()
    {
        InitializeBar();
    }

    private void OnDisable()
    {
        tween?.Kill();
        tween = null;
    }

    /// <summary>Sets the slider range from the stats asset's Max HP and fills it to Current HP.</summary>
    public void InitializeBar()
    {
        if (slider == null)
        {
            Debug.LogWarning($"{nameof(HPBar)}: No Slider assigned.", this);
            return;
        }

        int max = GetMaxHP();
        int current = GetCurrentHP();

        slider.minValue = 0;
        slider.maxValue = max;
        slider.value = current;

        SetText(current, max);
    }

    /// <summary>Tweens the bar from its current value to the stats asset's current HP.</summary>
    public void UpdateHPBar()
    {
        if (slider == null)
        {
            Debug.LogWarning($"{nameof(HPBar)}: No Slider assigned.", this);
            return;
        }

        int target = GetCurrentHP();
        SetText(target, GetMaxHP());

        tween?.Kill();
        tween = DOTween.To(() => slider.value, v => slider.value = v, target, tweenDuration)
            .SetEase(tweenEase);
    }

    private void SetText(int current, int max)
    {
        if (hpText != null)
        {
            hpText.text = current + "/" + max;
        }
    }

    private int GetCurrentHP()
    {
        // Player takes priority if both are set (a bar tracks one combatant).
        if (playerStats != null)
        {
            return playerStats.CurrentHP;
        }
        if (enemyStats != null)
        {
            return enemyStats.CurrentHP;
        }

        Debug.LogWarning($"{nameof(HPBar)}: No EnemyStats or PlayerStats assigned.", this);
        return 0;
    }

    private int GetMaxHP()
    {
        if (playerStats != null)
        {
            return playerStats.MaxHP;
        }
        if (enemyStats != null)
        {
            return enemyStats.MaxHP;
        }
        return 0;
    }
}
