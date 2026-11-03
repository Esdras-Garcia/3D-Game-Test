using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public sealed class FireballProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 3f;
    private Vector3 direction;

    public void Launch(Vector3 launchDirection)
    {
        direction = launchDirection.normalized;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        var distance = speed * Time.deltaTime;
        if (Physics.SphereCast(transform.position, 0.12f, direction, out var hit, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null) enemy.TakeHit();
            Destroy(gameObject);
        }

        transform.position += direction * distance;
    }
}
