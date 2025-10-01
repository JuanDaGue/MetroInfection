using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Very small object pool. Call Prewarm if you want to fill initially.
/// </summary>
public class SimplePool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int initialSize = 8;
    Queue<GameObject> pool = new Queue<GameObject>();

    public void Initialize(GameObject prefabPrefab, int size = 8)
    {
        prefab = prefabPrefab;
        initialSize = size;
        Prewarm(size);
    }

    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var go = CreateInstance();
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    GameObject CreateInstance()
    {
        if (prefab == null) return null;
        var go = Instantiate(prefab, transform);
        return go;
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject go = null;
        while (pool.Count > 0)
        {
            go = pool.Dequeue();
            if (go != null) break;
            go = null;
        }

        if (go == null) go = CreateInstance();
        if (go == null) return null;

        go.transform.position = position;
        go.transform.rotation = rotation;
        go.SetActive(true);
        return go;
    }

    public void Return(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        go.transform.SetParent(transform, false);
        pool.Enqueue(go);
    }
}
