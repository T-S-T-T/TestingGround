using System.Collections.Generic;
using UnityEngine;


public enum FormationType
{
    Grid,
    Diamond,
    Random,
    CenterFan
}
public class ClusterPosition1 : MonoBehaviour
{
    private List<Vector3> nodePositions = new List<Vector3>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //GenerateRectangle(100, transform.position + new Vector3(10, 10, 10), transform.forward, 5f, 3f, FormationType.Random);
        //GenerateCurvedRectangle(100, transform.position + new Vector3(10, 10, 10), transform.forward, 5f, 3f, 45f, 10f, FormationType.Diamond);
        GenerateCircle(100, transform.position + new Vector3(10, 10, 10), transform.forward, 5f, FormationType.Diamond);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void GenerateRectangle(int memberCount, Vector3 spawnPosition, Vector3 spawnDirection, float sizeX, float sizeY, FormationType formation)
    {
        nodePositions.Clear();

        // Orientation setup
        Vector3 forward = spawnDirection.normalized;
        Vector3 up = Vector3.up;
        if (Vector3.Dot(forward, up) > 0.99f)
            up = Vector3.right;

        Vector3 right = Vector3.Cross(up, forward).normalized;
        up = Vector3.Cross(forward, right).normalized;

        // Grid dimensions
        int rows = Mathf.CeilToInt(Mathf.Sqrt(memberCount));
        int cols = Mathf.CeilToInt((float)memberCount / rows);

        switch (formation)
        {
            case FormationType.Grid:
                GenerateGrid(memberCount, spawnPosition, right, up, sizeX, sizeY, rows, cols);
                break;

            case FormationType.Diamond:
                GenerateDiamond(memberCount, spawnPosition, right, up, sizeX, sizeY, rows, cols);
                break;

            case FormationType.Random:
                GenerateRandom(memberCount, spawnPosition, right, up, sizeX, sizeY);
                break;
        }
    }

    private void GenerateGrid(int memberCount, Vector3 center, Vector3 right, Vector3 up, float sizeX, float sizeY, int rows, int cols)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (nodePositions.Count >= memberCount) break;

                float x = (c / (float)(cols - 1) - 0.5f) * sizeX;
                float y = (r / (float)(rows - 1) - 0.5f) * sizeY;

