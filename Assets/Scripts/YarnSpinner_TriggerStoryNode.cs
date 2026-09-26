using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Starts a Yarn Spinner node by name. Hook <see cref="TriggerNode"/> up to a
/// UnityEvent (button OnClick, animation event, RouletteSpin's onInstantWin, etc.)
/// and type the node's name in the event's string field.
/// </summary>
public class YarnSpinner_TriggerStoryNode : MonoBehaviour
{
    [Tooltip("Optional. If left empty, the first DialogueRunner in the scene is used.")]
    [SerializeField] private DialogueRunner dialogueRunner;

    [Tooltip("If dialogue is already running, stop it first and start the new node instead of ignoring the request.")]
    [SerializeField] private bool interruptIfAlreadyRunning = false;

    private void Awake()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        }
    }

    /// <summary>Starts the Yarn node with the given name. Wire this to a UnityEvent.</summary>
    public void TriggerNode(string nodeName)
    {
        if (dialogueRunner == null)
        {
            Debug.LogError($"{nameof(YarnSpinner_TriggerStoryNode)}: No DialogueRunner found.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(nodeName))
        {
            Debug.LogWarning($"{nameof(YarnSpinner_TriggerStoryNode)}: No node name was provided.", this);
            return;
        }

        if (dialogueRunner.IsDialogueRunning)
        {
            if (!interruptIfAlreadyRunning)
            {
                Debug.LogWarning($"{nameof(YarnSpinner_TriggerStoryNode)}: Dialogue is already running; ignoring '{nodeName}'.", this);
                return;
            }

            dialogueRunner.Stop();
        }

        _ = dialogueRunner.StartDialogue(nodeName);
    }
}
