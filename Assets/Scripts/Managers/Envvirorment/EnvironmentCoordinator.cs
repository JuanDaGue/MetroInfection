using System.Collections.Generic;
using UnityEngine;

// Principio de Responsabilidad Única: Coordina pero no implementa lógica específica
public class EnvironmentCoordinator : MonoBehaviour
{
    [Header("Configuración General")]
    public float centerX = 1.5f;
    public float movementSpeed = 5f;

    [Header("Configuraciones Específicas")]
    public TrackConfig trackConfig = new TrackConfig();
    public BuildingConfig buildingConfig = new BuildingConfig();
    public RoadConfig roadConfig = new RoadConfig();

    private List<IEnvironmentManager> managers = new List<IEnvironmentManager>();
    private TrackManager trackManager;
    private BuildingManager buildingManager;
    private RoadManager roadManager;

    // Propiedades públicas para acceso de los managers
    public float CenterX => centerX;
    public float MovementSpeed => movementSpeed;
    public float GetTrackEndZ() => trackManager?.GetCurrentEndZ() ?? 0f;

    void Start()
    {
        InitializeManagers();
        WarmUpPools();
    }

    private void InitializeManagers()
    {
        // Crear managers usando inyección de dependencias
        trackManager = new TrackManager(this, trackConfig);
        buildingManager = new BuildingManager(this, buildingConfig);
        roadManager = new RoadManager(this, roadConfig);

        managers.Add(trackManager);
        managers.Add(buildingManager);
        managers.Add(roadManager);

        // Inicializar todos los managers
        foreach (var manager in managers)
        {
            manager.Initialize();
        }
    }

    private void WarmUpPools()
    {
        // Calentar pools para mejor rendimiento inicial
        var objectPool = ObjectPool.GetPoolInstance();
        if (objectPool != null)
        {
            objectPool.WarmUp(50);
        }
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;

        foreach (var manager in managers)
        {
            manager.UpdateManager(deltaTime);
            manager.RecycleElements();
        }
    }

    public void SetEnvironmentSpeed(float newSpeed)
    {
        movementSpeed = newSpeed;
    }

    void OnDestroy()
    {
        // Limpieza
        foreach (var manager in managers)
        {
            if (manager is System.IDisposable disposable)
                disposable.Dispose();
        }
    }
}