using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Drives ONE roulette wheel for ONE combatant and reports the move it lands on.
/// (Player and enemy each have their own RouletteSpin; the RPS compare + HP live in
/// <see cref="BattleResolver"/>.)
///
/// - The doughnut is rendered by <see cref="RouletteWheelGraphic"/>; each slice is a
///   Rock/Paper/Scissors move, sized by this combatant's RPS preference values.
/// - The arrow is a Transform rotated around Z. The landed slice is found purely from
///   its angle (no collider), so a result only registers when a spin ends.
///
/// Two ways a move is produced:
///   - <see cref="RandomSpin"/>   : fast, uncontrollable ~0.3s spin (eased), then lands.
///   - <see cref="StartSpin"/> + <see cref="StopSpin"/> : the player times the stop.
/// Either way, when it lands it stores <see cref="LastMove"/>, writes the combatant's
/// CurrentSpinResult, pops the slice, raises <see cref="onSpinLanded"/>, and fires the
/// <see cref="Landed"/> C# event that BattleResolver listens to.
/// </summary>
public class RouletteSpin : MonoBehaviour
{
    public enum Combatant { Player, Enemy }

    /// <summary>What a wheel segment represents.</summary>
    public enum Rps { Rock, Paper, Scissors }

    [System.Serializable]
    public struct Segment
    {
        [Tooltip("The Rock / Paper / Scissors move this slice represents.")]
        public Rps rps;

        [Tooltip("Segment colour on the doughnut (editable here - no sprite needed).")]
        public Color color;

        [Tooltip("Manual share of the wheel, used only when this combatant's RPS stats are all 0.")]
        [Min(0f)] public float weight;
    }

    [Header("Identity")]
    [Tooltip("Which combatant this wheel belongs to. Sizes the wheel from their RPS values and writes their CurrentSpinResult.")]
    [SerializeField] private Combatant combatant = Combatant.Player;

    [Header("Stats")]
    [Tooltip("Enemy stats asset (for spin speed + enemy RPS weighting). Assign on both wheels.")]
    [SerializeField] private EnemyStats_ScriptableObject enemyStats;

    [Tooltip("Player stats asset (for slowdown speed + player RPS weighting). Assign on both wheels.")]
    [SerializeField] private PlayerStats_ScriptableObject playerStats;

    [Header("Wheel")]
    [Tooltip("Segments in CLOCKWISE order starting from the top.")]
    [SerializeField]
    private Segment[] segments = new Segment[]
    {
        new Segment { rps = Rps.Rock,     color = new Color(0.90f, 0.20f, 0.25f), weight = 1f },
        new Segment { rps = Rps.Paper,    color = new Color(0.10f, 0.70f, 0.45f), weight = 1f },
        new Segment { rps = Rps.Scissors, color = new Color(0.30f, 0.20f, 0.95f), weight = 1f },
    };

    [Tooltip("Optional. The doughnut renderer. If left empty it is looked up in children.")]
    [SerializeField] private RouletteWheelGraphic wheel;

    [Header("Arrow")]
    [Tooltip("The arrow transform. Rotated around Z; its art should point UP at 0 rotation.")]
    [SerializeField] private Transform arrow;

    [Tooltip("Standard rotation speed in degrees/second (360 = one full turn per second), before enemy/player modifiers.")]
    [SerializeField] private float baseRotationSpeed = 360f;

    [Tooltip("Rotation speed is never allowed below this (degrees/second).")]
    [SerializeField] private float minRotationSpeed = 0f;

    [Header("Random Spin")]
    [Tooltip("How long RandomSpin() spins for, in seconds.")]
    [SerializeField] private float randomSpinDuration = 0.3f;

    [Tooltip("How many full turns the arrow makes before settling on its random landing spot. More turns = faster blur.")]
    [SerializeField] private int randomSpinTurns = 3;

    [Tooltip("Easing for RandomSpin(). OutCubic/OutQuart give a roulette-like fast start that decelerates into the landing.")]
    [SerializeField] private Ease randomSpinEase = Ease.OutCubic;

    [Header("Landing Punch")]
    [Tooltip("Duration of the pop the landed slice plays so the player can see the result clearly.")]
    [SerializeField] private float landingPunchDuration = 0.35f;

    [Tooltip("Easing for the landing pop. OutBack overshoots then settles for a punchy feel.")]
    [SerializeField] private Ease landingPunchEase = Ease.OutBack;

