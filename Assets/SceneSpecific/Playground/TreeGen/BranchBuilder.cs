using System.Collections.Generic;
using UnityEngine;

public static class BranchBuilder
{
    static System.Random rng;
    static TreeSettings cfg;

    public static Mesh Build(TreeSettings settings)
    {
        cfg = settings;
        rng = new System.Random(settings.seed);

        var combines = new List<CombineInstance>();
        Recurse(Vector3.zero, Quaternion.identity,
                cfg.initialLength, cfg.initialRadius, 0, combines);

        var final = new Mesh { name = "ProceduralTree" };
        final.CombineMeshes(combines.ToArray(), true, true);
        final.RecalculateNormals();
        final.RecalculateBounds();
        return final;
    }

    static void Recurse(Vector3 origin, Quaternion orientation,
                        float length, float radius, int depth,
                        List<CombineInstance> combines)
    {
        if (depth > cfg.maxDepth || radius < 0.01f) return;

        // Vary length slightly
        float l = length * (1f + Variance(cfg.lengthVariance));
        Vector3 tip = origin + orientation * Vector3.up * l;

        // Build cylinder segment
        var seg = MakeCylinder(origin, tip, radius, radius * cfg.radiusScale);
        combines.Add(new CombineInstance
        {
            mesh = seg,
            transform = Matrix4x4.identity
        });

        if (depth == cfg.maxDepth) return;

        // Spawn child branches
        float goldenTwist = 0f;
        for (int i = 0; i < cfg.branchCount; i++)
        {
            float pitch = cfg.branchAngle + Variance(cfg.angleVariance) * cfg.branchAngle;
            float yaw = goldenTwist + i * (360f / cfg.branchCount);
            goldenTwist += cfg.twistAngle;

            Quaternion childRot = orientation
                * Quaternion.Euler(pitch, yaw, 0f);

            Recurse(tip, childRot,
                    length * cfg.lengthScale,
                    radius * cfg.radiusScale,
                    depth + 1, combines);
        }
    }

    static Mesh MakeCylinder(Vector3 bottom, Vector3 top,
                              float rBot, float rTop)
    {
        int segs = cfg.radialSegments;
        var verts = new Vector3[(segs + 1) * 2];
        var uvs = new Vector2[verts.Length];
        var tris = new List<int>();

        Quaternion rot = Quaternion.FromToRotation(Vector3.up, top - bottom);

        for (int i = 0; i <= segs; i++)
        {
            float t = i / (float)segs;
            float rad = t * Mathf.PI * 2f;
            Vector3 circle = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad));

            verts[i] = bottom + rot * (circle * rBot);
            verts[i + segs + 1] = top + rot * (circle * rTop);
            uvs[i] = new Vector2(t, 0);
            uvs[i + segs + 1] = new Vector2(t, 1);
        }

        for (int i = 0; i < segs; i++)
        {
            int a = i, b = i + 1, c = i + segs + 1, d = i + segs + 2;
            tris.AddRange(new[] { a, c, b, b, c, d });
        }

        var m = new Mesh();
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        return m;
    }

    static float Variance(float amount) =>
        (float)(rng.NextDouble() * 2 - 1) * amount;
}