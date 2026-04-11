using UnityEditor.Rendering;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TreeGenerator : MonoBehaviour
{
    [Tooltip("Assign a TreeSettings asset here")]
    public TreeSettings settings;

    [Header("Regenerate")]
    public bool autoGenerateOnStart = true;

    MeshFilter _mf;
    MeshRenderer _mr;

    void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();
    }

    void Start()
    {
        if (autoGenerateOnStart) Generate();
    }

    [ContextMenu("Generate Tree")]
    public void Generate()
    {
        if (settings == null)
        {
            Debug.LogError("[TreeGenerator] Assign a TreeSettings asset.", this);
            return;
        }

        // Remove old leaves
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        _mf.sharedMesh = BranchBuilder.Build(settings);
        _mr.sharedMaterial = settings.barkMaterial;

        // Leaves use a separate child renderer so they can have their own material
        var rng = new System.Random(settings.seed + 1);
        LeafPlacer.Place(transform, settings, rng);
    }

    // Handy for editor-time iteration
    void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && settings != null)
            UnityEditor.EditorApplication.delayCall += Generate;
#endif
    }
}