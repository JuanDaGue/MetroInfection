using UnityEngine;
using System.Collections.Generic;

// Principio de Segregación de Interfaces: Interfaces específicas
public interface IEnvironmentElement
{
    void Initialize();
    void UpdateElement(float deltaTime);
    void Recycle();
    bool ShouldRecycle();
    Vector3 Position { get; }
}

public interface IEnvironmentManager
{
    void Initialize();
    void UpdateManager(float deltaTime);
    void RecycleElements();
    string ManagerType { get; }
}

public interface IMovableElement
{
    void Move(Vector3 movement);
    void SetPosition(Vector3 position);
}

// Data classes para configuración (Principio de Responsabilidad Única)
[System.Serializable]
public class TrackConfig
{
    public string poolKey = "track";
    public List<GameObject> prefabs;
    public int initialSegmentCount = 15;
    public float segmentLength = 10f;
    public float recycleOffset = -10f;
}

[System.Serializable]
public class BuildingConfig
{
    public string poolKey = "building";
    public List<GameObject> prefabs;
    public float spawnDistance = 100f;
    public float recycleDistance = -50f;
    public Vector2 xRange = new Vector2(-20f, 20f);
    public Vector2 spacingRange = new Vector2(8f, 15f);
}

[System.Serializable]
public class RoadConfig
{
    public string poolKey = "road";
    public List<GameObject> prefabs;
    public float segmentLength = 10f;
    public float offsetFromTrack = 10f;
    public int initialSegmentCount = 15;
    public float recycleOffset = -10f;
}