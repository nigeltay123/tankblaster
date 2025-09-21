using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Refs")]
    public Health playerHealth;   // drag the player instance (or a prefab if spawned early)
    public TMP_Text levelText;    // e.g., "Lv 5"
    public TMP_Text hpText;       // e.g., "HP 5 / 7"
    public Slider hpBar;          // optional: fill shows HP%

    void Update()
    {
        // level
        if (levelText)
            levelText.text = $"Lv {DifficultyManager.Level}";

        // hp
        if (playerHealth)
        {
            int cur = playerHealth.Current;
            int max = playerHealth.maxHealth;

            if (hpText) hpText.text = $"HP {cur} / {max}";
            if (hpBar)
            {
                hpBar.maxValue = max;
                hpBar.value = cur;
            }
        }
        else
        {
            // try to find the player once (covers respawn)
            var p = GameObject.FindWithTag("Player");
            if (p) playerHealth = p.GetComponent<Health>();
        }
    }
}
