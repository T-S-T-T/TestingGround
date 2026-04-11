// Place this file inside an "Editor" folder (e.g. Assets/Tentacle/Editor/)

using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for <see cref="TentacleBezierIK"/> (Version 2).
/// Extends <see cref="TentacleIKEditor"/> to also draw:
///   • The Bézier curve preview in the Scene view
///   • Control point markers and labels
///   • Lines from root → each CP → target showing the control polygon
/// </summary>
[CustomEditor(typeof(TentacleBezierIK))]
public class TentacleBezierIKEditor : TentacleIKEditor
{
    // ── Colours ───────────────────────────────────────────────────────────────
    private static readonly Color BezierCurveColor   = new Color(0.9f, 0.3f, 1.0f, 0.9f);
    private static readonly Color ControlPolygonColor = new Color(1.0f, 0.5f, 0.0f, 0.55f);
    private static readonly Color ControlPointColor   = new Color(1.0f, 0.6f, 0.1f, 1.0f);

    private const int BezierPreviewSamples = 48;

    // ─────────────────────────────────────────────────────────────────────────
    #region Inspector GUI

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(6);

        var tentacle = (TentacleBezierIK)target;

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("⚙  Generate Bones", GUILayout.Height(30)))
        {
            Undo.RegisterFullObjectHierarchyUndo(tentacle.gameObject, "Generate Tentacle Bones");
            tentacle.GenerateBones();
            EditorUtility.SetDirty(tentacle);
        }
        GUI.backgroundColor = Color.white;

        // Summary info
        EditorGUILayout.Space(4);

        int cpCount = CountValidControlPoints(tentacle);

        if (tentacle.Bones != null && tentacle.Bones.Length > 0)
        {
            int degree = cpCount + 1; // degree = number of CPs + 1 (for start→end)
            EditorGUILayout.HelpBox(
                $"Joints: {tentacle.Bones.Length}   " +
                $"Total reach: {tentacle.SegmentCount * tentacle.SegmentLength:F2} units\n" +
                $"Control points: {cpCount}   Bézier degree: {degree}",
                MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "No bones found. Press \"Generate Bones\" to build the chain.",
                MessageType.Warning);
        }

        if (cpCount == 0)
        {
            EditorGUILayout.HelpBox(
                "No control points assigned. Add transforms to the Control Points array " +
                "to start curving the tentacle.",
                MessageType.Info);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Scene Gizmos

    protected override void OnSceneGUI()
    {
        var tentacle = (TentacleBezierIK)target;
        if (tentacle == null) return;

        // Draw base stuff (reach circle, bone chain, target line)
        DrawReachCircle(tentacle);
        DrawBoneChain(tentacle);
        DrawTargetLine(tentacle);

        // Draw Bézier-specific overlays
        DrawControlPolygon(tentacle);
        DrawBezierCurvePreview(tentacle);
        DrawControlPointMarkers(tentacle);
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws the control polygon: root → CP0 → CP1 → … → target.
    /// This is the "cage" that defines the Bézier curve.
    /// </summary>
    private void DrawControlPolygon(TentacleBezierIK tentacle)
    {
        var cps = tentacle.ControlPoints;
        if (cps == null) return;
        if (tentacle.Target == null) return;

        // Collect valid points: root, CPs, target
        var pts = new System.Collections.Generic.List<Vector3>();
        pts.Add(tentacle.transform.position);

        foreach (var cp in cps)
            if (cp != null) pts.Add(cp.position);

        pts.Add(tentacle.Target.position);

        if (pts.Count < 2) return;

        Handles.color = ControlPolygonColor;
        for (int i = 0; i < pts.Count - 1; i++)
            DrawDashedLine(pts[i], pts[i + 1], 0.12f);
    }

    /// <summary>
    /// Samples the Bézier curve and draws it as a smooth polyline.
    /// </summary>
    private void DrawBezierCurvePreview(TentacleBezierIK tentacle)
    {
        if (tentacle.Target == null) return;

        var linePoints = new Vector3[BezierPreviewSamples + 1];
        for (int i = 0; i <= BezierPreviewSamples; i++)
        {
            float t = (float)i / BezierPreviewSamples;
            linePoints[i] = tentacle.SampleCurve(t);
        }

        Handles.color = BezierCurveColor;
        Handles.DrawAAPolyLine(2.5f, linePoints);

        // Arrow at the midpoint indicating curve direction
        if (BezierPreviewSamples >= 4)
        {
            int   mid = BezierPreviewSamples / 2;
            Vector3 dir = (linePoints[mid + 1] - linePoints[mid - 1]).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                Handles.color = BezierCurveColor * 1.2f;
                float sz = HandleUtility.GetHandleSize(linePoints[mid]) * 0.12f;
                Handles.ConeHandleCap(0, linePoints[mid], Quaternion.LookRotation(dir),
                                      sz, EventType.Repaint);
            }
        }
    }

    /// <summary>
    /// Draws a labelled sphere at each control point position.
    /// </summary>
    private void DrawControlPointMarkers(TentacleBezierIK tentacle)
    {
        var cps = tentacle.ControlPoints;
        if (cps == null) return;

        int validIdx = 0;
        for (int i = 0; i < cps.Length; i++)
        {
            if (cps[i] == null) continue;

            float sz = HandleUtility.GetHandleSize(cps[i].position) * 0.12f;

            Handles.color = ControlPointColor;
            Handles.SphereHandleCap(0, cps[i].position, Quaternion.identity,
                                    sz * 2f, EventType.Repaint);

            Handles.color = Color.white;
            Handles.Label(cps[i].position + Vector3.up * sz * 1.5f,
                          $"CP {validIdx}", EditorStyles.miniLabel);
            validIdx++;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Utility

    private static int CountValidControlPoints(TentacleBezierIK t)
    {
        if (t.ControlPoints == null) return 0;
        int n = 0;
        foreach (var cp in t.ControlPoints)
            if (cp != null) n++;
        return n;
    }

    #endregion
}
