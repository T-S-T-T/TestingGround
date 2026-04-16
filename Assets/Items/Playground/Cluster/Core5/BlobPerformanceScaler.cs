using UnityEngine;

/// <summary>
/// Optional runtime component that monitors frame rate and advises the user
/// (or optionally auto-destroys excess particles) so the simulation stays
/// above a target FPS.
///
/// Attach to the same GameObject as <see cref="BlobController"/>.
/// This component is entirely optional; the blob works without it.
///
/// NOTE: Unity does not support removing individual Rigidbodies mid-play
/// without side effects, so this scaler works by disabling the BlobParticle
/// MonoBehaviour on surplus particles (stopping their force application)
/// and making them kinematic, then destroys them cleanly.
/// </summary>
public class BlobPerformanceScaler : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Minimum acceptable frame rate. Below this, particles are culled.")]
    [SerializeField] private float targetFPS = 45f;

    [Tooltip("FPS headroom before re-enabling particles (avoids thrashing).")]
    [SerializeField] private float fpsHysteresis = 10f;

    [Tooltip("How often (seconds) to sample FPS and take action.")]
    [SerializeField] private float sampleInterval = 2f;

    [Tooltip("Maximum particles to remove per sample interval.")]
    [SerializeField] private int   cullBatchSize  = 20;

    [Tooltip("Minimum particle count. Scaler won't go below this.")]
    [SerializeField] private int   minParticles   = 50;

    [Header("Auto-Action")]
    [Tooltip("If true, the scaler automatically culls particles. " +
             "If false it only logs warnings.")]
    [SerializeField] private bool autoScale = true;

    // ──────────────────────────────────────────────────────────────────────

    private BlobController blob;
    private float          sampleTimer;
    private float          fpsAccumulator;
    private int            fpsFrames;

    private void Awake()
    {
        blob = GetComponent<BlobController>();
        if (blob == null)
        {
            Debug.LogWarning("[BlobPerformanceScaler] No BlobController found.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        // Accumulate instantaneous FPS
        fpsAccumulator += 1f / Time.unscaledDeltaTime;
        fpsFrames++;

        sampleTimer += Time.unscaledDeltaTime;
        if (sampleTimer < sampleInterval) return;

        float avgFPS = fpsAccumulator / fpsFrames;
        fpsAccumulator = 0f;
        fpsFrames      = 0;
        sampleTimer    = 0f;

        if (avgFPS < targetFPS)
        {
            string msg = $"[BlobPerformanceScaler] FPS={avgFPS:F1} below target {targetFPS}. " +
                         $"Particles={blob.ParticleCount}.";

            if (autoScale && blob.ParticleCount > minParticles)
            {
                int toRemove = Mathf.Min(cullBatchSize,
                                         blob.ParticleCount - minParticles);
                CullParticles(toRemove);
                Debug.Log(msg + $" Culled {toRemove} particles.");
            }
            else
            {
                Debug.LogWarning(msg + " Consider reducing particle count in BlobSettings.");
            }
        }
    }

    /// <summary>
    /// Disables and destroys the last <paramref name="count"/> particles
    /// in the list.
    /// </summary>
    private void CullParticles(int count)
    {
        var parts = blob.GetParticles();

        for (int i = 0; i < count && i < parts.Count; i++)
        {
            int idx = parts.Count - 1 - i;
            var p   = parts[idx];
            if (p == null) continue;

            // Make kinematic before destroy to avoid physics glitches
            p.Rigidbody.isKinematic = true;
            Destroy(p.gameObject);
        }
    }
}
