using System.Collections.Generic;
using DG.Tweening;
using Dott;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Plays a DOTween Timeline from Yarn by naming the GameObject it sits on.
///
/// Yarn commands (registered if a DialogueRunner is in the scene):
///     &lt;&lt;PlayTimeline "MyObjectName"&gt;&gt;                     // play forward now
///     &lt;&lt;PlayTimelineDelayed "MyObjectName" 0.5&gt;&gt;          // play forward after 0.5s
///     &lt;&lt;PlayTimelineReverse "MyObjectName"&gt;&gt;              // play in reverse now
///     &lt;&lt;PlayTimelineReverseDelayed "MyObjectName" 0.5&gt;&gt;   // play in reverse after 0.5s
///
/// The target GameObject must have a <see cref="DOTweenTimeline"/> component and be
/// ACTIVE (GameObject.Find only locates active objects).
///
/// Note: reverse plays the tweens backwards (end -&gt; start). DOTween callbacks only fire
/// going forward, so sprite-swap / SFX callbacks in the timeline won't trigger on reverse.
/// </summary>
public class YarnCommand_TweenAnimationStart : MonoBehaviour
{
    [Tooltip("Optional. If left empty, the first DialogueRunner in the scene is used.")]
    [SerializeField] private DialogueRunner dialogueRunner;

    // Yarn commands are global to a DialogueRunner, so we register them only once per
    // runner - extra instances (or a persisted runner) won't re-register and error.
    private static readonly HashSet<DialogueRunner> registeredRunners = new HashSet<DialogueRunner>();

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
            Debug.LogError($"{nameof(YarnCommand_TweenAnimationStart)}: No DialogueRunner found. Yarn commands were not registered.", this);
            return;
        }

        // Drop any destroyed runners, then skip if this runner already has the commands.
        registeredRunners.RemoveWhere(r => r == null);
        if (!registeredRunners.Add(dialogueRunner))
        {
            return; // already registered on this DialogueRunner
        }

        dialogueRunner.AddCommandHandler<string>("PlayTimeline", PlayTimeline);
        dialogueRunner.AddCommandHandler<string, float>("PlayTimelineDelayed", PlayTimelineDelayed);
        dialogueRunner.AddCommandHandler<string>("PlayTimelineReverse", PlayTimelineReverse);
        dialogueRunner.AddCommandHandler<string, float>("PlayTimelineReverseDelayed", PlayTimelineReverseDelayed);
    }

    // --- Yarn commands (also callable from UnityEvents) ------------------

    /// <summary><c>&lt;&lt;PlayTimeline "MyObjectName"&gt;&gt;</c></summary>
    public void PlayTimeline(string objectName) => PlayByName(objectName, 0f, reverse: false);

    /// <summary><c>&lt;&lt;PlayTimelineDelayed "MyObjectName" 0.5&gt;&gt;</c></summary>
    public void PlayTimelineDelayed(string objectName, float delay) => PlayByName(objectName, delay, reverse: false);

    /// <summary><c>&lt;&lt;PlayTimelineReverse "MyObjectName"&gt;&gt;</c></summary>
    public void PlayTimelineReverse(string objectName) => PlayByName(objectName, 0f, reverse: true);

    /// <summary><c>&lt;&lt;PlayTimelineReverseDelayed "MyObjectName" 0.5&gt;&gt;</c></summary>
    public void PlayTimelineReverseDelayed(string objectName, float delay) => PlayByName(objectName, delay, reverse: true);

    // --- Internal --------------------------------------------------------

    private void PlayByName(string objectName, float delay, bool reverse)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            Debug.LogWarning($"{nameof(YarnCommand_TweenAnimationStart)}: No object name given.", this);
            return;
        }

        GameObject go = GameObject.Find(objectName);
        if (go == null)
        {
            Debug.LogWarning($"{nameof(YarnCommand_TweenAnimationStart)}: No active GameObject named '{objectName}' was found.", this);
            return;
        }

        DOTweenTimeline timeline = go.GetComponent<DOTweenTimeline>();
        if (timeline == null)
        {
            Debug.LogWarning($"{nameof(YarnCommand_TweenAnimationStart)}: '{objectName}' has no DOTweenTimeline component.", this);
            return;
        }

        if (delay <= 0f)
        {
            Trigger(timeline, reverse);
        }
        else
        {
            DOVirtual.DelayedCall(delay, () =>
            {
                if (timeline != null)
                {
                    Trigger(timeline, reverse);
                }
            });
        }
    }

    private static void Trigger(DOTweenTimeline timeline, bool reverse)
    {
        // Always start from a fresh sequence. The timeline auto-kills its sequence after a
        // normal play, but a reverse leaves one alive - killing here keeps both paths clean.
        if (timeline.Sequence != null && timeline.Sequence.IsActive())
        {
            timeline.Sequence.Kill();
        }

        if (!reverse)
        {
            timeline.DOPlay();
            return;
        }

        // Build + play the sequence, then reverse it end -> start.
        Sequence seq = timeline.Play();
        if (seq == null)
        {
            return;
        }

        seq.SetAutoKill(false);  // keep it alive through Complete + the backward play
        seq.Complete(false);     // snap to the end (no callbacks) - reliable even on a fresh tween
        seq.PlayBackwards();     // play from end back to start
    }
}
