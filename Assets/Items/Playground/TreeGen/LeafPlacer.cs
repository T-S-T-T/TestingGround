using UnityEngine;

public static class LeafPlacer
{
    public static void Place(Transform treeRoot, TreeSettings cfg,
                             System.Random rng)
    {
        if (!cfg.generateLeaves || cfg.leafMaterial == null) return;

        // Gather all tip positions that were tagged during generation
        // We re-derive them by walking child transforms tagged "Tip"
        foreach (Transform tip in treeRoot.GetComponentsInChildren<Transform>())
        {
            if (!tip.CompareTag("Tip")) continue;

            for (int i = 0; i < cfg.leavesPerTip; i++)
            {
                var go = new GameObject("Leaf");
                go.transform.SetParent(tip, false);
                go.transform.localPosition = RandomOffset(rng, cfg.leafSize);
                go.transform.localRotation = Quaternion.Euler(
                    0, rng.Next(0, 360), 0);
                go.transform.localScale = Vector3.one * cfg.leafSize;

                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = MakeQuad();

                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = cfg.leafMaterial;
                mr.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }

    static Mesh MakeQuad()
    {
        var m = new Mesh();
        m.vertices = new[]
        {
            new Vector3(-0.5f, 0, 0), new Vector3( 0.5f, 0, 0),
            new Vector3(-0.5f, 1, 0), new Vector3( 0.5f, 1, 0)
        };
        m.uv = new[]
        {
            new Vector2(0,0), new Vector2(1,0),
            new Vector2(0,1), new Vector2(1,1)
        };
        m.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        m.RecalculateNormals();
        return m;
    }

    static Vector3 RandomOffset(System.Random rng, float size) =>
        new Vector3(
            (float)(rng.NextDouble() - 0.5) * size,
            (float)(rng.NextDouble() - 0.5) * size,
            (float)(rng.NextDouble() - 0.5) * size);
}