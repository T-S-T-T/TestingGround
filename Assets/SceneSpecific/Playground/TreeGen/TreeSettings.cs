using UnityEngine;

[CreateAssetMenu(fileName = "TreeSettings", menuName = "Procedural/Tree Settings")]
public class TreeSettings : ScriptableObject
{
    [Header("Structure")]
    [Range(1, 8)] public int maxDepth = 5;
    [Range(2, 5)] public int branchCount = 3;
    public float initialLength = 4f;
    public float initialRadius = 0.3f;

    [Header("Reduction per level")]
    [Range(0.4f, 0.9f)] public float lengthScale = 0.65f;
    [Range(0.4f, 0.9f)] public float radiusScale = 0.60f;

    [Header("Angles")]
    [Range(10f, 60f)] public float branchAngle = 30f;
    public float twistAngle = 137.5f;   // golden angle – natural spread

    [Header("Randomness")]
    public int seed = 42;
    [Range(0f, 1f)] public float angleVariance = 0.2f;
    [Range(0f, 1f)] public float lengthVariance = 0.15f;

    [Header("Leaves")]
    public bool generateLeaves = true;
    [Range(1, 6)] public int leavesPerTip = 3;
    public float leafSize = 0.4f;
    public Material leafMaterial;

    [Header("Bark")]
    public Material barkMaterial;
    [Range(4, 12)] public int radialSegments = 6;
}