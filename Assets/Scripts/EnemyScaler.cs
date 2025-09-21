using UnityEngine;

[DisallowMultipleComponent]
public class EnemyScaler : MonoBehaviour
{
    [Header("Base Stats (set per prefab)")]
    public int baseHealth = 3;
    public float baseMoveSpeed = 2.6f;     // match EnemyPatrol patrolSpeed at level 1
    public float baseFireCooldown = 1.5f;  // match EnemyShooting fireRate at level 1

    [Header("Optional Visual")]
    public bool tintOnMilestone = true;
    public Color milestoneTint = new Color(1f, 0.85f, 0.4f, 1f);

    void Start()
    {
        int level = DifficultyManager.Level;

        // 1) Health scaling: +2 every milestone (default: every 5 levels)
        int scaledHealth = baseHealth + DifficultyManager.ExtraHealthByMilestones();
        ApplyHealth(scaledHealth);

        // 2) Movement speed scaling
        float speedMult = DifficultyManager.SpeedMultiplier();
        ApplyMoveSpeed(baseMoveSpeed * speedMult);

        // 3) Fire cooldown scaling (faster shooting as levels go up)
        float cdMult = DifficultyManager.FireCooldownMultiplier();
        ApplyFireCooldown(baseFireCooldown * cdMult);

        // 4) Optional: visual tint on milestone levels
        if (tintOnMilestone && DifficultyManager.IsMilestoneLevel())
            TintSprite(milestoneTint);

        Debug.Log($"[EnemyScaler] Enemy scaled for level {level}: HP={scaledHealth}, Speed={baseMoveSpeed * speedMult}, FireCD={baseFireCooldown * cdMult}");
    }

    void ApplyHealth(int newMaxHealth)
    {
        var h = GetComponent<Health>();
        if (h != null)
        {
            h.maxHealth = newMaxHealth;

            // reset private _hp using reflection
            var field = typeof(Health).GetField("_hp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(h, newMaxHealth);
        }
    }

    void ApplyMoveSpeed(float newSpeed)
    {
        var patrol = GetComponent<EnemyPatrol>();
        if (patrol != null) patrol.patrolSpeed = newSpeed;
    }

    void ApplyFireCooldown(float newCooldown)
    {
        var shoot = GetComponent<EnemyShooting>();
        if (shoot != null) shoot.fireRate = Mathf.Max(0.15f, newCooldown);
    }

    void TintSprite(Color c)
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.color = c;
    }
}
