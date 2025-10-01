using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader I;
    void Awake() { if (I==null) { I=this; DontDestroyOnLoad(gameObject);} else Destroy(gameObject); }

    public IEnumerator LoadLevelAdditive(string sceneName) {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;
        // Optionally find spawn points, Init level, etc.
    }

    public IEnumerator UnloadLevel(string sceneName) {
        var op = SceneManager.UnloadSceneAsync(sceneName);
        while (!op.isDone) yield return null;
    }
}
