using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Scene Management")]
    [SerializeField] private string initialScene = "MetroMoveScene";
    [SerializeField] private string[] scenesToLoad;


    // Singleton for easy access
    public static SceneLoader Instance { get; private set; }

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
        }
    }

    void Start()
    {
        // Load initial scene if specified
        if (!string.IsNullOrEmpty(initialScene))
        {
            LoadAdditive(initialScene);
        }
    }

    public void LoadAdditive(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Scene name is null or empty");
            return;
        }
        StartCoroutine(LoadAdditiveCoroutine(sceneName));
    }

    private IEnumerator LoadAdditiveCoroutine(string sceneName)
    {
        // Check if scene is already loaded
        Scene targetScene = SceneManager.GetSceneByName(sceneName);
        if (targetScene.IsValid() && targetScene.isLoaded)
        {
            Debug.Log($"Scene {sceneName} is already loaded");
            yield break;
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Set the newly loaded scene as active
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid())
        {
            SceneManager.SetActiveScene(loadedScene);
            Debug.Log($"Loaded and activated scene: {sceneName}");
        }
    }

    public void UnloadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) 
        {
            Debug.LogWarning("Scene name is null or empty");
            return;
        }
        StartCoroutine(UnloadCoroutine(sceneName));
    }

    private IEnumerator UnloadCoroutine(string sceneName)
    {
        Scene targetScene = SceneManager.GetSceneByName(sceneName);
        
        if (!targetScene.IsValid() || !targetScene.isLoaded)
        {
            Debug.Log($"Scene {sceneName} is not loaded, skipping unload");
            yield break;
        }

        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);
        
        while (asyncUnload != null && !asyncUnload.isDone)
        {
            yield return null;
        }

        Debug.Log($"Unloaded scene: {sceneName}");
    }

    public void SwitchScenes(string sceneToLoad, string sceneToUnload)
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
            LoadAdditive(sceneToLoad);
            
        if (!string.IsNullOrEmpty(sceneToUnload))
            UnloadScene(sceneToUnload);
    }

    public bool IsSceneLoaded(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        return scene.IsValid() && scene.isLoaded;
    }
}