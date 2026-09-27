using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

/// <summary>
/// Plays a sound as each character of a Yarn line is typed out (typewriter SFX).
///
/// This is a Yarn <see cref="ActionMarkupHandler"/>: add it to your LinePresenter's
/// "Event Handlers" list in the inspector. Yarn calls <see cref="OnCharacterWillAppear"/>
/// once per visible character, which is where the tick plays.
///
/// Put it on a GameObject with an AudioSource (added automatically).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class TypingSoundMarkupHandler : ActionMarkupHandler
{
    [Tooltip("AudioSource used for the typing ticks. Auto-filled from this GameObject.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("One is picked at random per tick (leave a single clip for a consistent sound).")]
    [SerializeField] private AudioClip[] typingClips;

    [Tooltip("Play a tick every Nth character (2-3 feels good; 1 = every character).")]
    [Min(1)]
    [SerializeField] private int playEveryNCharacters = 2;

    [Tooltip("Don't tick on spaces / line breaks.")]
    [SerializeField] private bool skipWhitespace = true;

    [Tooltip("Random pitch range per tick, for a less robotic sound. Set both to 1 to disable.")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    private int counter;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    // --- ActionMarkupHandler ---------------------------------------------

    public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text) { }

    public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
    {
        counter = 0; // reset the "every Nth" cadence at the start of each line
    }

    public override YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken)
    {
        TryPlayTick(line, currentCharacterIndex);
        return YarnTask.CompletedTask; // don't add any extra delay to the typewriter
    }

    public override void OnLineDisplayComplete() { }

    public override void OnLineWillDismiss() { }

    // --- Internal --------------------------------------------------------

    private void TryPlayTick(MarkupParseResult line, int index)
    {
        if (audioSource == null || typingClips == null || typingClips.Length == 0)
        {
            return;
        }

        if (skipWhitespace &&
            line.Text != null && index >= 0 && index < line.Text.Length &&
            char.IsWhiteSpace(line.Text[index]))
        {
            return;
        }

        counter++;
        if (counter < playEveryNCharacters)
        {
            return;
        }
        counter = 0;

        AudioClip clip = typingClips[Random.Range(0, typingClips.Length)];
        if (clip == null)
        {
            return;
        }

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip);
    }
}
