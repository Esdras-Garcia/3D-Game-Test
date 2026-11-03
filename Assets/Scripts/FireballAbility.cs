using UnityEngine;
using UnityEngine.InputSystem;

public sealed class FireballAbility : MonoBehaviour
{
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform projectileOrigin;
    [SerializeField] private float launchHeight = 1.1f;

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        var camera = Camera.main;
        if (camera == null || fireballPrefab == null) return;

        var ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        var hits = Physics.RaycastAll(ray, 500f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject.name != "Ground") continue;

            var start = projectileOrigin != null
                ? projectileOrigin.position
                : transform.position + Vector3.up * launchHeight;
            var target = new Vector3(hit.point.x, start.y, hit.point.z);
            var direction = target - start;
            if (direction.sqrMagnitude < 0.01f) return;
            var projectile = Instantiate(fireballPrefab, start, Quaternion.LookRotation(direction));
            projectile.GetComponent<FireballProjectile>().Launch(direction.normalized);
            return;
        }
    }
}
