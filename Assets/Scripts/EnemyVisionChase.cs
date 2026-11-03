using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public sealed class EnemyVisionChase : MonoBehaviour
{
    [SerializeField, Range(30f, 120f)] private float fieldOfView = 70f;
    [SerializeField, Min(1f)] private float viewDistance = 15f;
    [SerializeField] private float destinationRefreshRate = 0.2f;
    [SerializeField] private Transform player;

    private NavMeshAgent agent;
    private bool hasSpottedPlayer;
    private float nextDestinationRefresh;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        if (player == null)
        {
            var playerObject = GameObject.Find("Player");
            if (playerObject != null) player = playerObject.transform;
        }
    }

    private void Update()
    {
        if (player == null || !agent.isOnNavMesh) return;
        if (!hasSpottedPlayer && CanSeePlayer()) hasSpottedPlayer = true;
        if (!hasSpottedPlayer) return;

        agent.isStopped = false;
        if (Time.time >= nextDestinationRefresh)
        {
            agent.SetDestination(player.position);
            nextDestinationRefresh = Time.time + destinationRefreshRate;
        }

        var desiredVelocity = Vector3.ProjectOnPlane(agent.desiredVelocity, Vector3.up);
        if (desiredVelocity.sqrMagnitude > 0.01f)
        {
            var desiredRotation = Quaternion.LookRotation(desiredVelocity.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation,
                agent.angularSpeed * Time.deltaTime);
        }
    }

    private bool CanSeePlayer()
    {
        var toPlayer = player.position + Vector3.up - (transform.position + Vector3.up);
        var distance = toPlayer.magnitude;
        if (distance > viewDistance) return false;
        if (Vector3.Angle(transform.forward, toPlayer) > fieldOfView * 0.5f) return false;
        return !Physics.Raycast(transform.position + Vector3.up, toPlayer.normalized, distance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up, viewDistance);
        var left = Quaternion.Euler(0f, -fieldOfView * 0.5f, 0f) * transform.forward;
        var right = Quaternion.Euler(0f, fieldOfView * 0.5f, 0f) * transform.forward;
        Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + left * viewDistance);
        Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + right * viewDistance);
    }
}
