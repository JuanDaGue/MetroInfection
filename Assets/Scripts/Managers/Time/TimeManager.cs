using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private float timeOnStation = 100f;
    [SerializeField] private float timeInTravel = 100f;
    
    [Header("Scene Names")]
    [SerializeField] private string stationSceneName = "StationScene";
    [SerializeField] private string travelSceneName = "MetroMoveScene";
    [SerializeField] private EnemyManager enemyManager;
    
    private bool isOnStation = false;
    private float currentTime = 0f;
    
    // Reference to SceneLoader
    private SceneLoader sceneLoader;

    // Singleton for easy access
    public static TimeManager Instance { get; private set; }

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Get reference to SceneLoader
        sceneLoader = SceneLoader.Instance;
        if (sceneLoader == null)
        {
            Debug.LogError("SceneLoader instance not found!");
            return;
        }

        InitializeTime();
    }

    private void InitializeTime()
    {
        currentTime = timeInTravel;
        isOnStation = false;
        
        // Load initial scene
        sceneLoader.SwitchScenes(travelSceneName, stationSceneName);
    }

    void Update()
    {
        UpdateTimer();
    }

    private void UpdateTimer()
    {
        if (currentTime <= 0) return;
        
        currentTime -= Time.deltaTime;
        
        if (currentTime <= 0)
        {
            SwitchLocation();
        }
    }

    private void SwitchLocation()
    {
        isOnStation = !isOnStation;

        if (isOnStation)
        {
            // Switching to station
            sceneLoader.SwitchScenes(stationSceneName, travelSceneName);
            currentTime = timeOnStation;
            Debug.Log("Switched to Station");
            // Start enemy spawning
            enemyManager.canSpawningEnemies = true;
            enemyManager.StartWave();
        }
        else
        {
            // Switching to travel
            sceneLoader.SwitchScenes(travelSceneName, stationSceneName);
            currentTime = timeInTravel;
            Debug.Log("Switched to Travel");
        }
    }

    // Public methods for time management
    public void AddTime(float time)
    {
        currentTime += time;
        Debug.Log($"Added {time} seconds. Current time: {currentTime}");
    }

    public float GetCurrentTime()
    {
        return currentTime;
    }

    public bool IsOnStation()
    {
        return isOnStation;
    }

    public float GetTimeOnStation()
    {
        return timeOnStation;
    }

    public float GetTimeInTravel()
    {
        return timeInTravel;
    }

    public void SetTimeOnStation(float time)
    {
        timeOnStation = Mathf.Max(0, time);
        if (isOnStation)
            currentTime = timeOnStation;
    }

    public void SetTimeInTravel(float time)
    {
        timeInTravel = Mathf.Max(0, time);
        if (!isOnStation)
            currentTime = timeInTravel;
    }

    public void SetCurrentTime(float time)
    {
        currentTime = Mathf.Max(0, time);
    }

    // Method to manually trigger scene switch (for debugging or special events)
    public void ForceSwitchLocation()
    {
        SwitchLocation();
    }

    // Get current location name for UI etc.
    public string GetCurrentLocationName()
    {
        return isOnStation ? "Station" : "Traveling";
    }
}