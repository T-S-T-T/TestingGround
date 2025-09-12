using System.Collections;
using UnityEngine;

public class TreeBodyScript : MonoBehaviour
{
    private float minSpawnDelay = 0.01f;
    private float maxSpawnDelay = 0.5f;
    private float growStartDelay = 5f;
    private float growInterval = 2f;

    [Header("Spawn Settings")]
    public Transform spawnPoint; // assign your SpawnPoint child here
    public GameObject Test;      // assign your Test prefab here
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SpawnRoutine());
        StartCoroutine(GrowRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
        yield return new WaitForSeconds(delay);
        Spawn();
    }

    IEnumerator GrowRoutine()
    {
        yield return new WaitForSeconds(growStartDelay);

        while (true)
        {
            Grow();
            yield return new WaitForSeconds(growInterval);
        }
    }

    void Spawn()
    {
        if (Test == null || spawnPoint == null)
        {
            Debug.LogWarning("Test prefab or spawnPoint not assigned", this);
            return;
        }

        // Base rotation plus small random Euler offset on each axis
        Vector3 baseAngles = transform.rotation.eulerAngles;
        Vector3 randomOffset = new Vector3(
            Random.Range(-10, 10f),
            Random.Range(0, 90f),
            Random.Range(-10, 10f)
        );
        Quaternion spawnRotation = Quaternion.Euler(baseAngles + randomOffset);

        Instantiate(Test, spawnPoint.position, spawnRotation);
        Debug.Log($"Spawned at {Time.time:F2}s with offset {randomOffset}");
    }

    void Grow()
    {
        Debug.Log("Grow");
    }
}