                nodePositions.Add(center + right * x + up * y);
            }
        }
    }

    private void GenerateDiamond(int memberCount, Vector3 center, Vector3 right, Vector3 up, float sizeX, float sizeY, int rows, int cols)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (nodePositions.Count >= memberCount) break;

                // Normalized position in grid
                float x = (c / (float)(cols - 1) - 0.5f) * sizeX;
                float y = (r / (float)(rows - 1) - 0.5f) * sizeY;

                // Apply half-step offset for odd rows
                if (r % 2 == 1)
                {
                    float stepX = sizeX / (cols - 1); // horizontal spacing
                    x += stepX / 2f;
                }

                nodePositions.Add(center + right * x + up * y);
            }
        }
    }

    private void GenerateRandom(int memberCount, Vector3 center, Vector3 right, Vector3 up, float sizeX, float sizeY)
    {
        for (int i = 0; i < memberCount; i++)
        {
            float x = (Random.value - 0.5f) * sizeX;
            float y = (Random.value - 0.5f) * sizeY;

            nodePositions.Add(center + right * x + up * y);
        }
    }


    public void GenerateCurvedRectangle(int memberCount, Vector3 spawnPosition, Vector3 spawnDirection, float sizeX, float sizeY, float arcAngle, float radius, FormationType formation)
    {
        nodePositions.Clear();

        // Orientation setup
        Vector3 forward = spawnDirection.normalized;
        Vector3 up = Vector3.up;
        if (Vector3.Dot(forward, up) > 0.99f)
            up = Vector3.right;

        Vector3 right = Vector3.Cross(up, forward).normalized;
        up = Vector3.Cross(forward, right).normalized;

        // Grid dimensions
        int rows = Mathf.CeilToInt(Mathf.Sqrt(memberCount));
        int cols = Mathf.CeilToInt((float)memberCount / rows);

        switch (formation)
        {
            case FormationType.Grid:
                GenerateCurvedGrid(memberCount, spawnPosition, right, up, forward, sizeX, sizeY, rows, cols, arcAngle, radius);
                break;

            case FormationType.Diamond:
                GenerateCurvedDiamond(memberCount, spawnPosition, right, up, forward, sizeX, sizeY, rows, cols, arcAngle, radius);
                break;

            case FormationType.Random:
                GenerateCurvedRandom(memberCount, spawnPosition, right, up, forward, sizeX, sizeY, arcAngle, radius);
                break;
        }
    }

    private void GenerateCurvedGrid(int memberCount, Vector3 center, Vector3 right, Vector3 up, Vector3 forward, float sizeX, float sizeY, int rows, int cols, float arcAngle, float radius)
    {
        float angleStep = arcAngle / Mathf.Max(cols - 1, 1);
        float heightStep = sizeY / Mathf.Max(rows - 1, 1);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (nodePositions.Count >= memberCount) break;

                float angle = -arcAngle / 2f + c * angleStep;
                float heightOffset = -sizeY / 2f + r * heightStep;

                float rad = angle * Mathf.Deg2Rad;
                Vector3 arcOffset = (Mathf.Sin(rad) * radius) * right + (Mathf.Cos(rad) * radius - radius) * forward;
                Vector3 verticalOffset = up * heightOffset;

                nodePositions.Add(center + arcOffset + verticalOffset);
            }
        }
    }

    private void GenerateCurvedDiamond(int memberCount, Vector3 center, Vector3 right, Vector3 up, Vector3 forward, float sizeX, float sizeY, int rows, int cols, float arcAngle, float radius)
    {
        float angleStep = arcAngle / Mathf.Max(cols - 1, 1);
        float heightStep = sizeY / Mathf.Max(rows - 1, 1);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (nodePositions.Count >= memberCount) break;

                // stagger odd rows by half an angle step
                float angle = -arcAngle / 2f + c * angleStep + ((r % 2 == 1) ? angleStep / 2f : 0f);
                float heightOffset = -sizeY / 2f + r * heightStep;

                float rad = angle * Mathf.Deg2Rad;
                Vector3 arcOffset = (Mathf.Sin(rad) * radius) * right + (Mathf.Cos(rad) * radius - radius) * forward;
                Vector3 verticalOffset = up * heightOffset;

                nodePositions.Add(center + arcOffset + verticalOffset);
            }
        }
    }

    private void GenerateCurvedRandom(int memberCount, Vector3 center, Vector3 right, Vector3 up, Vector3 forward, float sizeX, float sizeY, float arcAngle, float radius)
    {
        for (int i = 0; i < memberCount; i++)
        {
            float angle = -arcAngle / 2f + Random.value * arcAngle;
            float heightOffset = -sizeY / 2f + Random.value * sizeY;

            float rad = angle * Mathf.Deg2Rad;
            Vector3 arcOffset = (Mathf.Sin(rad) * radius) * right + (Mathf.Cos(rad) * radius - radius) * forward;
            Vector3 verticalOffset = up * heightOffset;

            nodePositions.Add(center + arcOffset + verticalOffset);
        }
    }


    public void GenerateCircle(int memberCount,Vector3 spawnPosition,Vector3 spawnDirection,float radius,FormationType formation = FormationType.Grid)
    {
        nodePositions.Clear();

        // Orientation setup
        Vector3 forward = spawnDirection.normalized;
        Vector3 up = Vector3.up;
        if (Vector3.Dot(forward, up) > 0.99f)
            up = Vector3.right;

        Vector3 right = Vector3.Cross(up, forward).normalized;
        up = Vector3.Cross(forward, right).normalized;

        switch (formation)
        {
            case FormationType.Grid:
                GenerateCircleGrid(memberCount, spawnPosition, right, up, radius);
                break;

            case FormationType.Diamond:
                GenerateCircleDiamond(memberCount, spawnPosition, right, up, radius);
                break;

            case FormationType.Random:
                GenerateCircleRandom(memberCount, spawnPosition, right, up, radius);
                break;

            case FormationType.CenterFan:
                GenerateCircleCenterFan(memberCount, spawnPosition, right, up, radius);
                break;
        }
    }

    private void GenerateCircleGrid(int memberCount, Vector3 center, Vector3 right, Vector3 up, float radius)
    {
        for (int i = 0; i < memberCount; i++)
        {
            float angle = (i / (float)memberCount) * Mathf.PI * 2f;
            Vector3 pos = center + Mathf.Cos(angle) * right * radius + Mathf.Sin(angle) * up * radius;
            nodePositions.Add(pos);
        }
    }

    private void GenerateCircleDiamond(int memberCount, Vector3 center, Vector3 right, Vector3 up, float radius)
    {
        for (int i = 0; i < memberCount; i++)
        {
            float angle = ((i + (i % 2 == 1 ? 0.5f : 0f)) / (float)memberCount) * Mathf.PI * 2f;
            Vector3 pos = center + Mathf.Cos(angle) * right * radius + Mathf.Sin(angle) * up * radius;
            nodePositions.Add(pos);
        }
    }

    private void GenerateCircleRandom(int memberCount, Vector3 center, Vector3 right, Vector3 up, float radius)
    {
        for (int i = 0; i < memberCount; i++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            Vector3 pos = center + Mathf.Cos(angle) * right * radius + Mathf.Sin(angle) * up * radius;
            nodePositions.Add(pos);
        }
    }

    private void GenerateCircleCenterFan(int memberCount, Vector3 center, Vector3 right, Vector3 up, float radius)
    {
        if (memberCount <= 0) return;

        // Always put one node at the center
        nodePositions.Add(center);

        if (memberCount == 1) return;

        // Remaining nodes fan out evenly around the circle
        int outerCount = memberCount - 1;
        for (int i = 0; i < outerCount; i++)
        {
            float angle = (i / (float)outerCount) * Mathf.PI * 2f;
            Vector3 pos = center + Mathf.Cos(angle) * right * radius + Mathf.Sin(angle) * up * radius;
            nodePositions.Add(pos);
        }
    }



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
