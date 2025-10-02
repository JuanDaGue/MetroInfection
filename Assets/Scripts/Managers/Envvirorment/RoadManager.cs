using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class RoadManager : EnvironmentManagerBase
{
    private RoadConfig config;
    private Queue<GameObject> leftRoadSegments = new Queue<GameObject>();
    private Queue<GameObject> rightRoadSegments = new Queue<GameObject>();
    private float currentEndZ = 0f;
    
    // Unity's ObjectPool for road segments
    private IObjectPool<GameObject> roadSegmentPool;

    public override void UpdateManager(float deltaTime)
    {
        // Implement logic to update the road manager each frame if needed
        // For example, you might want to call RecycleElements here
        RecycleElements();
    }

    public override string ManagerType => "Road";

    public RoadManager(EnvironmentCoordinator coord, RoadConfig roadConfig) : base(coord)
    {
        config = roadConfig;
        InitializeUnityPool();
    }

    private void InitializeUnityPool()
    {
        roadSegmentPool = new ObjectPool<GameObject>(
            createFunc: () => CreateRoadSegment(),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Object.Destroy(obj),
            collectionCheck: true, // Prevents double-returning objects
            defaultCapacity: config.initialSegmentCount * 2 + 10,
            maxSize: 100
        );
    }

    private GameObject CreateRoadSegment()
    {
        if (config.prefabs.Count == 0) return null;
        
        GameObject prefab = config.prefabs[Random.Range(0, config.prefabs.Count)];
        GameObject segment = UnityEngine.Object.Instantiate(prefab);
        
        // Add your PoolableObject component for tracking
        var poolable = segment.GetComponent<PoolableObject>() ?? segment.AddComponent<PoolableObject>();
        // You might need to adapt this for Unity's pool
        return segment;
    }

    public override void Initialize()
    {
        // Warm up the pool
        List<GameObject> warmUpObjects = new List<GameObject>();
        int warmUpCount = config.initialSegmentCount * 2 + 10;
        
        for (int i = 0; i < warmUpCount; i++)
        {
            warmUpObjects.Add(roadSegmentPool.Get());
        }
        
        // Return them immediately to fill the pool
        foreach (var obj in warmUpObjects)
        {
            roadSegmentPool.Release(obj);
        }

        // Initialize your road segments
        for (int i = 0; i < config.initialSegmentCount; i++)
        {
            SpawnNewRoadSegments(i * config.segmentLength);
        }
        currentEndZ = (config.initialSegmentCount - 1) * config.segmentLength;
    }

    private void SpawnNewRoadSegments(float zPosition)
    {
        // Carretera izquierda
        GameObject leftSegment = roadSegmentPool.Get();
        if (leftSegment != null) 
        {
            leftSegment.transform.position = new Vector3(-config.offsetFromTrack + coordinator.CenterX, 0, zPosition);
            leftSegment.transform.rotation = Quaternion.identity;
            leftRoadSegments.Enqueue(leftSegment);
        }

        // Carretera derecha
        GameObject rightSegment = roadSegmentPool.Get();
        if (rightSegment != null) 
        {
            rightSegment.transform.position = new Vector3(config.offsetFromTrack + coordinator.CenterX, 0, zPosition);
            rightSegment.transform.rotation = Quaternion.identity;
            rightRoadSegments.Enqueue(rightSegment);
        }
    }

    // Update your RecycleElements to use Release instead of RecycleElement
    public override void RecycleElements()
    {
        if (leftRoadSegments.Count > 0 && rightRoadSegments.Count > 0)
        {
            GameObject firstLeft = leftRoadSegments.Peek();
            GameObject firstRight = rightRoadSegments.Peek();

            if (firstLeft != null && firstLeft.transform.position.z < config.recycleOffset)
            {
                leftRoadSegments.Dequeue();
                rightRoadSegments.Dequeue();

                // Use Unity's pool release instead of your RecycleElement
                roadSegmentPool.Release(firstLeft);
                roadSegmentPool.Release(firstRight);

                currentEndZ += config.segmentLength;
                SpawnNewRoadSegments(currentEndZ);
            }
        }
    }
}