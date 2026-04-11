using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Version 1 — Simple FABRIK Tentacle IK.
///
/// Attach this component to an empty GameObject (the tentacle root).
/// Right-click the component header and choose "Generate Bones" to build
/// the bone chain. Assign a Target transform — the tentacle will reach
/// toward it every frame using FABRIK (Forward And Backward Reaching IK).
///
/// Prefab workflow:
///   1. Create an empty GameObject, attach TentacleIK.
///   2. Tune Segment Count and Segment Length.
///   3. Right-click ▶ "Generate Bones".
///   4. Create a separate "Target" GameObject and assign it.
///   5. Save as a Prefab.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class TentacleIK : MonoBehaviour
{
    // ─── Structure ────────────────────────────────────────────────────────────

    [Header("Tentacle Structure")]
    [Tooltip("Number of bone segments. The chain will have SegmentCount + 1 joints.")]
    [SerializeField, Min(2)] protected int segmentCount = 8;

    [Tooltip("World-space length of each segment.")]
    [SerializeField, Min(0.01f)] protected float segmentLength = 0.5f;

    // ─── IK ───────────────────────────────────────────────────────────────────

    [Header("IK")]
    [Tooltip("The transform the tentacle tip will try to reach.")]
    [SerializeField] protected Transform target;

    [Tooltip("Maximum FABRIK iterations per frame. Higher = more accurate but slower.")]
    [SerializeField, Range(1, 30)] protected int maxIterations = 10;

    [Tooltip("Stop iterating once the tip is within this distance from the target.")]
    [SerializeField, Min(0f)] protected float tolerance = 0.001f;

    // ─── Visuals ──────────────────────────────────────────────────────────────

    [Header("Visuals")]
    [Tooltip("LineRenderer width at the root (base) of the tentacle.")]
    [SerializeField] protected float baseWidth = 0.15f;

    [Tooltip("LineRenderer width at the tip of the tentacle.")]
    [SerializeField] protected float tipWidth = 0.02f;

    // ─── Runtime state ────────────────────────────────────────────────────────

    protected Transform[] bones;
    protected Vector3[]   positions;
    protected LineRenderer lineRenderer;

    // Width curve is rebuilt only when base/tip widths change
    private float cachedBaseWidth = -1f;
    private float cachedTipWidth  = -1f;

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity Messages

    protected virtual void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        InitializeBones();
    }

    protected virtual void OnEnable()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        InitializeBones();
    }

    protected virtual void LateUpdate()
    {
        if (bones == null || bones.Length == 0) return;

        SyncPositionsFromBones();

        if (target != null)
            SolveFABRIK(target.position);

        PostSolve();

        ApplyPositionsToBones();
        UpdateLineRenderer();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region IK Core

    /// <summary>
    /// FABRIK solver. Modifies the <c>positions</c> array in place so the
    /// tip converges toward <paramref name="targetPosition"/>.
    /// </summary>
    protected void SolveFABRIK(Vector3 targetPosition)
    {
        int   n           = positions.Length;
        float totalLength = segmentLength * (n - 1);

        // If the target is farther than the fully-extended chain, just stretch.
        if ((targetPosition - positions[0]).sqrMagnitude >= totalLength * totalLength)
        {
            Vector3 dir = (targetPosition - positions[0]).normalized;
            for (int i = 1; i < n; i++)
                positions[i] = positions[i - 1] + dir * segmentLength;
            return;
        }

        Vector3 rootAnchor = positions[0];

        for (int iter = 0; iter < maxIterations; iter++)
        {
            // ── Forward pass (tip → root) ──────────────────────────────────
            positions[n - 1] = targetPosition;
            for (int i = n - 2; i >= 0; i--)
            {
                Vector3 dir = (positions[i] - positions[i + 1]).normalized;
                positions[i] = positions[i + 1] + dir * segmentLength;
            }

            // ── Backward pass (root → tip) ─────────────────────────────────
            positions[0] = rootAnchor;
            for (int i = 1; i < n; i++)
            {
                Vector3 dir = (positions[i] - positions[i - 1]).normalized;
                positions[i] = positions[i - 1] + dir * segmentLength;
            }

            // ── Convergence check ──────────────────────────────────────────
            if ((positions[n - 1] - targetPosition).sqrMagnitude < tolerance * tolerance)
                break;
        }
    }

    /// <summary>
    /// Called every frame after FABRIK is solved, before positions are
    /// written back to the bone transforms. Override in subclasses to add
    /// extra deformation (e.g. Bezier blending).
    /// </summary>
    protected virtual void PostSolve() { }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Bone Sync

    /// <summary>Copies current bone world positions into the <c>positions</c> array.</summary>
    protected void SyncPositionsFromBones()
    {
        for (int i = 0; i < positions.Length; i++)
            positions[i] = bones[i].position;
    }

    /// <summary>Writes solved positions back to bone transforms and orients each bone.</summary>
    protected void ApplyPositionsToBones()
    {
        for (int i = 0; i < bones.Length; i++)
        {
            bones[i].position = positions[i];

            if (i < bones.Length - 1)
            {
                Vector3 dir = positions[i + 1] - positions[i];
                if (dir.sqrMagnitude > 0.0001f)
                    bones[i].rotation = Quaternion.LookRotation(dir);
            }
            else if (i > 0)
            {
                bones[i].rotation = bones[i - 1].rotation; // tip copies parent
            }
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region LineRenderer

    private void UpdateLineRenderer()
    {
        if (lineRenderer == null) return;

        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);

        // Rebuild width curve only when values actually changed
        if (!Mathf.Approximately(baseWidth, cachedBaseWidth) ||
            !Mathf.Approximately(tipWidth,  cachedTipWidth))
        {
            lineRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0f, baseWidth),
                new Keyframe(1f, tipWidth)
            );
            cachedBaseWidth = baseWidth;
            cachedTipWidth  = tipWidth;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Bone Initialisation

    /// <summary>
    /// Searches for existing "Bone_N" children. Falls back to any children if
    /// the expected naming is not found. Call after Awake or after generation.
    /// </summary>
    protected void InitializeBones()
    {
        var found = new List<Transform>();
        for (int i = 0; i <= segmentCount; i++)
        {
            Transform b = transform.Find("Bone_" + i);
            if (b != null) found.Add(b);
        }

        if (found.Count == segmentCount + 1)
        {
            bones = found.ToArray();
        }
        else if (transform.childCount >= 2)
        {
            // Graceful fallback: use whatever children exist
            bones = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                bones[i] = transform.GetChild(i);
        }

        if (bones != null && bones.Length > 0)
            positions = new Vector3[bones.Length];
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Prefab Setup (Editor)

    /// <summary>
    /// Destroys any existing "Bone_N" children and rebuilds the chain from
    /// scratch using the current <c>segmentCount</c> and <c>segmentLength</c>.
    /// Also configures the LineRenderer. Run this from the Inspector context menu.
    /// </summary>
    [ContextMenu("Generate Bones")]
    public virtual void GenerateBones()
    {
        // Remove old bones
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Bone_"))
                DestroyImmediate(child.gameObject);
        }

        bones     = new Transform[segmentCount + 1];
        positions = new Vector3[segmentCount + 1];

        for (int i = 0; i <= segmentCount; i++)
        {
            var go = new GameObject("Bone_" + i);
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0f, i * segmentLength);
            bones[i]     = go.transform;
            positions[i] = go.transform.position;
        }

        ConfigureLineRenderer();

        Debug.Log($"[TentacleIK] Generated {segmentCount + 1} joints | " +
                  $"segmentLength = {segmentLength} | " +
                  $"total reach = {segmentCount * segmentLength:F2}");
    }

    protected void ConfigureLineRenderer()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace    = true;
        lineRenderer.positionCount    = segmentCount + 1;
        lineRenderer.numCapVertices   = 5;
        lineRenderer.numCornerVertices = 5;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        if (lineRenderer.sharedMaterial == null)
            lineRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Public Accessors

    public int        SegmentCount  => segmentCount;
    public float      SegmentLength => segmentLength;
    public Transform  Target        => target;
    public Transform[] Bones        => bones;
    public Vector3[]   Positions    => positions;

    #endregion
}
