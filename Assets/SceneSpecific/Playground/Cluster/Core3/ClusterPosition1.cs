using System.Collections.Generic;
using UnityEngine;

public class ClusterPosition1 : MonoBehaviour
{
    private List<Vector3> nodePositions = new List<Vector3>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateRectangle(100, transform.position + new Vector3(10,10,10), transform.forward, 5f, 3f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Generates evenly spaced nodes in a rectangle.
    /// </summary>
    /// <param name="memberCount">Number of nodes</param>
    /// <param name="spawnPosition">Center of rectangle</param>
    /// <param name="spawnDirection">Forward direction of rectangle</param>
    /// <param name="sizeX">Width of rectangle</param>
    /// <param name="sizeY">Height of rectangle</param>
    public void GenerateRectangle(int memberCount, Vector3 spawnPosition, Vector3 spawnDirection, float sizeX, float sizeY)
    {
        nodePositions.Clear();

        // Normalize direction and build orientation
        Vector3 forward = spawnDirection.normalized;
        Vector3 up = Vector3.up;
        if (Vector3.Dot(forward, up) > 0.99f) // avoid collinearity
            up = Vector3.right;

        Vector3 right = Vector3.Cross(up, forward).normalized;
        up = Vector3.Cross(forward, right).normalized;

        // Rectangle corners
        Vector3 center = spawnPosition;
        Vector3 halfRight = right * (sizeX / 2f);
        Vector3 halfUp = up * (sizeY / 2f);

        // Distribute nodes evenly across the rectangle
        int rows = Mathf.CeilToInt(Mathf.Sqrt(memberCount));
        int cols = Mathf.CeilToInt((float)memberCount / rows);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (nodePositions.Count >= memberCount) break;

                float x = (c / (float)(cols - 1) - 0.5f) * sizeX;
                float y = (r / (float)(rows - 1) - 0.5f) * sizeY;

                Vector3 pos = center + right * x + up * y;
                nodePositions.Add(pos);
            }
        }
    }

    // Draw gizmos to visualize nodes
    private void OnDrawGizmos()
    {
        if (nodePositions == null) return;

        Gizmos.color = Color.cyan;
        foreach (var pos in nodePositions)
        {
            Gizmos.DrawSphere(pos, 0.1f);
        }
    }
}
