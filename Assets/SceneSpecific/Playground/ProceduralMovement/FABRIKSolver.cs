using UnityEngine;

/// <summary>
/// FABRIKSolver — attach to the leg root bone.
/// Solves a chain of N bones so the tip reaches a target position.
/// Works for 2-bone (coxa → femur → tibia) or 3-bone chains.
/// 
/// HOW IT WORKS
/// FABRIK (Forward And Backward Reaching IK) alternates two passes:
///   Forward pass  — pull tip to target, propagate up the chain.
///   Backward pass — re-anchor root, propagate back down.
/// 5 iterations is plenty for spider legs.
/// </summary>
public class FABRIKSolver : MonoBehaviour
{
    [Header("Chain")]
    [Tooltip("Assign bones from root to tip (e.g. Coxa, Femur, Tibia, Foot).")]
    public Transform[] bones;

    [Header("Target")]
    [Tooltip("The LegStepController for this leg — we read FootPosition from it.")]
    public LegStepController legStep;

    [Header("Solver")]
    public int iterations = 10;
    public float tolerance = 0.001f;

    [Header("Pole Target (optional)")]
    [Tooltip("Optional hint Transform to keep the knee bending in a consistent direction.")]
    public Transform poleTarget;
    public float poleWeight = 0.3f;

    // Cached bone lengths
    private float[] boneLengths;
    private float totalLength;
    private Vector3[] positions;

    void Awake()
    {
        if (bones == null || bones.Length < 2)
        {
            Debug.LogError($"[FABRIKSolver] {name}: need at least 2 bones.", this);
            enabled = false;
            return;
        }

        int n = bones.Length;
        boneLengths = new float[n - 1];
        positions   = new Vector3[n];
        totalLength = 0f;

        for (int i = 0; i < n - 1; i++)
        {
            boneLengths[i] = Vector3.Distance(bones[i].position, bones[i + 1].position);
            totalLength    += boneLengths[i];
        }
    }

    void LateUpdate()
    {
        if (legStep == null || bones == null || bones.Length < 2) return;

        SolveIK(legStep.FootPosition);
    }

    void SolveIK(Vector3 target)
    {
        int n = bones.Length;

        // 1. Copy current world positions into working array
        for (int i = 0; i < n; i++)
            positions[i] = bones[i].position;

        Vector3 rootPos = positions[0];

        // 2. If target is out of reach, stretch the chain straight toward it
        float distToTarget = Vector3.Distance(rootPos, target);
        if (distToTarget >= totalLength)
        {
            Vector3 dir = (target - rootPos).normalized;
            for (int i = 1; i < n; i++)
                positions[i] = positions[i - 1] + dir * boneLengths[i - 1];
        }
        else
        {
            // 3. Iterate FABRIK passes until converged or max iterations reached
            for (int iter = 0; iter < iterations; iter++)
            {
                // Forward pass: pull tip to target
                positions[n - 1] = target;
                for (int i = n - 2; i >= 0; i--)
                {
                    Vector3 dir = (positions[i] - positions[i + 1]).normalized;
                    positions[i] = positions[i + 1] + dir * boneLengths[i];
                }

                // Backward pass: re-anchor root
                positions[0] = rootPos;
                for (int i = 1; i < n; i++)
                {
                    Vector3 dir = (positions[i] - positions[i - 1]).normalized;
                    positions[i] = positions[i - 1] + dir * boneLengths[i - 1];
                }

                // Early exit if tip is close enough
                if (Vector3.Distance(positions[n - 1], target) < tolerance)
                    break;
            }
        }

        // 4. Optional: pull mid-bone toward pole target to control knee direction
        if (poleTarget != null && n >= 3)
        {
            int mid = n / 2;
            positions[mid] = Vector3.Lerp(
                positions[mid],
                poleTarget.position,
                poleWeight);
        }

        // 5. Apply solved positions back to bone transforms
        for (int i = 0; i < n - 1; i++)
        {
            // Point this bone toward the next solved position
            Vector3 dir = (positions[i + 1] - positions[i]).normalized;
            if (dir == Vector3.zero) continue;

            bones[i].rotation = Quaternion.LookRotation(
                dir,
                bones[i].up   // preserve the bone's existing up-axis as much as possible
            );

            // Keep the world position locked (LookRotation shifts it slightly)
            bones[i].position = positions[i];
        }

        // Final tip bone: match tip to target exactly
        bones[n - 1].position = positions[n - 1];
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (bones == null) return;
        Gizmos.color = Color.green;
        for (int i = 0; i < bones.Length - 1; i++)
        {
            if (bones[i] != null && bones[i + 1] != null)
                Gizmos.DrawLine(bones[i].position, bones[i + 1].position);
        }
        if (legStep != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(legStep.FootPosition, 0.04f);
        }
    }
#endif
}
