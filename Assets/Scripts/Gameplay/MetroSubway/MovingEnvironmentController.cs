using System.Collections.Generic;
using UnityEngine;

public class MovingEnvironmentController : MonoBehaviour
{
    [Header("Configuración center")]
    public float centerX = 1.5f;
    [Header("Configuración de Vía")]

    public List<GameObject> trackPrefabs; // Arrastra tus prefabs de segmentos de vía aquí
    public int initialSegmentCount = 15; // Segmentos iniciales visibles
    public float segmentLength = 10f; // Longitud de cada segmento (debe coincidir con el diseño)
    public float movementSpeed = 5f; // Velocidad base del movimiento

    [Header("Configuración de Edificios")]
    public List<GameObject> buildingPrefabs; // Prefabs de edificios
    public float buildingSpawnDistance = 100f; // Distancia inicial de generación
    public float buildingRecycleDistance = -50f; // Distancia para reciclar edificios
    public Vector2 buildingXRange = new Vector2(-20f, 20f); // Rango en X para colocar edificios
    public Vector2 buildingSpacingRange = new Vector2(8f, 15f); // Espaciado entre edificios
    [Header("Road Segments")]
    public List<GameObject> roadPrefabs; // Prefabs de segmentos de carretera
    public float roadSegmentLength = 10f; // Longitud de cada segmento de carretera
    public float roadOffsetFromTrack = 10f; // Distancia de la carretera desde el centro de la vía
    public int initialRoadSegmentCount = 15;

    private Queue<GameObject> activeTrackSegments = new Queue<GameObject>();
    private List<GameObject> leftBuildings = new List<GameObject>();
    private List<GameObject> rightBuildings = new List<GameObject>();
    private Queue<GameObject> activeLeftRoadSegments = new Queue<GameObject>();
    private Queue<GameObject> activeRightRoadSegments = new Queue<GameObject>();
    private float currentTrackEndZ = 0f; // Seguimiento del final de la vía actual
    private float currentRoadEndZ = 0f;
    void Start()
    {
        InitializeTrack();
        InitializeBuildings();
        InitializeRoads();
    }

      void InitializeRoads()
    {
        // Genera segmentos de carretera iniciales a ambos lados de la vía
        for (int i = 0; i < initialRoadSegmentCount; i++)
        {
            SpawnNewRoadSegments(i * roadSegmentLength);
        }
        currentRoadEndZ = (initialRoadSegmentCount - 1) * roadSegmentLength;
    }

     void SpawnNewRoadSegments(float zPosition)
    {
        if (roadPrefabs.Count == 0)
        {
            Debug.LogError("No hay prefabs de carretera asignados!");
            return;
        }

        // Carretera izquierda
        GameObject leftRoadPrefab = roadPrefabs[Random.Range(0, roadPrefabs.Count)];
        GameObject leftRoadSegment = Instantiate(
            leftRoadPrefab, 
            new Vector3(-roadOffsetFromTrack + centerX, 0, zPosition),
            Quaternion.identity
        );
        activeLeftRoadSegments.Enqueue(leftRoadSegment);

        // Carretera derecha
        GameObject rightRoadPrefab = roadPrefabs[Random.Range(0, roadPrefabs.Count)];
        GameObject rightRoadSegment = Instantiate(
            rightRoadPrefab, 
            new Vector3(roadOffsetFromTrack + centerX, 0, zPosition),
            Quaternion.identity
        );
        activeRightRoadSegments.Enqueue(rightRoadSegment);
    }


    void Update()
    {
        MoveEnvironment();
        RecycleEnvironmentElements();
    }

    void InitializeTrack()
    {
        // Genera segmentos de vía iniciales
        for (int i = 0; i < initialSegmentCount; i++)
        {
            SpawnNewTrackSegment(i * segmentLength);
        }
        currentTrackEndZ = (initialSegmentCount - 1) * segmentLength;
    }

    void SpawnNewTrackSegment(float zPosition)
    {
        if (trackPrefabs.Count == 0)
        {
            Debug.LogError("No hay prefabs de vía asignados!");
            return;
        }

        GameObject segmentPrefab = trackPrefabs[Random.Range(0, trackPrefabs.Count)];
        GameObject newSegment = Instantiate(segmentPrefab, new Vector3(0, 0, zPosition), Quaternion.identity);
        activeTrackSegments.Enqueue(newSegment);
    }

    void InitializeBuildings()
    {
        // Genera edificios iniciales a ambos lados de la vía
        float currentZ = 0f;
        while (currentZ < buildingSpawnDistance)
        {
            SpawnBuildingRow(currentZ);
            currentZ += Random.Range(buildingSpacingRange.x, buildingSpacingRange.y);
        }
    }

