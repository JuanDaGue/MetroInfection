using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : EnvironmentManagerBase
{
    private BuildingConfig config;
    private List<GameObject> leftBuildings = new List<GameObject>();
    private List<GameObject> rightBuildings = new List<GameObject>();
    private float nextSpawnZ = 0f;

    public override string ManagerType => "Building";

    public BuildingManager(EnvironmentCoordinator coord, BuildingConfig buildingConfig) : base(coord)
    {
        config = buildingConfig;
    }

    public override void Initialize()
    {
        objectPool.WarmUpPool(config.poolKey, 30); // Calentar con edificios iniciales

        nextSpawnZ = 0f;
        while (nextSpawnZ < config.spawnDistance)
        {
            SpawnBuildingRow(nextSpawnZ);
            nextSpawnZ += Random.Range(config.spacingRange.x, config.spacingRange.y);
        }
    }

    public override void UpdateManager(float deltaTime)
    {
        float moveAmount = coordinator.MovementSpeed * deltaTime;

        MoveBuildingList(leftBuildings, -moveAmount);
        MoveBuildingList(rightBuildings, -moveAmount);
    }

    public override void RecycleElements()
    {
        RecycleBuildingList(leftBuildings);
        RecycleBuildingList(rightBuildings);

        // Spawn nuevos edificios si es necesario
        float farthestZ = GetFarthestBuildingZ();
        float trackEndZ = coordinator.GetTrackEndZ();

        while (farthestZ < trackEndZ + config.spawnDistance)
        {
            SpawnBuildingRow(farthestZ + Random.Range(config.spacingRange.x, config.spacingRange.y));
            farthestZ = GetFarthestBuildingZ();
        }
    }

    private void SpawnBuildingRow(float zPosition)
    {
        // Edificio izquierdo
        float leftX = Random.Range(-config.xRange.y, -config.xRange.x);
        GameObject leftBuilding = SpawnElement(config.poolKey, 
            new Vector3(leftX, 0, zPosition), Quaternion.identity);
        
        if (leftBuilding != null)
        {
            leftBuilding.transform.localScale = Vector3.one * Random.Range(0.9f, 1.1f);
            leftBuilding.transform.Rotate(0, Random.Range(0, 360), 0);
            leftBuildings.Add(leftBuilding);
        }

        // Edificio derecho
        float rightX = Random.Range(config.xRange.x, config.xRange.y);
        GameObject rightBuilding = SpawnElement(config.poolKey,
            new Vector3(rightX, 0, zPosition), Quaternion.identity);
        
        if (rightBuilding != null)
        {
            rightBuildings.Add(rightBuilding);
        }
    }

    private void MoveBuildingList(List<GameObject> buildings, float moveAmount)
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

    private void RecycleBuildingList(List<GameObject> buildings)
    {
        for (int i = buildings.Count - 1; i >= 0; i--)
        {
            if (buildings[i] != null && buildings[i].transform.position.z < config.recycleDistance)
            {
                RecycleElement(buildings[i]);
                buildings.RemoveAt(i);
            }
        }
    }

    private float GetFarthestBuildingZ()
    {
        float farthestZ = nextSpawnZ;
        
        foreach (var building in leftBuildings)
        {
            if (building != null && building.transform.position.z > farthestZ)
                farthestZ = building.transform.position.z;
        }
        
        foreach (var building in rightBuildings)
        {
            if (building != null && building.transform.position.z > farthestZ)
                farthestZ = building.transform.position.z;
        }
        
        return farthestZ;
    }
}