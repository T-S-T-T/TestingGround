using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager that spawns a configurable number of Boids and provides
/// a shared list so each Boid can iterate over its neighbours efficiently.
///
/// Usage
/// ─────
/// 1. Create an empty GameObject in your scene and attach this script.
/// 2. Assign a Prefab that has the <see cref="Boid"/> component (and a visible
///    mesh) to the <see cref="boidPrefab"/> field.
/// 3. Press Play – the manager spawns <see cref="boidCount"/> boids at random
///    positions inside <see cref="spawnRadius"/>.
/// </summary>
public class BoidManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static BoidManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Spawning")]
    [Tooltip("Prefab must have a Boid component attached.")]
    public GameObject boidPrefab;

    [Range(1, 500)]
    public int boidCount = 80;

    [Tooltip("Boids are spawned randomly within this sphere around the origin.")]
    public float spawnRadius = 10f;

    [Header("Global Boid Settings")]
    [Tooltip("When true, these values override the prefab's defaults at spawn time.")]
    public bool overridePrefabSettings = true;

    public float minSpeed          = 3f;
    public float maxSpeed          = 7f;
    public float maxForce          = 5f;
    public float perceptionRadius  = 5f;
    [Range(0f, 360f)]
    public float fieldOfView       = 270f;

    [Header("Rule Weights")]
    public float separationWeight  = 1.5f;
    public float alignmentWeight   = 1.0f;
    public float cohesionWeight    = 1.0f;

    [Header("Boundary")]
    public float boundaryRadius    = 25f;
    public float boundaryForce     = 6f;

    // ── Public data ───────────────────────────────────────────────────────────
    /// <summary>All active boids – read by each Boid every FixedUpdate.</summary>
    public IReadOnlyList<Boid> AllBoids => boids;

    // ── Private state ─────────────────────────────────────────────────────────
    private readonly List<Boid> boids = new List<Boid>();

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (boidPrefab == null)
        {
            Debug.LogError("[BoidManager] boidPrefab is not assigned!");
            return;
        }

        SpawnBoids();
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    private void SpawnBoids()
    {
        for (int i = 0; i < boidCount; i++)
        {
            Vector3    pos   = Random.insideUnitSphere * spawnRadius;
            Quaternion rot   = Random.rotation;
            GameObject go    = Instantiate(boidPrefab, pos, rot, transform);
            go.name          = $"Boid_{i:000}";

            Boid boid = go.GetComponent<Boid>();
            if (boid == null)
            {
                Debug.LogError("[BoidManager] Prefab is missing a Boid component.");
                Destroy(go);
                continue;
            }

            if (overridePrefabSettings)
                ApplySettings(boid);

            boids.Add(boid);
        }

        Debug.Log($"[BoidManager] Spawned {boids.Count} boids.");
    }

    private void ApplySettings(Boid b)
    {
        b.minSpeed          = minSpeed;
        b.maxSpeed          = maxSpeed;
        b.maxForce          = maxForce;
        b.perceptionRadius  = perceptionRadius;
        b.fieldOfView       = fieldOfView;
        b.separationWeight  = separationWeight;
        b.alignmentWeight   = alignmentWeight;
        b.cohesionWeight    = cohesionWeight;
        b.boundaryRadius    = boundaryRadius;
        b.boundaryForce     = boundaryForce;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Removes a boid from the simulation (e.g. on death).</summary>
    public void UnregisterBoid(Boid b)
    {
        boids.Remove(b);
    }
}
