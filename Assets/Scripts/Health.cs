using UnityEngine;

public class Health : MonoBehaviour
{
    public enum Owner { Player, Enemy }

    [Header("Health")]
    public Owner owner = Owner.Enemy;
    public int maxHealth = 3;

    int _hp;

    void Awake() => _hp = maxHealth;

    public void TakeDamage(int amount)
    {
        if (_hp <= 0) return;
        _hp -= Mathf.Max(1, amount);

        if (_hp <= 0) Die();
    }

    void Die()
    {
        if (owner == Owner.Enemy)
        {
            // inform the wave tracker
            EnemySpawner.NotifyEnemyDied();
        }
        else // Player
        {
            // simple auto-restart on death (no buttons needed)
            var gm = FindObjectOfType<GameManager>();
            gm?.RestartGame();
        }

        Destroy(gameObject);
    }

    public void Heal(int amount)
    {
        if (_hp <= 0) return;
        _hp = Mathf.Min(maxHealth, _hp + Mathf.Max(1, amount));
    }
}
