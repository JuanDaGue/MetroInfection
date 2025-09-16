using System.Collections.Generic;
using UnityEngine;

public class MovingEnvironmentController : MonoBehaviour
{
    [Header("Track Settings")]
    public List<GameObject> trackPrefabs; // Assign your track segment prefabs
    public int initialSegmentCount = 10;
    public float segmentLength = 10f;
    public float movementSpeed = 5f;

    [Header("Building Settings")]
    public List<GameObject> buildingPrefabs; // Assign your building prefabs
    public int buildingRows = 3;
    public float buildingSpacing = 15f;
    public Vector2 buildingOffsetRange = new Vector2(-10f, 10f);

    private Queue<GameObject> activeTrackSegments = new Queue<GameObject>();
    private List<GameObject> leftBuildings = new List<GameObject>();
    private List<GameObject> rightBuildings = new List<GameObject>();
    private float currentTrackPosition = 0f;

    void Start()
    {
        InitializeTrack();
        InitializeBuildings();
    }

    void Update()
    {
        MoveEnvironment();
        ManageSegmentRecycling();
    }

    void InitializeTrack()
    {
        for (int i = 0; i < initialSegmentCount; i++)
        {
            GameObject segment = Instantiate(
                trackPrefabs[Random.Range(0, trackPrefabs.Count)],
                new Vector3(0, 0, i * segmentLength),
                Quaternion.identity
            );
            activeTrackSegments.Enqueue(segment);
            currentTrackPosition = (initialSegmentCount - 1) * segmentLength;
        }
    }

    void InitializeBuildings()
    {
        // Create buildings on both sides of the track
        for (int row = 0; row < buildingRows; row++)
        {
            for (int side = -1; side <= 1; side += 2) // -1 for left, 1 for right
            {
                if (side == 0) continue;

                GameObject building = Instantiate(
                    buildingPrefabs[Random.Range(0, buildingPrefabs.Count)],
                    new Vector3(
                        side * buildingSpacing + Random.Range(buildingOffsetRange.x, buildingOffsetRange.y),
                        0,
                        row * buildingSpacing
                    ),
                    Quaternion.identity
                );

                if (side < 0)
                    leftBuildings.Add(building);
                else
                    rightBuildings.Add(building);
            }
        }
    }

    void MoveEnvironment()
    {
        float moveAmount = movementSpeed * Time.deltaTime;

        // Move all track segments
        foreach (GameObject segment in activeTrackSegments)
        {
            segment.transform.Translate(0, 0, -moveAmount);
        }

        // Move all buildings
        MoveBuildingList(leftBuildings, -moveAmount);
        MoveBuildingList(rightBuildings, -moveAmount);
    }

    void MoveBuildingList(List<GameObject> buildings, float moveAmount)
    {
        for (int i = buildings.Count - 1; i >= 0; i--)
        {
            if (buildings[i] == null)
            {
                buildings.RemoveAt(i);
                continue;
            }

            buildings[i].transform.Translate(0, 0, moveAmount);

            // Recycle buildings that move behind the train
            if (buildings[i].transform.position.z < -50f)
            {
                buildings[i].transform.position = new Vector3(
                    buildings[i].transform.position.x,
                    0,
                    currentTrackPosition + Random.Range(20f, 40f)
                );
            }
        }
    }

    void ManageSegmentRecycling()
    {
        if (activeTrackSegments.Peek().transform.position.z < -segmentLength)
        {
            GameObject oldSegment = activeTrackSegments.Dequeue();
            oldSegment.transform.position = new Vector3(0, 0, currentTrackPosition + segmentLength);
            activeTrackSegments.Enqueue(oldSegment);
            currentTrackPosition += segmentLength;
        }
    }

    public void SetSpeed(float newSpeed)
    {
        movementSpeed = newSpeed;
    }
}