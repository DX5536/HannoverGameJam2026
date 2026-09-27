using UnityEngine;

/// <summary>
/// Quits the game. Wire <see cref="QuitGame"/> to a button's OnClick.
/// In the editor it stops Play mode so you can test it.
/// </summary>
public class QuitButton : MonoBehaviour
{
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
