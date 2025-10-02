using System.Collections.Generic;
using UnityEngine;

// Principio de Responsabilidad Única: Solo se encarga del pooling
public interface IObjectPool
{
    GameObject GetObject(string poolKey); // FIXED: Added poolKey parameter
    void ReturnObject(GameObject obj);
    void WarmUp(int count);
    void WarmUpPool(string poolKey, int count);
}

// Componente para identificar objetos poolables
public class PoolableObject : MonoBehaviour
{
    public string PoolKey { get; set; }

    void OnDisable()
    {
        // Auto-regresar al pool cuando se desactiva
        if (!string.IsNullOrEmpty(PoolKey) && ObjectPool.Instance != null)
        {
            // FIXED: Check if object is already in pool before returning
            if (!ObjectPool.Instance.IsObjectInPool(this.gameObject))
            {
                ObjectPool.Instance.ReturnObject(this.gameObject);
            }
        }
    }
}

// Principio de Abierto/Cerrado: Se puede extender para diferentes tipos de pools
public class ObjectPool : MonoBehaviour, IObjectPool
{
    [System.Serializable]
    public class PoolConfig
    {
        public string poolKey;
        public GameObject prefab;
        public int initialSize = 10;
        public int maxSize = 50;
        public Transform parent;
    }

    [Header("Configuración de Pools")]
    public List<PoolConfig> poolConfigs = new List<PoolConfig>();

    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, PoolConfig> configDictionary;
    private Dictionary<string, int> activeObjectsCount;
    private HashSet<GameObject> objectsCurrentlyInPool = new HashSet<GameObject>(); // NEW: Tracks pooled objects

    public static ObjectPool Instance { get; private set; }

    // NEW: Helper method to check if object is already in a pool
    public bool IsObjectInPool(GameObject obj)
    {
        return objectsCurrentlyInPool.Contains(obj);
    }

    // Principio de Inversión de Dependencias: Dependemos de la interfaz
    public static IObjectPool GetPoolInstance() => Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializePools();
    }

    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        configDictionary = new Dictionary<string, PoolConfig>();
        activeObjectsCount = new Dictionary<string, int>();

        foreach (var config in poolConfigs)
        {
            CreatePool(config);
        }
    }

    private void CreatePool(PoolConfig config)
    {
        var objectPool = new Queue<GameObject>();
        
        for (int i = 0; i < config.initialSize; i++)
        {
            GameObject obj = CreateNewObject(config);
            objectPool.Enqueue(obj);
            objectsCurrentlyInPool.Add(obj); // NEW: Track from creation
        }

        poolDictionary.Add(config.poolKey, objectPool);
        configDictionary.Add(config.poolKey, config);
        activeObjectsCount.Add(config.poolKey, 0);
    }

    private GameObject CreateNewObject(PoolConfig config)
    {
        GameObject obj = Instantiate(config.prefab);
        obj.SetActive(false);
        
        if (config.parent != null)
            obj.transform.SetParent(config.parent);
        else
            obj.transform.SetParent(transform);

        // Añadir componente para identificar el pool
        var poolable = obj.GetComponent<PoolableObject>() ?? obj.AddComponent<PoolableObject>();
        poolable.PoolKey = config.poolKey;

        return obj;
    }

    // FIXED: Interface implementation now matches
    public GameObject GetObject(string poolKey)
    {
        if (!poolDictionary.ContainsKey(poolKey))
        {
            Debug.LogError($"Pool con key '{poolKey}' no existe!");
            return null;
        }

        var pool = poolDictionary[poolKey];
        GameObject obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            objectsCurrentlyInPool.Remove(obj); // NEW: No longer in pool
        }
        else
        {
            // Crear nuevo objeto si el pool está vacío pero no exceder maxSize
            if (activeObjectsCount[poolKey] < configDictionary[poolKey].maxSize)
            {
                obj = CreateNewObject(configDictionary[poolKey]);
                activeObjectsCount[poolKey]++; // NEW: Track active count for new objects
            }
            else
            {
                Debug.LogWarning($"Pool '{poolKey}' alcanzó tamaño máximo!");
                return null;
            }
        }

        obj.SetActive(true);
        activeObjectsCount[poolKey]++;
        return obj;
    }

    public void ReturnObject(GameObject obj)
    {
        if (obj == null) return;

        var poolable = obj.GetComponent<PoolableObject>();
        if (poolable == null)
        {
            Debug.LogError("Objeto no es poolable!");
            return;
        }

        string poolKey = poolable.PoolKey;
        if (!poolDictionary.ContainsKey(poolKey))
        {
            Debug.LogError($"Pool key '{poolKey}' no encontrado!");
            return;
        }

        // FIXED: Critical check to prevent re-adding to pool
        if (objectsCurrentlyInPool.Contains(obj))
        {
            return; // Already in pool, do nothing
        }

        obj.SetActive(false);
        poolDictionary[poolKey].Enqueue(obj);
        objectsCurrentlyInPool.Add(obj); // NEW: Track as being in pool
        activeObjectsCount[poolKey]--;
    }

    public void WarmUp(int count)
    {
        foreach (var config in poolConfigs)
        {
            WarmUpPool(config.poolKey, count);
        }
    }

    public void WarmUpPool(string poolKey, int count)
    {
        if (!poolDictionary.ContainsKey(poolKey)) return;

        var pool = poolDictionary[poolKey];
        var config = configDictionary[poolKey];

        for (int i = pool.Count; i < count && i < config.maxSize; i++)
        {
            GameObject obj = CreateNewObject(config);
            pool.Enqueue(obj);
            objectsCurrentlyInPool.Add(obj); // NEW: Track warmed-up objects
        }
    }

    // REMOVED: Parameterless GetObject() to avoid interface mismatch
    // If you need a default pool, consider specifying which pool should be default
}