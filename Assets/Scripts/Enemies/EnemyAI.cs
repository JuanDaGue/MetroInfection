using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent

public class EnemyAI : MonoBehaviour
{
    public Transform target; // Assign this to a "window" or entry point object
    private NavMeshAgent agent;
    private enum EnemyState { Patrol, Chase, Attack }
    private EnemyState currentState = EnemyState.Patrol;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (target != null)
        {
            agent.SetDestination(target.position);
        }
    }

    void Update()
    {
        // Basic state machine logic
        switch (currentState)
        {
            case EnemyState.Patrol:
                // Logic to search for windows/player
                // You can use Physics.OverlapSphere or other triggers here :cite[6]
                break;
            case EnemyState.Chase:
                // If player is found, chase them
                if (target != null)
                {
                    agent.SetDestination(target.position);
                }
                break;
            case EnemyState.Attack:
                // If in range, attack the player
                break;
        }
    }

    // This function can be called by the EnemyManager or when the enemy detects something
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null && agent != null)
        {
            agent.SetDestination(target.position);
        }
    }

    // Example: When the enemy reaches a window, it can then target the player inside.
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Window"))
        {
            // Find the player and start chasing
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                SetTarget(player.transform);
            }
        }
    }
}