using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_TweenAnimation_2DRectMask : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RectMask2D targetMask;

    [Header("Tween Settings")]
    [Tooltip("Tween length in seconds.")]
    [SerializeField] private float duration = 1f;

    [Tooltip("Delay before the tween starts, in seconds.")]
    [SerializeField] private float delay = 0f;

    [Tooltip("Snap tweened values to whole numbers while animating.")]
    [SerializeField] private bool snap = false;

    [Tooltip("Easing curve for the tween.")]
    [SerializeField] private Ease easeType = Ease.OutQuad;

    [Header("Which Value(s) To Tween")]
    [Tooltip("You can check more than one — every checked component is tweened from Initial to End in unison.")]
    [SerializeField] private bool tweenPaddingLeft;
    [SerializeField] private bool tweenPaddingRight;
    [SerializeField] private bool tweenPaddingTop;
    [SerializeField] private bool tweenPaddingBottom;
    [SerializeField] private bool tweenSoftnessX;
    [SerializeField] private bool tweenSoftnessY;

    [Header("Values")]
    [SerializeField] private float initialValue = 0f;
    [SerializeField] private float endValue = 0f;

    private Tween currentTween;

    void Start()
    {
        //Reset every checked component to the initial value so the tween starts from a known state
        ApplyValueToCheckedFields(initialValue);
    }

    //UnityEvent-friendly entry point (void return so it shows up in the Inspector dropdown).
    //Use this from Buttons, DOTween Timeline OnPlay, etc.
    public void DOTweenRectMaskAnimation()
    {
        PlayAndReturnTween();
    }

    //Script-friendly entry point — returns the Tween so external callers can chain callbacks:
    //    myMaskAnim.PlayAndReturnTween()
    //        ?.OnComplete(() => Debug.Log("done"));
    //NOTE: Unity's UnityEvent dropdown hides this because Tween isn't a serializable type.
    //For UnityEvent wiring use DOTweenRectMaskAnimation() above.
    public Tween PlayAndReturnTween()
    {
        return Play(initialValue, endValue);
    }

    //UnityEvent-friendly REVERSE entry point (End -> Initial). Use from Buttons,
    //DOTween Timeline callbacks, onHoverExit, etc.
    public void DOTweenRectMaskAnimationReverse()
    {
        PlayReverseAndReturnTween();
    }

    //Script-friendly reverse — tweens from End back to Initial and returns the Tween.
    public Tween PlayReverseAndReturnTween()
    {
        return Play(endValue, initialValue);
    }

    //Shared play logic: snaps to 'from', then tweens the checked fields to 'to'.
    private Tween Play(float from, float to)
    {
        if (targetMask == null)
        {
            Debug.LogWarning($"[{nameof(UI_TweenAnimation_2DRectMask)}] No RectMask2D assigned on '{name}'.");
            return null;
        }

        if (currentTween != null && currentTween.IsActive())
            currentTween.Kill();

        //Snap to the start value before tweening so replays always look identical
        ApplyValueToCheckedFields(from);

        currentTween = DOTween.To(
                () => from,
                v => ApplyValueToCheckedFields(v),
                to,
                duration)
            .SetDelay(delay)
            .SetEase(easeType)
            .SetOptions(snap)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        return currentTween;
    }

    public void InstantResetToInitial()
    {
        if (currentTween != null && currentTween.IsActive())
            currentTween.Kill();

        ApplyValueToCheckedFields(initialValue);
    }

    public void InstantJumpToEnd()
    {
        if (currentTween != null && currentTween.IsActive())
            currentTween.Kill();

        ApplyValueToCheckedFields(endValue);
    }

    private void ApplyValueToCheckedFields(float value)
    {
        if (targetMask == null) return;

        //RectMask2D.padding: Vector4(left, bottom, right, top) per Unity's docs
        Vector4 padding = targetMask.padding;
        if (tweenPaddingLeft)   padding.x = value;
        if (tweenPaddingBottom) padding.y = value;
        if (tweenPaddingRight)  padding.z = value;
        if (tweenPaddingTop)    padding.w = value;
        targetMask.padding = padding;

        //Softness is Vector2Int, so round if we're driving it
        if (tweenSoftnessX || tweenSoftnessY)
        {
            Vector2Int softness = targetMask.softness;
            int intValue = Mathf.RoundToInt(value);
            if (tweenSoftnessX) softness.x = intValue;
            if (tweenSoftnessY) softness.y = intValue;
            targetMask.softness = softness;
        }
    }

    private void OnDisable()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
            currentTween = null;
        }
    }
}
