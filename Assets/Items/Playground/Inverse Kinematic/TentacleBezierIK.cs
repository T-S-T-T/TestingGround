using UnityEngine;

/// <summary>
/// Version 2 — Bezier-Influenced FABRIK Tentacle IK.
///
/// Extends <see cref="TentacleIK"/> with an array of Control Point transforms
/// that shape the tentacle's curve, similar to a Bézier curve.
///
/// How it works:
///   1. FABRIK solves the chain toward <c>Target</c> exactly as in Version 1.
///   2. A generalised Bézier curve is evaluated using:
///         P0 = tentacle root
///         P1..Pn-1 = control points (in order)
///         Pn = target position
///   3. Each joint's FABRIK position is blended toward its corresponding point
///      on the Bézier curve by <c>BezierInfluence</c> (0 = pure FABRIK,
///      1 = full Bézier shape while still pinned to target).
///
/// Prefab workflow:
///   1. Create an empty GameObject, attach TentacleBezierIK.
///   2. Set Segment Count, Segment Length, and the number of Control Points.
///   3. Right-click ▶ "Generate Bones".
///   4. Create a "Target" GameObject and assign it.
///   5. Create one or more empty GameObjects as control points, assign them
///      to the Control Points array, and position them freely in the scene.
///   6. Adjust Bezier Influence to taste.
///   7. Save as a Prefab.
/// </summary>
[ExecuteAlways]
public class TentacleBezierIK : TentacleIK
{
    // ─── Bezier Settings ──────────────────────────────────────────────────────

    [Header("Bézier Influence")]
    [Tooltip("Transforms used as intermediate Bézier control points.\n" +
             "Order matters: P0 (root) → control points → Pn (target).\n" +
             "You can use 0 control points for a straight stretch toward the target.")]
    [SerializeField] private Transform[] controlPoints = new Transform[1];

    [Tooltip("0 = pure FABRIK (ignores control points).\n" +
             "1 = joints fully follow the Bézier curve shape.\n" +
             "Values in between blend both behaviours.")]
    [SerializeField, Range(0f, 1f)] private float bezierInfluence = 0.6f;

    // ─────────────────────────────────────────────────────────────────────────
    #region Bézier Post-Solve

    /// <summary>
    /// Called after FABRIK each frame. Blends every joint toward its
    /// corresponding point on the Bézier curve.
    /// The tip (last joint) is always pinned exactly to the target so that the
    /// tentacle still reaches its destination regardless of influence.
    /// </summary>
    protected override void PostSolve()
    {
        if (positions == null || positions.Length < 2) return;
        if (Mathf.Approximately(bezierInfluence, 0f))   return;

        // Build the full control-point list: root + user CPs + target
        Vector3 targetPos  = Target != null ? Target.position : positions[positions.Length - 1];
        Vector3[] curvePts = BuildCurvePoints(positions[0], targetPos);

        int n = positions.Length;

        // Blend interior joints; leave root (0) and tip (n-1) unchanged.
        for (int i = 1; i < n - 1; i++)
        {
            float   t          = (float)i / (n - 1);
            Vector3 bezierPos  = EvaluateBezier(curvePts, t);
            positions[i]       = Vector3.Lerp(positions[i], bezierPos, bezierInfluence);
        }

        // Always pin tip to target
        positions[n - 1] = targetPos;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Bézier Math

    /// <summary>
    /// Assembles the full ordered list of Bézier control points:
    /// [root, cp0, cp1, ..., cpN, target].
    /// Null entries in <c>controlPoints</c> are skipped gracefully.
    /// </summary>
    private Vector3[] BuildCurvePoints(Vector3 root, Vector3 targetPos)
    {
        // Count valid (non-null) control points
        int validCount = 0;
        if (controlPoints != null)
            foreach (var cp in controlPoints)
                if (cp != null) validCount++;

        // Total curve points: root + valid CPs + target
        var pts = new Vector3[validCount + 2];
        pts[0] = root;

        int idx = 1;
        if (controlPoints != null)
            foreach (var cp in controlPoints)
                if (cp != null) pts[idx++] = cp.position;

        pts[pts.Length - 1] = targetPos;
        return pts;
    }

    /// <summary>
    /// Evaluates a generalised Bézier curve at parameter <paramref name="t"/>
    /// using the De Casteljau algorithm. Works for any degree.
    /// </summary>
    /// <param name="pts">Ordered array of control points (including start and end).</param>
    /// <param name="t">Curve parameter in [0, 1].</param>
    /// <returns>World-space point on the curve.</returns>
    public static Vector3 EvaluateBezier(Vector3[] pts, float t)
    {
        int n = pts.Length;
        if (n == 1) return pts[0];

        // Work on a temporary copy to avoid modifying the source array
        var temp = new Vector3[n];
        System.Array.Copy(pts, temp, n);

        for (int r = 1; r < n; r++)
            for (int i = 0; i < n - r; i++)
                temp[i] = Vector3.Lerp(temp[i], temp[i + 1], t);

        return temp[0];
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Public Accessors

    public Transform[] ControlPoints    => controlPoints;
    public float        BezierInfluence => bezierInfluence;

    /// <summary>
    /// Samples the Bézier curve at <paramref name="t"/> ∈ [0,1] using the
    /// current root, control points, and target. Useful for editor gizmos.
    /// Returns <c>Vector3.zero</c> if no target is assigned.
    /// </summary>
    public Vector3 SampleCurve(float t)
    {
        if (Positions == null || Positions.Length == 0) return Vector3.zero;
        Vector3 targetPos = Target != null ? Target.position : Positions[Positions.Length - 1];
        return EvaluateBezier(BuildCurvePoints(transform.position, targetPos), t);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Context Menu

    [ContextMenu("Generate Bones")]
    public override void GenerateBones()
    {
        base.GenerateBones();
        Debug.Log("[TentacleBezierIK] Remember to assign Control Point transforms " +
                  "and a Target in the Inspector.");
    }

    #endregion
}
