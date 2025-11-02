using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;


public enum FormationType
{
    Grid,
    Diamond,
    Random,
    CenterFan
}
public class ClusterPosition1 : MonoBehaviour
{

    private ClusterSpawn1 clusterSpawn1;

    private List<Vector3> nodePositions = new List<Vector3>();

    private int nodeCount;
    private float timer = 10f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        clusterSpawn1 = GetComponent<ClusterSpawn1>();

        //GenerateRectangle(100, transform.position + new Vector3(10, 10, 10), transform.forward, 5f, 3f, FormationType.Random);
        //GenerateCurvedRectangle(100, transform.position + new Vector3(10, 10, 10), transform.forward, 5f, 3f, 45f, 10f, FormationType.Diamond);
        //GenerateCircleArea(1000, transform.position + new Vector3(10, 10, 10), transform.forward, 5f, FormationType.Diamond);
        //GenerateParaboloid(1000, transform.position + new Vector3(10, 10, 10), transform.forward, 5f, 10f, FormationType.Diamond);
        //GenerateCone(1000, transform.position + new Vector3(10, 10, 10), transform.forward, 5f, 10f, FormationType.CenterFan);

        StartCoroutine(CycleFormations());
    }

    // Update is called once per frame
    void Update()
    {
        if (clusterSpawn1 != null)
        {
            nodeCount = clusterSpawn1.MemberCount;
        }
    }

    IEnumerator CycleFormations()
    {
        Vector3 basePosition = transform.position + new Vector3(0, 0, 10);
        Vector3 direction = transform.forward;

        FormationType[] modes = new FormationType[]
        {
            FormationType.Grid,
            FormationType.Diamond,
            FormationType.Random,
            FormationType.CenterFan
        };

        while (true)
        {
            foreach (var mode in modes)
            {
                GenerateRectangle(nodeCount, basePosition, direction, 5f, 3f, mode);
                AssignNode();
                yield return new WaitForSeconds(timer);

                GenerateCurvedRectangle(nodeCount, basePosition, direction, 5f, 3f, 45f, 10f, mode);
                AssignNode();
                yield return new WaitForSeconds(timer);

                GenerateCircleArea(nodeCount, basePosition, direction, 5f, mode);
                AssignNode();
                yield return new WaitForSeconds(timer);

                GenerateParaboloid(nodeCount, basePosition, direction, 5f, 10f, mode);
                AssignNode();
                yield return new WaitForSeconds(timer);

                GenerateCone(nodeCount, basePosition, direction, 5f, 10f, mode);
                AssignNode();
                yield return new WaitForSeconds(timer);
            }
        }
    }

    public void AssignNode()
    {

        List<GameObject> members = clusterSpawn1.members;
        // Use the smaller count to avoid mismatches
        int count = Mathf.Min(nodePositions.Count, members.Count);

        // Make a working copy of nodes so we can remove them as we assign
        List<Vector3> availableNodes = new List<Vector3>(nodePositions);

        for (int i = 0; i < count; i++)
        {
            GameObject member = members[i];
            if (member == null) continue;

            MemberMovement2 movement = member.GetComponent<MemberMovement2>();
            if (movement == null) continue;

            // Find the closest available node
            Vector3 closestNode = availableNodes[0];
            float closestDist = Vector3.Distance(member.transform.position, closestNode);

            foreach (Vector3 node in availableNodes)
            {
                float dist = Vector3.Distance(member.transform.position, node);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestNode = node;
                }
            }

            // Assign node and move
            movement.node = closestNode;
            //movement.MoveToNode();

            // Remove this node so no one else can use it
            availableNodes.Remove(closestNode);
        }
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


    public void GenerateCircleArea(int memberCount, Vector3 spawnPosition, Vector3 spawnDirection, float radius, FormationType formation = FormationType.Grid)
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
                GenerateCircleAreaGrid(memberCount, spawnPosition, right, up, radius);
                break;

            case FormationType.Diamond:
                GenerateCircleAreaDiamond(memberCount, spawnPosition, right, up, radius);
                break;

            case FormationType.Random:
                GenerateCircleAreaRandom(memberCount, spawnPosition, right, up, radius);
                break;

            case FormationType.CenterFan:
                GenerateCircleAreaCenterFan(memberCount, spawnPosition, right, up, radius);
                break;
        }
    }

    private void GenerateCircleAreaGrid(int memberCount, Vector3 center, Vector3 right, Vector3 up, float radius)
    {
        int rings = Mathf.CeilToInt(Mathf.Sqrt(memberCount)); // number of concentric rings
        int placed = 0;

        for (int r = 0; r < rings; r++)
        {
            float ringRadius = (r / (float)(rings - 1)) * radius;
            int pointsInRing = (r == 0) ? 1 : Mathf.CeilToInt(2 * Mathf.PI * ringRadius / (radius / rings));

            for (int i = 0; i < pointsInRing; i++)
            {
                if (placed >= memberCount) return;

                if (r == 0)
                {
                    nodePositions.Add(center); // center node
                }
                else
                {
                    float angle = (i / (float)pointsInRing) * Mathf.PI * 2f;
                    Vector3 pos = center + Mathf.Cos(angle) * right * ringRadius + Mathf.Sin(angle) * up * ringRadius;
                    nodePositions.Add(pos);
                }
                placed++;
            }
        }
    }

    private void GenerateCircleAreaDiamond(int memberCount, Vector3 center, Vector3 right, Vector3 up, float radius)
    {
        int rings = Mathf.CeilToInt(Mathf.Sqrt(memberCount));
        int placed = 0;

        for (int r = 0; r < rings; r++)
        {
            float ringRadius = (r / (float)(rings - 1)) * radius;
            int pointsInRing = (r == 0) ? 1 : Mathf.CeilToInt(2 * Mathf.PI * ringRadius / (radius / rings));

            for (int i = 0; i < pointsInRing; i++)
            {
                if (placed >= memberCount) return;

                float offset = (r % 2 == 1) ? 0.5f : 0f; // stagger odd rings
                float angle = ((i + offset) / (float)pointsInRing) * Mathf.PI * 2f;

                if (r == 0)
                {
                    nodePositions.Add(center);
                }
                else
                {
                    Vector3 pos = center + Mathf.Cos(angle) * right * ringRadius + Mathf.Sin(angle) * up * ringRadius;
                    nodePositions.Add(pos);
                }
                placed++;
            }
        }
    }

    private void GenerateCircleAreaRandom(int memberCount, Vector3 center, Vector3 right, Vector3 up, float radius)
    {
        for (int i = 0; i < memberCount; i++)
        {
            // Use sqrt(Random) to ensure uniform density across area
            float r = radius * Mathf.Sqrt(Random.value);
            float angle = Random.value * Mathf.PI * 2f;

            Vector3 pos = center + Mathf.Cos(angle) * right * r + Mathf.Sin(angle) * up * r;
            nodePositions.Add(pos);
        }
    }

    private void GenerateCircleAreaCenterFan(int memberCount, Vector3 center, Vector3 right, Vector3 up, float radius)
    {
        if (memberCount <= 0) return;

        nodePositions.Add(center); // always one in the middle
        if (memberCount == 1) return;

        int spokes = Mathf.CeilToInt(Mathf.Sqrt(memberCount));
        int rings = Mathf.CeilToInt((float)memberCount / spokes);

        int placed = 1;
        for (int r = 1; r < rings; r++)
        {
            float ringRadius = (r / (float)(rings - 1)) * radius;
            for (int s = 0; s < spokes; s++)
            {
                if (placed >= memberCount) return;

                float angle = (s / (float)spokes) * Mathf.PI * 2f;
                Vector3 pos = center + Mathf.Cos(angle) * right * ringRadius + Mathf.Sin(angle) * up * ringRadius;
                nodePositions.Add(pos);
                placed++;
            }
        }
    }


    public void GenerateParaboloid(int memberCount, Vector3 spawnPosition, Vector3 spawnDirection, float radius, float height, FormationType formation = FormationType.Grid)
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
                GenerateParaboloidGrid(memberCount, spawnPosition, right, up, forward, radius, height);
                break;

            case FormationType.Diamond:
                GenerateParaboloidDiamond(memberCount, spawnPosition, right, up, forward, radius, height);
                break;

            case FormationType.Random:
                GenerateParaboloidRandom(memberCount, spawnPosition, right, up, forward, radius, height);
                break;

            case FormationType.CenterFan:
                GenerateParaboloidCenterFan(memberCount, spawnPosition, right, up, forward, radius, height);
                break;
        }
    }

    private void GenerateParaboloidGrid(int memberCount, Vector3 center, Vector3 right, Vector3 up, Vector3 forward, float radius, float height)
    {
        int rings = Mathf.CeilToInt(Mathf.Sqrt(memberCount));
        int placed = 0;

        for (int r = 0; r < rings; r++)
        {
            float t = r / (float)(rings - 1);
            float ringRadius = t * radius;
            float z = (t * t) * height; // paraboloid curve

            int pointsInRing = (r == 0) ? 1 : Mathf.CeilToInt(2 * Mathf.PI * ringRadius / (radius / rings));

            for (int i = 0; i < pointsInRing; i++)
            {
                if (placed >= memberCount) return;

                if (r == 0)
                {
                    nodePositions.Add(center); // apex
                }
                else
                {
                    float angle = (i / (float)pointsInRing) * Mathf.PI * 2f;
                    Vector3 pos = center
                        + right * Mathf.Cos(angle) * ringRadius
                        + up * Mathf.Sin(angle) * ringRadius
                        + forward * z;
                    nodePositions.Add(pos);
                }
                placed++;
            }
        }
    }

    private void GenerateParaboloidDiamond(int memberCount, Vector3 center, Vector3 right, Vector3 up, Vector3 forward, float radius, float height)
    {
        int rings = Mathf.CeilToInt(Mathf.Sqrt(memberCount));
        int placed = 0;

        for (int r = 0; r < rings; r++)
        {
            float t = r / (float)(rings - 1);
            float ringRadius = t * radius;
            float z = (t * t) * height;

            int pointsInRing = (r == 0) ? 1 : Mathf.CeilToInt(2 * Mathf.PI * ringRadius / (radius / rings));

            for (int i = 0; i < pointsInRing; i++)
            {
                if (placed >= memberCount) return;

                float offset = (r % 2 == 1) ? 0.5f : 0f;
                float angle = ((i + offset) / (float)pointsInRing) * Mathf.PI * 2f;

                if (r == 0)
                {
                    nodePositions.Add(center);
                }
                else
                {
                    Vector3 pos = center
                        + right * Mathf.Cos(angle) * ringRadius
                        + up * Mathf.Sin(angle) * ringRadius
                        + forward * z;
                    nodePositions.Add(pos);
                }
                placed++;
            }
        }
    }

    private void GenerateParaboloidRandom(int memberCount, Vector3 center, Vector3 right, Vector3 up, Vector3 forward, float radius, float height)
    {
        for (int i = 0; i < memberCount; i++)
        {
            float r = radius * Mathf.Sqrt(Random.value); // uniform in disk
            float angle = Random.value * Mathf.PI * 2f;
            float t = r / radius;
            float z = (t * t) * height;

            Vector3 pos = center
                + right * Mathf.Cos(angle) * r
                + up * Mathf.Sin(angle) * r
                + forward * z;

            nodePositions.Add(pos);
        }
    }

    private void GenerateParaboloidCenterFan(int memberCount, Vector3 center, Vector3 right, Vector3 up, Vector3 forward, float radius, float height)
    {
        if (memberCount <= 0) return;

        nodePositions.Add(center); // apex
        if (memberCount == 1) return;

        int spokes = Mathf.CeilToInt(Mathf.Sqrt(memberCount));
        int rings = Mathf.CeilToInt((float)memberCount / spokes);

        int placed = 1;
        for (int r = 1; r < rings; r++)
        {
            float t = r / (float)(rings - 1);
            float ringRadius = t * radius;
            float z = (t * t) * height;

            for (int s = 0; s < spokes; s++)
            {
                if (placed >= memberCount) return;

                float angle = (s / (float)spokes) * Mathf.PI * 2f;
                Vector3 pos = center
                    + right * Mathf.Cos(angle) * ringRadius
                    + up * Mathf.Sin(angle) * ringRadius
                    + forward * z;

                nodePositions.Add(pos);
                placed++;
            }
        }
    }


    public void GenerateCone(int memberCount, Vector3 spawnPosition, Vector3 spawnDirection, float radius, float height, FormationType formation = FormationType.Grid)
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
                GenerateConeGrid(memberCount, spawnPosition, right, up, forward, radius, height);
                break;

            case FormationType.Diamond:
                GenerateConeDiamond(memberCount, spawnPosition, right, up, forward, radius, height);
                break;

            case FormationType.Random:
                GenerateConeRandom(memberCount, spawnPosition, right, up, forward, radius, height);
                break;

            case FormationType.CenterFan:
                GenerateConeCenterFan(memberCount, spawnPosition, right, up, forward, radius, height);
                break;
        }
    }

    private void GenerateConeGrid(int memberCount, Vector3 center, Vector3 right, Vector3 up, Vector3 forward, float radius, float height)
    {
        int rings = Mathf.CeilToInt(Mathf.Sqrt(memberCount));
        int placed = 0;

        for (int r = 0; r < rings; r++)
        {
            float t = r / (float)(rings - 1);   // 0 at apex, 1 at base
            float ringRadius = (1 - t) * radius;
            float z = t * height;

            int pointsInRing = (r == 0) ? 1 : Mathf.CeilToInt(2 * Mathf.PI * ringRadius / (radius / rings));

            for (int i = 0; i < pointsInRing; i++)
            {
                if (placed >= memberCount) return;

                if (r == 0)
                {
                    nodePositions.Add(center); // apex
                }
                else
                {
                    float angle = (i / (float)pointsInRing) * Mathf.PI * 2f;
                    Vector3 pos = center
                        + right * Mathf.Cos(angle) * ringRadius
                        + up * Mathf.Sin(angle) * ringRadius
                        + forward * z;
                    nodePositions.Add(pos);
                }
                placed++;
            }
        }
    }

    private void GenerateConeDiamond(int memberCount, Vector3 center, Vector3 right, Vector3 up, Vector3 forward, float radius, float height)
    {
        int rings = Mathf.CeilToInt(Mathf.Sqrt(memberCount));
        int placed = 0;

        for (int r = 0; r < rings; r++)
        {
            float t = r / (float)(rings - 1);
            float ringRadius = (1 - t) * radius;
            float z = t * height;

            int pointsInRing = (r == 0) ? 1 : Mathf.CeilToInt(2 * Mathf.PI * ringRadius / (radius / rings));

            for (int i = 0; i < pointsInRing; i++)
            {
                if (placed >= memberCount) return;

                float offset = (r % 2 == 1) ? 0.5f : 0f;
                float angle = ((i + offset) / (float)pointsInRing) * Mathf.PI * 2f;

                if (r == 0)
                {
                    nodePositions.Add(center);
                }
                else
                {
                    Vector3 pos = center
                        + right * Mathf.Cos(angle) * ringRadius
                        + up * Mathf.Sin(angle) * ringRadius
                        + forward * z;
                    nodePositions.Add(pos);
                }
                placed++;
            }
        }
    }

    private void GenerateConeRandom(int memberCount, Vector3 center, Vector3 right, Vector3 up, Vector3 forward, float radius, float height)
    {
        for (int i = 0; i < memberCount; i++)
        {
            float t = Random.value; // height fraction
            float ringRadius = (1 - t) * radius;
            float z = t * height;

            float angle = Random.value * Mathf.PI * 2f;
            Vector3 pos = center
                + right * Mathf.Cos(angle) * ringRadius
                + up * Mathf.Sin(angle) * ringRadius
                + forward * z;

            nodePositions.Add(pos);
        }
    }

    private void GenerateConeCenterFan(int memberCount, Vector3 center, Vector3 right, Vector3 up, Vector3 forward, float radius, float height)
    {
        if (memberCount <= 0) return;

        nodePositions.Add(center); // apex
        if (memberCount == 1) return;

        int spokes = Mathf.CeilToInt(Mathf.Sqrt(memberCount));
        int rings = Mathf.CeilToInt((float)memberCount / spokes);

        int placed = 1;
        for (int r = 1; r < rings; r++)
        {
            float t = r / (float)(rings - 1);
            float ringRadius = (1 - t) * radius;
            float z = t * height;

            for (int s = 0; s < spokes; s++)
            {
                if (placed >= memberCount) return;

                float angle = (s / (float)spokes) * Mathf.PI * 2f;
                Vector3 pos = center
                    + right * Mathf.Cos(angle) * ringRadius
                    + up * Mathf.Sin(angle) * ringRadius
                    + forward * z;

                nodePositions.Add(pos);
                placed++;
            }
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
