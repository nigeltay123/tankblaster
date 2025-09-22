using UnityEngine;

public class UpgradeUI : MonoBehaviour
{
    [Header("Panel that contains the two buttons")]
    public GameObject panel;

    void Awake()
    {
        if (panel) panel.SetActive(false);
        Debug.Log($"[UpgradeUI] Awake, panel={(panel ? "ok" : "NULL")}");
    }

    public void Show()
    {
        if (panel) panel.SetActive(true);
        Time.timeScale = 0f; // pause
        Debug.Log("[UpgradeUI] Show → paused.");
    }

    public void Hide()
    {
        if (panel) panel.SetActive(false);
        Time.timeScale = 1f;
        Debug.Log("[UpgradeUI] Hide → resumed.");
    }

    // Used when the player dies while the upgrade panel is open.
    public void HideImmediate()
    {
        if (panel) panel.SetActive(false);
        // IMPORTANT: do NOT resume time here; GameManager will keep it paused for Game Over.
        Debug.Log("[UpgradeUI] HideImmediate (no time resume).");
    }

    // Button hook: +2 Max HP
    public void OnPickHealth()
    {
        var p = GameObject.FindWithTag("Player");
        var hp = p ? p.GetComponent<Health>() : null;
        PlayerUpgrades.Instance.PickHealth(hp);
        ContinueToNextLevel();
    }

    // Button hook: +1 Damage
    public void OnPickDamage()
    {
        PlayerUpgrades.Instance.PickDamage();
        ContinueToNextLevel();
    }

    void ContinueToNextLevel()
    {
        Hide();
        var gm = FindObjectOfType<GameManager>();
        if (gm) gm.GenerateLevel();
    }
}
