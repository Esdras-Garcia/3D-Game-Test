using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public sealed class ClickToMove : MonoBehaviour
{
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        // Rotaciona o personagem explicitamente para que o input NavMeshAgent.Move também
        // muda a raiz e o modelo consistentemente.
        agent.updateRotation = false;
    }

    private void Update()
    {
        if (Keyboard.current == null || !agent.isOnNavMesh) return;

        var input = Vector2.zero;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;

        if (input.sqrMagnitude < 0.01f)
        {
            if (agent.hasPath) agent.ResetPath();
            return;
        }

        input = Vector2.ClampMagnitude(input, 1f);
        var cameraTransform = Camera.main != null ? Camera.main.transform : transform;
        var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        var right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        var direction = (forward * input.y + right * input.x).normalized;
        if (agent.hasPath) agent.ResetPath();

        // Move segue o input atual do player diretamente enquanto o agent restringe
        // a posição para o chão que é possível de andar. Sem oscilar o destino entre
        // alternativas no final das paredes
        agent.Move(direction * agent.speed * Time.deltaTime);
        if (direction.sqrMagnitude > 0.001f)
        {
            var desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation,
                agent.angularSpeed * Time.deltaTime);
        }
    }
}
