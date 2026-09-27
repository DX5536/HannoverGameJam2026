using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Colors a dialogue "CharacterName" TMP_Text based on who is speaking.
///
/// Yarn's Character Colors (in the .yarnproject) are editor-only, so this mirrors them
/// at runtime: fill the list with name -> colour (pre-filled with the current cast) and
/// this recolors the CharacterName text whenever it changes to a new speaker.
///
/// Put it next to your dialogue UI and assign the CharacterName text.
/// </summary>
public class CharacterNameColorizer : MonoBehaviour
{
    [System.Serializable]
    public struct CharacterColor
    {
        [Tooltip("Must match the character name exactly as it appears in the CharacterName text (case-insensitive).")]
        public string name;
        public Color color;
    }

    [Tooltip("The dialogue's character-name label to recolor.")]
    [SerializeField] private TMP_Text characterName;

    [Tooltip("Colour used when the speaker isn't in the list below.")]
    [SerializeField] private Color defaultColor = Color.white;

    [Tooltip("Name -> colour. Copied from the .yarnproject's Character Colors.")]
    [SerializeField]
    private List<CharacterColor> characterColors = new List<CharacterColor>
    {
        new CharacterColor { name = "Sir Oreon",                  color = new Color32(0x14, 0x49, 0xA0, 0xFF) },
        new CharacterColor { name = "lady Waphelina",            color = new Color32(0xFF, 0xAD, 0x00, 0xFF) },
        new CharacterColor { name = "Gottfried Wilhelm Leibniz", color = new Color32(0x9F, 0x1B, 0x3F, 0xFF) },
        new CharacterColor { name = "Adventurer",                color = new Color32(0x6B, 0x7C, 0x83, 0xFF) },
    };

    private string lastAppliedName;

    private void LateUpdate()
    {
        // Runs after the line presenter sets the name each frame; recolor only on change.
        if (characterName == null)
        {
            return;
        }

        if (characterName.text != lastAppliedName)
        {
            lastAppliedName = characterName.text;
            characterName.color = GetColorFor(lastAppliedName);
        }
    }

    /// <summary>Returns the colour mapped to a name, or the default colour if not listed.</summary>
    public Color GetColorFor(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            string trimmed = name.Trim();
            foreach (CharacterColor entry in characterColors)
            {
                if (!string.IsNullOrEmpty(entry.name) &&
                    string.Equals(entry.name.Trim(), trimmed, System.StringComparison.OrdinalIgnoreCase))
                {
                    return entry.color;
                }
            }
        }
        return defaultColor;
    }
}
