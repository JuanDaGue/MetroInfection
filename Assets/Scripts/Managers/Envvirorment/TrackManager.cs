using System.Collections.Generic;
using UnityEngine;

public class TrackManager : EnvironmentManagerBase
{
    private TrackConfig config;
    private Queue<GameObject> segmentQueue = new Queue<GameObject>();
    private float currentEndZ = 0f;

    public override string ManagerType => "Track";

    public TrackManager(EnvironmentCoordinator coord, TrackConfig trackConfig) : base(coord)
    {
        config = trackConfig;
    }

    public override void Initialize()
    {
        // Calentar el pool
        // If your objectPool supports preloading, use the appropriate method here.
        // For example, if it has a Preload or Initialize method, use that instead.
        // objectPool.Preload(config.poolKey, config.initialSegmentCount + 5);

        // If not, you may need to manually spawn and recycle objects to warm up the pool:
        for (int i = 0; i < config.initialSegmentCount + 5; i++)
        {
            var temp = SpawnElement(config.poolKey, Vector3.zero, Quaternion.identity);
            if (temp != null)
                RecycleElement(temp);
        }

        for (int i = 0; i < config.initialSegmentCount; i++)
        {
            SpawnNewSegment(i * config.segmentLength);
        }
        currentEndZ = (config.initialSegmentCount - 1) * config.segmentLength;
    }

    public override void UpdateManager(float deltaTime)
    {
        float moveAmount = coordinator.MovementSpeed * deltaTime;
        
        foreach (var segment in segmentQueue)
        {
            if (segment != null)
                segment.transform.Translate(0, 0, -moveAmount, Space.World);
        }
    }

    public override void RecycleElements()
    {
        if (segmentQueue.Count == 0) return;

        GameObject firstSegment = segmentQueue.Peek();
        if (firstSegment != null && firstSegment.transform.position.z < config.recycleOffset)
        {
            segmentQueue.Dequeue();
            RecycleElement(firstSegment);

            currentEndZ += config.segmentLength;
            SpawnNewSegment(currentEndZ);
        }
    }

    private void SpawnNewSegment(float zPosition)
    {
        GameObject segment = SpawnElement(config.poolKey, new Vector3(0, 0, zPosition), Quaternion.identity);
        if (segment != null)
        {
            segmentQueue.Enqueue(segment);
        }
    }

    public float GetCurrentEndZ() => currentEndZ;
}