    [Header("Events")]
    [Tooltip("Fires whenever this wheel lands (random or player-stopped), after the move is recorded.")]
    public UnityEvent onSpinLanded;

    /// <summary>Raised when this wheel lands, passing itself. BattleResolver subscribes to this.</summary>
    public event Action<RouletteSpin> Landed;

    /// <summary>Which combatant this wheel represents.</summary>
    public Combatant Side => combatant;

    /// <summary>The move the wheel last landed on.</summary>
    public Rps LastMove { get; private set; }

    /// <summary>True once this wheel has landed a move this round (cleared by <see cref="ResetMove"/>).</summary>
    public bool HasMove { get; private set; }

    private bool isSpinning;
    private bool isRandomSpinning;
    private Tween randomSpinTween;
    private Tween highlightTween;

    // --- Unity lifecycle -------------------------------------------------

    private void Awake()
    {
        if (wheel == null)
        {
            wheel = GetComponentInChildren<RouletteWheelGraphic>();
        }
        SyncWheel();
    }

    private void Update()
    {
        if (!isSpinning || arrow == null)
        {
            return;
        }

        // Z positive is counter-clockwise in Unity, so subtract to spin clockwise.
        arrow.Rotate(0f, 0f, -CurrentRotationSpeed() * Time.deltaTime);
    }

    private void OnDisable()
    {
        randomSpinTween?.Kill();
        randomSpinTween = null;
        highlightTween?.Kill();
        highlightTween = null;
        isRandomSpinning = false;
        isSpinning = false;
    }

    // --- Spin control ----------------------------------------------------

    /// <summary>Effective speed in degrees/second: base + enemy spin - player slowdown.</summary>
    public float CurrentRotationSpeed()
    {
        float enemySpin = enemyStats != null ? enemyStats.SpinSpeed : 0f;
        float playerSlow = playerStats != null ? playerStats.SlowdownSpeed : 0f;
        return Mathf.Max(minRotationSpeed, baseRotationSpeed + enemySpin - playerSlow);
    }

    /// <summary>Begins a player-timed spin (call <see cref="StopSpin"/> to land it).</summary>
    public void StartSpin()
    {
        if (isRandomSpinning)
        {
            return; // a random spin is in progress and can't be interrupted
        }
        ClearHighlight();
        isSpinning = true;
    }

    /// <summary>Player's "stop": freezes the arrow and lands the current player-timed spin.</summary>
    public void StopSpin()
    {
        if (isRandomSpinning || !isSpinning)
        {
            return; // can't stop a random spin, and nothing to stop otherwise
        }
        isSpinning = false;
        OnSpinLanded();
    }

    /// <summary>
    /// Fast, uncontrollable spin that eases to a stop on a random slice. Landing odds
    /// per move match each slice's size, so a weighted wheel follows those odds.
    /// </summary>
    public void RandomSpin()
    {
        if (isRandomSpinning)
        {
            return;
        }
        isSpinning = false; // cancel any manual spin
        ClearHighlight();

        if (arrow == null)
        {
            OnSpinLanded(); // nothing to animate, land against whatever the wheel reports
            return;
        }

        isRandomSpinning = true;

        // Uniformly random landing angle; landing odds per move then match each arc's size.
        float landingAngle = UnityEngine.Random.value * 360f;

        // Clockwise = negative Z. Add whole turns so it visibly whirls before settling.
        // Ending at this Z leaves PointerAngleClockwise() exactly on landingAngle.
        float targetZ = -(Mathf.Max(0, randomSpinTurns) * 360f + landingAngle);

        randomSpinTween?.Kill();
        randomSpinTween = arrow
            .DOLocalRotate(new Vector3(0f, 0f, targetZ), randomSpinDuration, RotateMode.FastBeyond360)
            .SetEase(randomSpinEase)
            .OnComplete(() =>
            {
                isRandomSpinning = false;
                randomSpinTween = null;
                OnSpinLanded();
            });
    }

    /// <summary>Clears this wheel's recorded move so the next round starts fresh.</summary>
    public void ResetMove()
    {
        HasMove = false;
    }

    // --- Landing ---------------------------------------------------------

