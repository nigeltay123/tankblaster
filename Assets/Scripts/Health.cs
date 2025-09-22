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

        if (_hp <= 0)
        {
            _hp = 0; // ensure it never goes negative
            Die();
        }
    }

    void Die()
    {
        if (owner == Owner.Enemy)
        {
            // inform the wave tracker
            EnemySpawner.NotifyEnemyDied();
            Destroy(gameObject);
        }
        else // Player
        {
            var gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                Debug.Log("[Health] Player died → telling GameManager to handle game over.");
                gm.OnPlayerDied();
            }

            // remove the player tank object
            Destroy(gameObject);
        }
    }

    public void Heal(int amount)
    {
        if (_hp <= 0) return;

        _hp = Mathf.Min(maxHealth, _hp + Mathf.Max(1, amount));
    }

    public int Current => _hp;
}
