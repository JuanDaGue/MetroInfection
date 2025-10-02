using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnvironmentCoordinator))]
public class EnvironmentCoordinatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EnvironmentCoordinator coordinator = (EnvironmentCoordinator)target;

        if (GUILayout.Button("Warm Up Pools"))
        {
            ObjectPool pool = FindFirstObjectByType<ObjectPool>();
            if (pool != null)
            {
                pool.WarmUp(50);
                Debug.Log("Pools calentados!");
            }
        }

        if (GUILayout.Button("Set Speed to 10"))
        {
            coordinator.SetEnvironmentSpeed(10f);
        }
    }
}