    private void OnSpinLanded()
    {
        int index = PointedSegmentIndex();
        LastMove = index >= 0 ? segments[index].rps : Rps.Rock;
        HasMove = true;

        WriteSpinResult(LastMove);
        PlayLandingPunch(index);

        onSpinLanded?.Invoke();
        Landed?.Invoke(this);
    }

    private void WriteSpinResult(Rps move)
    {
        string value = move.ToString();
        if (combatant == Combatant.Player && playerStats != null) playerStats.CurrentSpinResult = value;
        if (combatant == Combatant.Enemy && enemyStats != null) enemyStats.CurrentSpinResult = value;
    }

    /// <summary>Pops the landed slice so the player can tell which slice the arrow settled on.</summary>
    private void PlayLandingPunch(int index)
    {
        if (wheel == null || index < 0)
        {
            return;
        }

        highlightTween?.Kill();
        wheel.SetHighlight(index, 0f);

        float amount = 0f;
        highlightTween = DOTween.To(() => amount, v =>
            {
                amount = v;
                wheel.SetHighlight(index, v);
            }, 1f, landingPunchDuration)
            .SetEase(landingPunchEase);
    }

    private void ClearHighlight()
    {
        highlightTween?.Kill();
        highlightTween = null;
        if (wheel != null)
        {
            wheel.SetHighlight(-1, 0f);
        }
    }

    // --- Pointer / segment maths -----------------------------------------

    /// <summary>Angle the arrow currently points at, measured clockwise from the top (0-360).</summary>
    public float PointerAngleClockwise()
    {
        if (arrow == null)
        {
            return 0f;
        }
        return Mathf.Repeat(-arrow.localEulerAngles.z, 360f);
    }

    /// <summary>Index of the segment the arrow is currently over, or -1 if the wheel is empty.</summary>
    public int PointedSegmentIndex()
    {
        float pointer = PointerAngleClockwise();
        float total = TotalWeight();

        if (segments == null || segments.Length == 0 || total <= Mathf.Epsilon)
        {
            return -1;
        }

        float acc = 0f;
        for (int i = 0; i < segments.Length; i++)
        {
            float sweep = (EffectiveWeight(i) / total) * 360f;
            if (pointer < acc + sweep || i == segments.Length - 1)
            {
                return i;
            }
            acc += sweep;
        }
        return segments.Length - 1;
    }

    // --- Wheel weights / colours ----------------------------------------

    private float TotalWeight()
    {
        float total = 0f;
        for (int i = 0; i < segments.Length; i++)
        {
            total += EffectiveWeight(i);
        }
        return total;
    }

    private float EffectiveWeight(int index)
    {
        // Slice size comes from this combatant's matching RPS value; fall back to the
        // manual weight only when the stats are missing / unset.
        float statValue = GetStatRpsValue(segments[index].rps);
        if (statValue < 0f)
        {
            return Mathf.Max(0f, segments[index].weight);
        }
        return Mathf.Max(0f, statValue);
    }

    /// <summary>This combatant's RPS stat value for a move, or -1 if the stats asset is missing.</summary>
    private float GetStatRpsValue(Rps move)
    {
        if (combatant == Combatant.Enemy && enemyStats != null)
        {
            switch (move)
            {
                case Rps.Rock: return enemyStats.EnemyRockValue;
                case Rps.Paper: return enemyStats.EnemyPaperValue;
                case Rps.Scissors: return enemyStats.EnemyScissorsValue;
            }
        }
        if (combatant == Combatant.Player && playerStats != null)
        {
            switch (move)
            {
                case Rps.Rock: return playerStats.PlayerRockValue;
                case Rps.Paper: return playerStats.PlayerPaperValue;
                case Rps.Scissors: return playerStats.PlayerScissorsValue;
            }
        }
        return -1f;
    }

    // Pushes the current colours + weights into the doughnut renderer.
    private void SyncWheel()
    {
        if (wheel == null || segments == null)
        {
            return;
        }

        var slices = new List<RouletteWheelGraphic.Slice>(segments.Length);
        for (int i = 0; i < segments.Length; i++)
        {
            slices.Add(new RouletteWheelGraphic.Slice
            {
                color = segments[i].color,
                weight = EffectiveWeight(i),
            });
        }
        wheel.SetSlices(slices);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (wheel == null)
        {
            wheel = GetComponentInChildren<RouletteWheelGraphic>();
        }
        SyncWheel();
    }
#endif
}
