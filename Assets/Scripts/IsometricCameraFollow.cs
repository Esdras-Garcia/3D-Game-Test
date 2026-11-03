using UnityEngine;

public sealed class IsometricCameraFollow : MonoBehaviour
{
    public Transform target;
    [SerializeField] private Vector3 offset = new Vector3(-8.5f, 14f, -8.5f);
    [SerializeField, Range(35f, 65f)] private float pitch = 50f;
    [SerializeField] private float yaw = 45f;
    [SerializeField] private float smoothSpeed = 6f;

    private void Start()
    {
        // Mantém a sala alinhada aos axes do mundo. A camera segue apenas a posição do player
        // Ela nunca orbita ou muda o angulo com a movimentação do player
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = Vector3.Lerp(transform.position, target.position + offset, smoothSpeed * Time.deltaTime);
    }
}
