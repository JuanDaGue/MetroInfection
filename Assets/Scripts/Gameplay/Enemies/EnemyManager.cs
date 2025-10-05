using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public int enemiesPerWave = 20; // Set this to 20 as you requested
    public float timeBetweenWaves = 10f;
    public float spawnInterval = 1f; // Time between each enemy spawn in a wave

    [Header("Enemy Prefabs")]
    public GameObject[] enemyPrefabs; // Assign your different enemy prefabs in the Inspector
    public Transform[] spawnPoints; // Assign spawn points in your scene
    public List<Transform> Targets = new List<Transform>();
    private int currentWave = 0;
    private bool isWaveInProgress = false;
    private bool CanSpawningEnemies = true;
    public bool canSpawningEnemies
    {
        get { return CanSpawningEnemies; }
        set { CanSpawningEnemies = value; }
    }
    // void Start()
    // {
    //     // Start the first wave after a delay
    //     if (isEnemySpawning) return;
    //     isEnemySpawning = true;
    //     StartCoroutine(StartWaveAfterDelay(timeBetweenWaves));
    // }
    public void StartWave()
    {
        if (!canSpawningEnemies) return;

        else
        {
            Debug.Log("Enemy spawning is habilitated");
            StartCoroutine(StartWaveAfterDelay(timeBetweenWaves));
        }

    }
    IEnumerator StartWaveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        if (isWaveInProgress) yield break;

        isWaveInProgress = true;
        currentWave++;
        Debug.Log("Starting Wave: " + currentWave);

        int enemiesSpawned = 0;

        while (enemiesSpawned < enemiesPerWave)
        {
            // Spawn a random enemy from the prefab list
            SpawnEnemy();
            enemiesSpawned++;

            // Wait before spawning the next enemy
            yield return new WaitForSeconds(spawnInterval);
        }

        // Wait for all enemies to be defeated before starting the next wave.
        // You need to implement a way to track living enemies (e.g., via a list).
        yield return StartCoroutine(WaitForWaveToEnd());

        isWaveInProgress = false;
        // Start the next wave
        StartCoroutine(StartWaveAfterDelay(timeBetweenWaves));
    }

    void SpawnEnemy()
    {
        // Choose a random spawn point and a random enemy prefab
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemyToSpawn = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        var EnemyAI = Instantiate(enemyToSpawn, spawnPoint.position, spawnPoint.rotation);
        EnemyAI enemyAIComponent = EnemyAI.GetComponent<EnemyAI>();
        if (enemyAIComponent != null && Targets.Count > 0)
        {
            enemyAIComponent.SetTarget(Targets[Random.Range(0, Targets.Count)]);
        }
        //EnemyAI.SetTarget(Targets[Random.Range(0, Targets.Count)]);
        // TODO: Add the spawned enemy to a list to track them.
    }

    IEnumerator WaitForWaveToEnd()
    {
        // This is a placeholder. You need to check a list of active enemies.
        // Example: while (GameManager.Instance.enemies.Count > 0) { yield return null; }
        yield return new WaitForSeconds(5f); // Temporary wait
    }
}