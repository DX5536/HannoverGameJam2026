using UnityEngine;

/// <summary>
/// Simple one-shot sound-effect player. Call the public methods from anywhere via
/// <see cref="Instance"/> (e.g. AudioManager.Instance.PlayAttackSFX()) or hook them to
/// UnityEvents (button OnClick -> PlayButtonPress, BattleResolver.onAttackHit -> PlayAttackSFX, etc.).
///
/// Put this on one GameObject in the scene, assign the clips, and (optionally) an
/// AudioSource - one is added automatically if you don't.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    /// <summary>Global access point so other scripts can play sounds without a reference.</summary>
    public static AudioManager Instance { get; private set; }

    [Header("SFX channel")]
    [Tooltip("The AudioSource used for one-shot SFX. Auto-filled from this GameObject if left empty.")]
    [SerializeField] private AudioSource sfxSource;

    [Header("SFX clips")]
    [SerializeField] private AudioClip buttonPress;
    [SerializeField] private AudioClip attackSFX;
    [SerializeField] private AudioClip blockSFX;
    [SerializeField] private AudioClip damageSFX;

    [Header("Music channel")]
    [Tooltip("The AudioSource used for looping background music. A separate one is created automatically if left empty.")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("Standard / exploration / menu background music.")]
    [SerializeField] private AudioClip standardBGM;

    [Tooltip("Combat background music.")]
    [SerializeField] private AudioClip combatBGM;

    [Tooltip("Play the standard BGM automatically on Start().")]
    [SerializeField] private bool playStandardBgmOnStart = true;

    [Header("Behaviour")]
    [Tooltip("Keep this manager alive across scene loads.")]
    [SerializeField] private bool persistAcrossScenes = true;

    private void Awake()
    {
        // Enforce a single instance.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        // Music gets its own AudioSource so it can loop independently of the SFX.
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        musicSource.playOnAwake = false;
        musicSource.loop = true;
    }

    private void Start()
    {
        if (playStandardBgmOnStart && standardBGM != null)
        {
            PlayStandardBGM();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // --- Public methods (call from code or UnityEvents) ------------------

    public void PlayButtonPress() => Play(buttonPress);

    public void PlayAttackSFX() => Play(attackSFX);

    public void PlayBlockSFX() => Play(blockSFX);

    public void PlayDamageSFX() => Play(damageSFX);

    // --- Music (call from code or UnityEvents) ---------------------------

    /// <summary>Plays the standard / exploration BGM (does nothing if it's already playing).</summary>
    public void PlayStandardBGM() => PlayMusic(standardBGM);

    /// <summary>Plays the combat BGM (does nothing if it's already playing).</summary>
    public void PlayCombatBGM() => PlayMusic(combatBGM);

    /// <summary>Switches the music channel to a specific track and loops it.</summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null)
        {
            return;
        }
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return; // already playing this track
        }
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    /// <summary>Stops the background music.</summary>
    public void StopBGM()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    // --- Internal --------------------------------------------------------

    private void Play(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }
        sfxSource.PlayOneShot(clip);
    }
}
