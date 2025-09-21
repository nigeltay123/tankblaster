using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Game Progress")]
    [Tooltip("1-based. Set this when a level starts.")]
    public int currentLevel = 1;

    [Header("Global Scaling")]
    [Tooltip("Per-level movement speed increase. 0.03 = +3%/level.")]
    public float speedPerLevel = 0.03f;
    [Tooltip("Per-level fire cooldown reduction. 0.02 = -2%/level.")]
    public float fireCooldownPerLevel = 0.02f;
    [Tooltip("Cooldown won't go below this fraction of base (0.5 = 50%).")]
    public float minFireCooldownFraction = 0.5f;

    [Header("Milestones")]
    [Tooltip("Every N levels is a milestone (e.g., upgrades, champions, etc.).")]
    public int milestoneEveryNLevels = 5;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static int Level => Instance ? Instance.currentLevel : 1;

    // +2 HP every milestone (5 by default)
    public static int ExtraHealthByMilestones()
    {
        int n = Instance ? Instance.milestoneEveryNLevels : 5;
        int milestones = Mathf.FloorToInt((Level - 1) / (float)n);
        return milestones * 2;
    }

    public static float SpeedMultiplier()
    {
        if (!Instance) return 1f;
        return 1f + Mathf.Max(0, Level - 1) * Instance.speedPerLevel;
    }

    // Multiplier applied to fire COOLDOWN (so <1 = faster)
    public static float FireCooldownMultiplier()
    {
        if (!Instance) return 1f;
        float mult = 1f - Mathf.Max(0, Level - 1) * Instance.fireCooldownPerLevel;
        return Mathf.Max(Instance.minFireCooldownFraction, mult);
    }

    public static bool IsMilestoneLevel()
    {
        if (!Instance) return (Level % 5) == 0;
        int n = Mathf.Max(1, Instance.milestoneEveryNLevels);
        return (Level % n) == 0;
    }
}
