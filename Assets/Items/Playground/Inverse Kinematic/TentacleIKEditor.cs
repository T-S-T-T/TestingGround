// Place this file inside an "Editor" folder (e.g. Assets/Tentacle/Editor/)
// Unity will automatically compile it as an editor-only assembly.

using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for <see cref="TentacleIK"/> (Version 1).
/// Draws bone gizmos and a line to the target in the Scene view.
/// Also adds an inspector button for "Generate Bones".
/// </summary>
[CustomEditor(typeof(TentacleIK))]
public class TentacleIKEditor : Editor
{
    // ── Colours ───────────────────────────────────────────────────────────────
    protected static readonly Color BoneColor   = new Color(0.3f, 0.9f, 0.4f, 0.9f);
    protected static readonly Color JointColor  = new Color(1.0f, 0.8f, 0.0f, 1.0f);
    protected static readonly Color TargetColor = new Color(0.0f, 0.8f, 1.0f, 1.0f);
    protected static readonly Color ReachColor  = new Color(1.0f, 1.0f, 1.0f, 0.08f);

    // ─────────────────────────────────────────────────────────────────────────
    #region Inspector GUI

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(6);

        var tentacle = (TentacleIK)target;

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("⚙  Generate Bones", GUILayout.Height(30)))
        {
            Undo.RegisterFullObjectHierarchyUndo(tentacle.gameObject, "Generate Tentacle Bones");
            tentacle.GenerateBones();
            EditorUtility.SetDirty(tentacle);
        }
        GUI.backgroundColor = Color.white;

        // Info panel
        if (tentacle.Bones != null && tentacle.Bones.Length > 0)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                $"Joints: {tentacle.Bones.Length}   " +
                $"Total reach: {tentacle.SegmentCount * tentacle.SegmentLength:F2} units",
                MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "No bones found. Press \"Generate Bones\" to build the chain.",
                MessageType.Warning);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Scene Gizmos

    protected virtual void OnSceneGUI()
    {
        var tentacle = (TentacleIK)target;
        if (tentacle == null) return;

        DrawReachCircle(tentacle);
        DrawBoneChain(tentacle);
        DrawTargetLine(tentacle);
    }

    /// <summary>Draws a disc showing the maximum reach of the tentacle.</summary>
    protected void DrawReachCircle(TentacleIK tentacle)
    {
        float reach = tentacle.SegmentCount * tentacle.SegmentLength;
        Handles.color = ReachColor;
        Handles.DrawSolidDisc(tentacle.transform.position, tentacle.transform.up, reach);
        Handles.color = new Color(1f, 1f, 1f, 0.3f);
        Handles.DrawWireDisc(tentacle.transform.position, tentacle.transform.up, reach);
    }

    /// <summary>Draws spheres at each joint and lines between them.</summary>
    protected void DrawBoneChain(TentacleIK tentacle)
    {
        var bones = tentacle.Bones;
        if (bones == null || bones.Length == 0) return;

        float jointRadius = tentacle.SegmentLength * 0.08f;
        jointRadius = Mathf.Clamp(jointRadius, 0.02f, 0.15f);

        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null) continue;

            // Joint sphere
            Handles.color = (i == 0)
                ? Color.white                           // root
                : (i == bones.Length - 1)
                    ? new Color(1f, 0.4f, 0.2f)         // tip
                    : JointColor;

            Handles.SphereHandleCap(0, bones[i].position, Quaternion.identity,
                                    jointRadius * 2f, EventType.Repaint);

            // Bone line
            if (i < bones.Length - 1 && bones[i + 1] != null)
            {
                Handles.color = BoneColor;
                Handles.DrawAAPolyLine(4f, bones[i].position, bones[i + 1].position);
            }
        }
    }

    /// <summary>Draws a dashed line from the tip to the target.</summary>
    protected void DrawTargetLine(TentacleIK tentacle)
    {
        if (tentacle.Target == null) return;
        var bones = tentacle.Bones;
        if (bones == null || bones.Length == 0) return;

        Transform tip = bones[bones.Length - 1];
        if (tip == null) return;

        Handles.color = TargetColor;
        DrawDashedLine(tip.position, tentacle.Target.position, 0.15f);

        // Target marker
        Handles.SphereHandleCap(0, tentacle.Target.position, Quaternion.identity,
                                HandleUtility.GetHandleSize(tentacle.Target.position) * 0.15f,
                                EventType.Repaint);
        Handles.Label(tentacle.Target.position + Vector3.up * 0.2f,
                      "Target", EditorStyles.miniLabel);
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region Utility

    protected static void DrawDashedLine(Vector3 a, Vector3 b, float dashLen)
    {
        Vector3 dir  = b - a;
        float   dist = dir.magnitude;
        if (dist < 0.001f) return;

        dir /= dist;
        float d = 0f;
        bool  on = true;
        while (d < dist)
        {
            float next = Mathf.Min(d + dashLen, dist);
            if (on) Handles.DrawAAPolyLine(2f, a + dir * d, a + dir * next);
            d  = next;
            on = !on;
        }
    }

    #endregion
    #endregion
}
