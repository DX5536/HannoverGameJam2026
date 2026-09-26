using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds a UI Slider (range 0-1) to one of the AudioManager's volume channels.
/// The slider shows 0-1; AudioManager scales that to the channel's real max volume
/// (SFX max 1, Music max ~0.1), so a full slider isn't ear-splitting for music.
///
/// Put this on the slider GameObject (or assign the slider), pick the channel, done.
/// The value is saved by AudioManager (PlayerPrefs) and restored here on enable.
/// </summary>
[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    public enum Channel { SFX, Music }

    [SerializeField] private Channel channel = Channel.SFX;

    [Tooltip("The slider to read. Auto-filled from this GameObject if left empty.")]
    [SerializeField] private Slider slider;

    private void Awake()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }
        slider.minValue = 0f;
        slider.maxValue = 1f;
    }

    private void OnEnable()
    {
        // Show the current saved value without firing our own callback.
        if (AudioManager.Instance != null)
        {
            float current = channel == Channel.SFX
                ? AudioManager.Instance.SfxVolume01
                : AudioManager.Instance.MusicVolume01;
            slider.SetValueWithoutNotify(current);
        }

        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value01)
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        if (channel == Channel.SFX)
        {
            AudioManager.Instance.SetSfxVolume01(value01);
        }
        else
        {
            AudioManager.Instance.SetMusicVolume01(value01);
        }
    }
}
