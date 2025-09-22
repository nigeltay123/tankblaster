using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Refs")]
    public Health playerHealth;   // player instance
    public TMP_Text levelText;    // e.g., "Lv 5"
    public TMP_Text hpText;       // e.g., "HP 5 / 7"
    public Slider  hpBar;         // fill shows HP%

    int _lastMax = 0;

    void Update()
    {
        if (levelText)
            levelText.text = $"Lv {DifficultyManager.Level}";

        if (playerHealth)
        {
            int cur = playerHealth.Current;
            int max = playerHealth.maxHealth;
            _lastMax = max;

            if (hpText) hpText.text = $"HP {cur} / {max}";
            if (hpBar)
            {
                hpBar.maxValue = max;
                hpBar.value = cur;
            }
        }
        else
        {
            // try find the player (covers respawn)
            var p = GameObject.FindWithTag("Player");
            if (p)
            {
                playerHealth = p.GetComponent<Health>();
            }
            else
            {
                // no player in scene → show empty bar
                if (hpText) hpText.text = $"HP 0 / {_lastMax}";
                if (hpBar)
                {
                    hpBar.maxValue = (_lastMax > 0 ? _lastMax : 1);
                    hpBar.value = 0;
                }
            }
        }
    }
}
