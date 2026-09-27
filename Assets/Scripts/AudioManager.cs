using UnityEngine;

/// <summary>
/// Simple one-shot sound-effect player. Call the public methods from anywhere via
/// <see cref="Instance"/> (e.g. AudioManager.Instance.PlayAttackSFX()) or hook them to
/// UnityEvents (button OnClick -> PlayButtonPress, BattleResolver.onAttackHit -> PlayAttackSFX, etc.).
///
/// Put this on one GameObject in the scene, assign the clips, and (optionally) an
/// AudioSource - one is added automatically if you don't.
///
/// Duplicates (e.g. the copy in a battle scene when the Main Menu's persistent manager is
/// already alive) are NOT destroyed: they stay as silent forwarders, so UnityEvents wired to
/// the scene's own copy still reach the persistent <see cref="Instance"/>.
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

    [Header("Volume (UI slider 0-1 maps to these maxima)")]
    [Tooltip("Actual AudioSource volume when the SFX slider is at 1.")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxMaxVolume = 1f;

    [Tooltip("Actual AudioSource volume when the Music slider is at 1. Music is loud, so keep this low (e.g. 0.1).")]
    [Range(0f, 1f)]
    [SerializeField] private float musicMaxVolume = 0.1f;

    [Header("Behaviour")]
    [Tooltip("Keep this manager alive across scene loads.")]
    [SerializeField] private bool persistAcrossScenes = true;

    private const string SfxPrefKey = "vol_sfx01";
    private const string MusicPrefKey = "vol_music01";

    // 0-1 UI values (what the sliders show). Actual source volume = value * maxVolume.
    private float sfxVolume01 = 1f;
    private float musicVolume01 = 1f;

    /// <summary>Current SFX slider value (0-1).</summary>
    public float SfxVolume01 => Main.sfxVolume01;

    /// <summary>Current Music slider value (0-1).</summary>
    public float MusicVolume01 => Main.musicVolume01;

    /// <summary>The manager that actually plays audio: the persistent instance, or this one if none exists.</summary>
    private AudioManager Main => Instance != null ? Instance : this;

    private void Awake()
    {
        // Enforce a single instance. A duplicate is kept (not destroyed) as a silent forwarder:
        // the scene's UnityEvents point at it, and destroying it would silently drop those calls.
        if (Instance != null && Instance != this)
        {
            foreach (AudioSource source in GetComponents<AudioSource>())
            {
                source.playOnAwake = false;
                source.enabled = false;
            }
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

        // Restore saved volumes and apply them.
        sfxVolume01 = PlayerPrefs.GetFloat(SfxPrefKey, 1f);
        musicVolume01 = PlayerPrefs.GetFloat(MusicPrefKey, 1f);
        ApplyVolumes();
    }

    private void Start()
    {
        if (Main != this)
        {
            return; // forwarder - the persistent instance already handles its own BGM
        }

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

    public void PlayButtonPress() => Main.Play(Main.buttonPress);

    public void PlayAttackSFX() => Main.Play(Main.attackSFX);

    public void PlayBlockSFX() => Main.Play(Main.blockSFX);

    public void PlayDamageSFX() => Main.Play(Main.damageSFX);

    // --- Music (call from code or UnityEvents) ---------------------------

    /// <summary>Plays the standard / exploration BGM (does nothing if it's already playing).</summary>
    public void PlayStandardBGM() => Main.PlayMusic(Main.standardBGM);

    /// <summary>Plays the combat BGM (does nothing if it's already playing).</summary>
    public void PlayCombatBGM() => Main.PlayMusic(Main.combatBGM);

    /// <summary>Switches the music channel to a specific track and loops it.</summary>
    public void PlayMusic(AudioClip clip)
    {
        if (Main != this)
        {
            Main.PlayMusic(clip);
            return;
        }
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
        if (Main != this)
        {
            Main.StopBGM();
            return;
        }
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    // --- Volume (wire sliders' OnValueChanged here) ----------------------

    /// <summary>Sets the SFX volume from a 0-1 slider value (scaled by sfxMaxVolume) and saves it.</summary>
    public void SetSfxVolume01(float value01)
    {
        if (Main != this)
        {
            Main.SetSfxVolume01(value01);
            return;
        }
        sfxVolume01 = Mathf.Clamp01(value01);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume01 * sfxMaxVolume;
        }
        PlayerPrefs.SetFloat(SfxPrefKey, sfxVolume01);
    }

    /// <summary>Sets the music volume from a 0-1 slider value (scaled by musicMaxVolume) and saves it.</summary>
    public void SetMusicVolume01(float value01)
    {
        if (Main != this)
        {
            Main.SetMusicVolume01(value01);
            return;
        }
        musicVolume01 = Mathf.Clamp01(value01);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume01 * musicMaxVolume;
        }
        PlayerPrefs.SetFloat(MusicPrefKey, musicVolume01);
    }

    private void ApplyVolumes()
    {
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume01 * sfxMaxVolume;
        }
        if (musicSource != null)
        {
            musicSource.volume = musicVolume01 * musicMaxVolume;
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
