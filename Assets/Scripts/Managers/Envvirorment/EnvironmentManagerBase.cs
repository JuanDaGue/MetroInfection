using System.Collections.Generic;
using UnityEngine;

// Principio de Liskov: Clase base que puede ser sustituida
public abstract class EnvironmentManagerBase : IEnvironmentManager
{
    protected EnvironmentCoordinator coordinator;
    protected IObjectPool objectPool;
    protected List<GameObject> activeElements = new List<GameObject>();

    public abstract string ManagerType { get; }

    protected EnvironmentManagerBase(EnvironmentCoordinator coord)
    {
        coordinator = coord;
        objectPool = ObjectPool.GetPoolInstance();
    }

    public abstract void Initialize();
    public abstract void UpdateManager(float deltaTime);
    public abstract void RecycleElements();

    protected virtual GameObject SpawnElement(string poolKey, Vector3 position, Quaternion rotation)
    {
        GameObject element = objectPool.GetObject(poolKey);
        if (element != null)
        {
            element.transform.position = position;
            element.transform.rotation = rotation;
            activeElements.Add(element);
        }
        return element;
    }

    protected virtual void RecycleElement(GameObject element)
    {
        if (activeElements.Contains(element))
        {
            activeElements.Remove(element);
            objectPool.ReturnObject(element);
        }
    }

    protected virtual void RecycleAllElements()
    {
        for (int i = activeElements.Count - 1; i >= 0; i--)
        {
            RecycleElement(activeElements[i]);
        }
    }
}