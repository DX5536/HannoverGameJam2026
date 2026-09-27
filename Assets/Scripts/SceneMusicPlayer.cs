using UnityEngine;

/// <summary>
/// Picks which BGM track a scene should play, always via the persistent
/// <see cref="AudioManager"/> singleton (never a scene reference, which would break when
/// the scene's own AudioManager is destroyed by the singleton guard).
///
/// Put one in each scene: set Play On Start to Combat in the battle scene, Standard in
/// the menu. Also callable from UnityEvents (e.g. BattleSequencer.onBattleStart -> PlayCombat).
/// </summary>
public class SceneMusicPlayer : MonoBehaviour
{
    public enum Track { None, Standard, Combat }

    [Tooltip("Track to start when this scene loads. None = leave whatever is playing.")]
    [SerializeField] private Track playOnStart = Track.Combat;

    private void Start()
    {
        Play(playOnStart);
    }

    // --- Public (UnityEvent-friendly) ------------------------------------

    public void PlayStandard() => Play(Track.Standard);
    public void PlayCombat() => Play(Track.Combat);
    public void StopMusic() => Play(Track.None);

    private void Play(Track track)
    {
        AudioManager am = AudioManager.Instance;
        if (am == null)
        {
            Debug.LogWarning($"{nameof(SceneMusicPlayer)}: No AudioManager in the scene.", this);
            return;
        }

        switch (track)
        {
            case Track.Standard:
                am.PlayStandardBGM();
                break;
            case Track.Combat:
                am.PlayCombatBGM();
                break;
            case Track.None:
            default:
                break; // leave current music as-is
        }
    }
}
