using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// DEBUG: logs what's under the mouse cursor.
///
/// - UI (the important one for buttons): uses the EventSystem raycast, listing every UI
///   element under the pointer TOP-FIRST. The first entry is what actually receives the
///   click - if it isn't your button, that first entry is the thing blocking it
///   (usually a full-screen Image / fade / panel with "Raycast Target" still on).
/// - Optionally also does 3D / 2D physics raycasts for world objects.
///
/// Drop it on any GameObject in the scene while debugging, then hover / click.
/// </summary>
public class DebugPointerRaycaster : MonoBehaviour
{
    [Header("When to log")]
    [Tooltip("Log the top UI element whenever it changes as you move the mouse.")]
    [SerializeField] private bool logTopmostOnHoverChange = true;

    [Tooltip("Log the full UI stack (all elements under the cursor) on left-click.")]
    [SerializeField] private bool logFullStackOnClick = true;

    [Header("Also raycast the world (optional)")]
    [SerializeField] private bool includePhysics3D = false;
    [SerializeField] private bool includePhysics2D = false;

    [Tooltip("Camera for world raycasts. Defaults to Camera.main.")]
    [SerializeField] private Camera worldCamera;

    private readonly List<RaycastResult> uiResults = new List<RaycastResult>();
    private int lastTopmostId;

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 mousePos = mouse.position.ReadValue();

        bool clicked = logFullStackOnClick && mouse.leftButton.wasPressedThisFrame;

        if (logTopmostOnHoverChange || clicked)
        {
            RaycastUI(mousePos, logFullStack: clicked);
        }

        if (clicked)
        {
            if (includePhysics3D) RaycastPhysics3D(mousePos);
            if (includePhysics2D) RaycastPhysics2D(mousePos);
        }
    }

    private void RaycastUI(Vector2 screenPos, bool logFullStack)
    {
        if (EventSystem.current == null)
        {
            if (logFullStack)
            {
                Debug.LogWarning($"{nameof(DebugPointerRaycaster)}: No EventSystem in the scene - UI clicks won't work at all.");
            }
            return;
        }

        var pointerData = new PointerEventData(EventSystem.current) { position = screenPos };
        uiResults.Clear();
        EventSystem.current.RaycastAll(pointerData, uiResults);

        if (uiResults.Count == 0)
        {
            if (logFullStack)
            {
                Debug.Log("[PointerDebug] UI: nothing under the cursor.");
            }
            // Reset so the next real hit logs again.
            lastTopmostId = 0;
            return;
        }

        GameObject top = uiResults[0].gameObject;

        // Hover-change logging: only when the topmost UI object changes.
        if (logTopmostOnHoverChange && top.GetInstanceID() != lastTopmostId)
        {
            lastTopmostId = top.GetInstanceID();
            Debug.Log($"[PointerDebug] Top UI under cursor: '{Path(top)}'  (layer: {LayerMask.LayerToName(top.layer)})", top);
        }

        if (logFullStack)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[PointerDebug] UI stack under cursor ({uiResults.Count}) - TOP first (top = receives the click):");
            for (int i = 0; i < uiResults.Count; i++)
            {
                GameObject go = uiResults[i].gameObject;
                sb.AppendLine($"  {i}: '{Path(go)}'  layer={LayerMask.LayerToName(go.layer)}  sortingOrder={uiResults[i].sortingOrder}");
            }
            Debug.Log(sb.ToString(), top);
        }
    }

    private void RaycastPhysics3D(Vector2 screenPos)
    {
        if (worldCamera == null) return;

        Ray ray = worldCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            Debug.Log($"[PointerDebug] 3D hit: '{Path(hit.collider.gameObject)}'  (layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})", hit.collider.gameObject);
        }
        else
        {
            Debug.Log("[PointerDebug] 3D: no collider hit.");
        }
    }

    private void RaycastPhysics2D(Vector2 screenPos)
    {
        if (worldCamera == null) return;

        Vector2 worldPoint = worldCamera.ScreenToWorldPoint(screenPos);
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);
        if (hit.collider != null)
        {
            Debug.Log($"[PointerDebug] 2D hit: '{Path(hit.collider.gameObject)}'  (layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})", hit.collider.gameObject);
        }
        else
        {
            Debug.Log("[PointerDebug] 2D: no collider hit.");
        }
    }

    // Builds a "Parent/Child/Leaf" path so you can identify the object in the hierarchy.
    private static string Path(GameObject go)
    {
        var sb = new StringBuilder(go.name);
        Transform t = go.transform.parent;
        while (t != null)
        {
            sb.Insert(0, t.name + "/");
            t = t.parent;
        }
        return sb.ToString();
    }
}