    void SpawnBuildingRow(float zPosition)
    {
        // Edificios a la izquierda
        GameObject leftBuilding = Instantiate(
            buildingPrefabs[Random.Range(0, buildingPrefabs.Count)],
            new Vector3(Random.Range(-buildingXRange.y, -buildingXRange.x), 0, zPosition),
            Quaternion.identity
        );
        leftBuilding.transform.localScale = Vector3.one * Random.Range(0.9f, 1.1f); // Pequeña variación de escala
        leftBuilding.transform.Rotate(0, Random.Range(0, 360), 0); // Rotación aleatoria en Y
        leftBuildings.Add(leftBuilding);

        // Edificios a la derecha
        GameObject rightBuilding = Instantiate(
            buildingPrefabs[Random.Range(0, buildingPrefabs.Count)],
            new Vector3(Random.Range(buildingXRange.x, buildingXRange.y), 0, zPosition),
            Quaternion.identity
        );
        rightBuildings.Add(rightBuilding);
    }
    void MoveEnvironment()
    {
        float moveAmount = movementSpeed * Time.deltaTime;

        // Mueve todos los segmentos de vía
        foreach (GameObject segment in activeTrackSegments)
        {
            segment.transform.Translate(0, 0, -moveAmount, Space.World);
        }

        // Mueve todos los edificios
        MoveBuildingList(leftBuildings, -moveAmount);
        MoveBuildingList(rightBuildings, -moveAmount);
          MoveRoadSegments(activeLeftRoadSegments, -moveAmount);
        MoveRoadSegments(activeRightRoadSegments, -moveAmount);
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
            buildings[i].transform.Translate(0, 0, moveAmount, Space.World);
        }
    }
        void MoveRoadSegments(Queue<GameObject> roadSegments, float moveAmount)
    {
        foreach (GameObject segment in roadSegments)
        {
            segment.transform.Translate(0, 0, moveAmount, Space.World);
        }
    }

    void RecycleEnvironmentElements()
    {
        RecycleTracks();
        RecycleBuildings();
        RecycleRoads();
    }

    void RecycleTracks()
    {
        // Recicla segmentos de vía que quedaron detrás de la cámara
        GameObject firstSegment = activeTrackSegments.Peek();
        if (firstSegment.transform.position.z < -segmentLength)
        {
            activeTrackSegments.Dequeue();
            Destroy(firstSegment);

            // Genera un nuevo segmento al final
            currentTrackEndZ += segmentLength;
            SpawnNewTrackSegment(currentTrackEndZ);
        }
    }

    void RecycleBuildings()
    {
        // Recicla edificios que quedaron muy atrás y genera nuevos adelante
        RecycleBuildingList(leftBuildings);
        RecycleBuildingList(rightBuildings);

        // Asegura que haya suficientes edificios hacia adelante
        float farthestBuildingZ = GetFarthestBuildingZ();
        while (farthestBuildingZ < currentTrackEndZ + buildingSpawnDistance)
        {
            SpawnBuildingRow(farthestBuildingZ + Random.Range(buildingSpacingRange.x, buildingSpacingRange.y));
            farthestBuildingZ = GetFarthestBuildingZ();
        }
    }
void RecycleRoads()
    {
        // Recicla segmentos de carretera que quedaron detrás de la cámara
        if (activeLeftRoadSegments.Count > 0 && activeRightRoadSegments.Count > 0)
        {
            GameObject firstLeftSegment = activeLeftRoadSegments.Peek();
            GameObject firstRightSegment = activeRightRoadSegments.Peek();
            
            if (firstLeftSegment.transform.position.z < -roadSegmentLength)
            {
                activeLeftRoadSegments.Dequeue();
                Destroy(firstLeftSegment);
                
                activeRightRoadSegments.Dequeue();
                Destroy(firstRightSegment);

                // Genera nuevos segmentos de carretera al final
                currentRoadEndZ += roadSegmentLength;
                SpawnNewRoadSegments(currentRoadEndZ);
            }
        }
    }
    void RecycleBuildingList(List<GameObject> buildings)
    {
        for (int i = buildings.Count - 1; i >= 0; i--)
        {
            if (buildings[i] != null && buildings[i].transform.position.z < buildingRecycleDistance)
            {
                Destroy(buildings[i]);
                buildings.RemoveAt(i);
            }
        }
    }

    float GetFarthestBuildingZ()
    {
        float farthestZ = 0f;
        foreach (GameObject building in leftBuildings)
        {
            if (building != null && building.transform.position.z > farthestZ)
                farthestZ = building.transform.position.z;
        }
        foreach (GameObject building in rightBuildings)
        {
            if (building != null && building.transform.position.z > farthestZ)
                farthestZ = building.transform.position.z;
        }
        return farthestZ;
    }

    public void SetEnvironmentSpeed(float newSpeed)
    {
        movementSpeed = newSpeed;
    }
}