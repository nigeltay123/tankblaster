using UnityEngine;
using System.Reflection;

public class PlayerUpgrades : MonoBehaviour
{
    public static PlayerUpgrades Instance { get; private set; }

    [Header("Per-pick amounts")]
    public int healthPerPick = 2;
    public int damagePerPick = 1;

    [Header("Totals (runtime)")]
    public int totalHealthBonus = 0;
    public int totalDamageBonus = 0;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[Upgrades] PlayerUpgrades singleton ready.");
    }

    public static int DamageBonus => Instance ? Instance.totalDamageBonus : 0;
    public static int HealthBonus => Instance ? Instance.totalHealthBonus : 0;

    // Apply saved bonuses to a newly spawned player.
    public static void ApplyToSpawnedPlayer(GameObject player)
    {
        if (!Instance || !player)
        {
            Debug.LogWarning("[Upgrades] ApplyToSpawnedPlayer: missing Instance or player.");
            return;
        }

        var h = player.GetComponent<Health>();
        if (!h)
        {
            Debug.LogWarning("[Upgrades] ApplyToSpawnedPlayer: player has no Health component.");
            return;
        }

        int before = h.maxHealth;
        h.maxHealth += Instance.totalHealthBonus;

        // reset private _hp to full
        var f = typeof(Health).GetField("_hp", BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) f.SetValue(h, h.maxHealth);

        Debug.Log($"[Upgrades] Applied to spawn: base {before} -> {h.maxHealth} (bonus={Instance.totalHealthBonus})");
    }

    // Called when the user picks the HP upgrade.
    public void PickHealth(Health currentPlayerHealth)
    {
        totalHealthBonus += healthPerPick;
        Debug.Log($"[Upgrades] PickHealth: +{healthPerPick}, totalHealthBonus={totalHealthBonus}");

        if (currentPlayerHealth)
        {
            int beforeMax = currentPlayerHealth.maxHealth;
            currentPlayerHealth.maxHealth += healthPerPick;

            // raise current hp too (not above new max)
            var f = typeof(Health).GetField("_hp", BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null)
            {
                int cur = (int)f.GetValue(currentPlayerHealth);
                cur = Mathf.Min(currentPlayerHealth.maxHealth, cur + healthPerPick);
                f.SetValue(currentPlayerHealth, cur);
                Debug.Log($"[Upgrades] Player HP now {cur}/{currentPlayerHealth.maxHealth} (was max {beforeMax})");
            }
            else
            {
                Debug.LogWarning("[Upgrades] PickHealth: couldn't reflect _hp; current HP not bumped.");
            }
        }
        else
        {
            Debug.Log("[Upgrades] PickHealth: no current player Health (will still persist for next spawns).");
        }
    }

    // Called when the user picks the Damage upgrade.
    public void PickDamage()
    {
        totalDamageBonus += damagePerPick;
        Debug.Log($"[Upgrades] PickDamage: +{damagePerPick}, totalDamageBonus={totalDamageBonus}");
    }
}
