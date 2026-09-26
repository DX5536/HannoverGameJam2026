using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a doughnut (ring) chart procedurally on a Canvas, one coloured segment
/// per entry. It renders whatever segments it is given, so the colours are fully
/// editable in the Inspector - no external sprites needed.
///
/// Segments start at the top (12 o'clock) and sweep CLOCKWISE, matching the arrow
/// used by <see cref="RouletteSpin"/>. Each segment's share of the ring is its
/// weight divided by the total weight.
///
/// Put this on a UI GameObject (RectTransform). <see cref="RouletteSpin"/> can push
/// its segments here automatically, or you can fill the list yourself for a standalone wheel.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class RouletteWheelGraphic : MaskableGraphic
{
    [System.Serializable]
    public struct Slice
    {
        public Color color;
        [Min(0f)] public float weight;
    }

    [Tooltip("Ring segments, in clockwise order starting from the top.")]
    [SerializeField] private List<Slice> slices = new List<Slice>();

    [Tooltip("Inner hole size as a fraction of the outer radius (0 = full pie, 0.6 = thin ring).")]
    [Range(0f, 0.95f)]
    [SerializeField] private float innerRadiusRatio = 0.6f;

    [Tooltip("Triangles used per full circle. Higher = smoother edges.")]
    [Range(24, 720)]
    [SerializeField] private int segmentsPerCircle = 180;

    /// <summary>Replaces the slices and redraws. Called by RouletteSpin.</summary>
    public void SetSlices(IList<Slice> newSlices)
    {
        slices.Clear();
        if (newSlices != null)
        {
            slices.AddRange(newSlices);
        }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (slices == null || slices.Count == 0)
        {
            return;
        }

        float totalWeight = 0f;
        foreach (Slice s in slices)
        {
            totalWeight += Mathf.Max(0f, s.weight);
        }
        if (totalWeight <= Mathf.Epsilon)
        {
            return;
        }

        Rect r = rectTransform.rect;
        Vector2 center = r.center;
        float outer = Mathf.Min(r.width, r.height) * 0.5f;
        float inner = outer * innerRadiusRatio;

        // 0 rad points up; angle increases clockwise.
        float startAngle = 0f;

        foreach (Slice s in slices)
        {
            float weight = Mathf.Max(0f, s.weight);
            if (weight <= Mathf.Epsilon)
            {
                continue;
            }

            float sweep = (weight / totalWeight) * (Mathf.PI * 2f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(segmentsPerCircle * (sweep / (Mathf.PI * 2f))));
            float step = sweep / steps;

            for (int i = 0; i < steps; i++)
            {
                float a0 = startAngle + step * i;
                float a1 = startAngle + step * (i + 1);
                AddQuad(vh, center, inner, outer, a0, a1, s.color);
            }

            startAngle += sweep;
        }
    }

    private static void AddQuad(VertexHelper vh, Vector2 center, float inner, float outer, float a0, float a1, Color color)
    {
        // Clockwise from top: x = sin(a), y = cos(a).
        Vector2 d0 = new Vector2(Mathf.Sin(a0), Mathf.Cos(a0));
        Vector2 d1 = new Vector2(Mathf.Sin(a1), Mathf.Cos(a1));

        int idx = vh.currentVertCount;

        vh.AddVert(center + d0 * inner, color, Vector2.zero);
        vh.AddVert(center + d0 * outer, color, Vector2.zero);
        vh.AddVert(center + d1 * outer, color, Vector2.zero);
        vh.AddVert(center + d1 * inner, color, Vector2.zero);

        vh.AddTriangle(idx + 0, idx + 1, idx + 2);
        vh.AddTriangle(idx + 2, idx + 3, idx + 0);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}
