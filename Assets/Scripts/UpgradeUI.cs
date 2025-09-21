using UnityEngine;

public class UpgradeUI : MonoBehaviour
{
    [Header("Panel that contains the two buttons")]
    public GameObject panel;

    [Header("Refs (optional)")]
    public PlayerUpgrades upgrades;   // you can leave this empty

    void Awake()
    {
        EnsureUpgradesExists();
        if (panel) panel.SetActive(false);
        Debug.Log($"[UpgradeUI] Awake. panel={(panel? "ok":"NULL")} upgrades={(upgrades? "ok":"NULL")}");
    }

    void EnsureUpgradesExists()
    {
        // already have one?
        if (upgrades) return;

        // try find one in the scene
        upgrades = PlayerUpgrades.Instance ?? FindObjectOfType<PlayerUpgrades>();
        if (upgrades) return;

        // still none: create one (will DontDestroyOnLoad in Awake)
        var go = new GameObject("Upgrades");
        upgrades = go.AddComponent<PlayerUpgrades>();
        Debug.Log("[UpgradeUI] Created PlayerUpgrades at runtime.");
    }

    public void Show()
    {
        if (panel) panel.SetActive(true);
        Time.timeScale = 0f; // pause
        Debug.Log("[UpgradeUI] Show → paused.");
    }

    void Hide()
    {
        if (panel) panel.SetActive(false);
        Time.timeScale = 1f;
        Debug.Log("[UpgradeUI] Hide → resumed.");
    }

    public void OnPickHealth()
    {
        EnsureUpgradesExists();

        var p  = GameObject.FindWithTag("Player");
        var hp = p ? p.GetComponent<Health>() : null;

        Debug.Log($"[UpgradeUI] OnPickHealth: upgrades={(upgrades? "ok":"NULL")}, player={(p? "ok":"NULL")}, health={(hp? "ok":"NULL")}");
        upgrades?.PickHealth(hp);
        ContinueToNextLevel();
    }

    public void OnPickDamage()
    {
        EnsureUpgradesExists();
        Debug.Log($"[UpgradeUI] OnPickDamage: upgrades={(upgrades? "ok":"NULL")}");
        upgrades?.PickDamage();
        ContinueToNextLevel();
    }

    void ContinueToNextLevel()
    {
        Hide();
        var gm = FindObjectOfType<GameManager>();
        if (gm) gm.GenerateLevel();
        else Debug.LogWarning("[UpgradeUI] ContinueToNextLevel: GameManager not found.");
    }
}
