using UnityEngine;

public sealed class EnemyHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int hitPoints = 3;
    private int remainingHitPoints;

    private void Awake() { remainingHitPoints = hitPoints; }

    public void TakeHit(int damage = 1)
    {
        remainingHitPoints -= damage;
        if (remainingHitPoints <= 0) Destroy(gameObject);
    }
}
