using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Generation")]
    public BSPDungeonGenerator dungeonGenerator;

    [Header("Enemies")]
    public EnemySpawner enemySpawner;   // assign in Inspector

    [Header("UI")]
    public Button nextLevelButton;      // optional, safe if None
    public Button restartButton;        // optional, safe if None
    public TMP_Text levelText;

    [Header("Upgrades")]
    public UpgradeUI upgradeUI;         // ← drag the UpgradeUI object here in Inspector

    [Header("Player & Camera")]
    public GameObject playerPrefab;
    public Camera mainCamera;

    // ---------- DEV TESTING ----------
    [Header("Dev Testing")]
    [Tooltip("If enabled, uses a much smaller map so you can reach milestones fast.")]
    public bool devSmallMap = false;
    public int devWidth  = 28;
    public int devHeight = 28;
    public int devDepth  = 3;

    private int currentLevel = 0;
    private GameObject playerInstance;

    private void Start()
    {
        if (nextLevelButton) nextLevelButton.onClick.AddListener(GenerateLevel);
        if (restartButton)   restartButton.onClick.AddListener(RestartGame);

        EnemySpawner.OnAllEnemiesDefeated += HandleLevelCleared;
        GenerateLevel();
    }

    private void OnDestroy()
    {
        EnemySpawner.OnAllEnemiesDefeated -= HandleLevelCleared;
    }

    public void GenerateLevel()
    {
        currentLevel++;

        // Tell DifficultyManager the level BEFORE spawning enemies
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.currentLevel = currentLevel;

        // --- map sizing ---
        if (devSmallMap)
        {
            dungeonGenerator.dungeonWidth  = devWidth;
            dungeonGenerator.dungeonHeight = devHeight;
            dungeonGenerator.maxDepth      = devDepth;
        }
        else
        {
            // grow map a bit each level (your original logic)
            dungeonGenerator.dungeonWidth  = 50 + (currentLevel * 10);
            dungeonGenerator.dungeonHeight = 50 + (currentLevel * 10);
            dungeonGenerator.maxDepth      = 4  + currentLevel;
        }

        dungeonGenerator.GenerateDungeon();

        if (levelText != null) levelText.text = $"Level {currentLevel}";

        SpawnPlayer();

        // spawn enemies AFTER player exists
        if (enemySpawner && playerInstance)
            enemySpawner.SpawnForLevel(currentLevel, playerInstance.transform);

        if (nextLevelButton) nextLevelButton.interactable = false; // optional
    }

    private void SpawnPlayer()
    {
        if (playerInstance != null)
        {
            Destroy(playerInstance);
            playerInstance = null;
        }

        Vector3 spawnPos = Vector3.zero;

        try
        {
            spawnPos = dungeonGenerator.GetSpawnWorldPosition(1);
        }
        catch
        {
            IReadOnlyList<RectInt> rooms = dungeonGenerator.Rooms;
            if (rooms != null && rooms.Count > 0)
            {
                RectInt r = rooms[0];
                Vector2 center = new Vector2(r.x + r.width / 2f, r.y + r.height / 2f);
                spawnPos = dungeonGenerator.floorsTilemap.CellToWorld(
                    new Vector3Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y), 0)
                ) + new Vector3(0.5f, 0.5f, 0f);
            }
        }

        playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        // Apply persistent upgrades (HP bonus, etc.) to the new player
        PlayerUpgrades.ApplyToSpawnedPlayer(playerInstance);

        // OPTIONAL: top up to new max
        var hp = playerInstance.GetComponent<Health>();
        if (hp != null) hp.Heal(9999); // clamp inside Health to max

        if (mainCamera != null)
        {
            var follow = mainCamera.GetComponent<CameraFollow2D>();
            if (follow != null) follow.target = playerInstance.transform;

            var camPos = mainCamera.transform.position;
            if (camPos.z > -5f)
                mainCamera.transform.position = new Vector3(camPos.x, camPos.y, -10f);
        }
    }

    private void HandleLevelCleared()
    {
        // Every 5 levels, pause and show the upgrade choices
        if (DifficultyManager.IsMilestoneLevel() && upgradeUI != null)
        {
            upgradeUI.Show();     // pauses via Time.timeScale = 0
            return;
        }

        // Otherwise auto-advance after 1 second
        Debug.Log("[GameManager] All enemies cleared, advancing to next level...");
        Invoke(nameof(GenerateLevel), 1.0f);

        // If you prefer using the button instead, comment the line above and:
        // if (nextLevelButton) nextLevelButton.interactable = true;
    }

    public void RestartGame()
    {
        currentLevel = 0;
        if (enemySpawner) enemySpawner.Clear();
        GenerateLevel();
    }

    // ---------- DEV HELPERS ----------
    [ContextMenu("DEV: Jump To Level 5")]
    public void EditorJumpToLevel5() => JumpToLevel(5);

    [ContextMenu("DEV: Jump To Level 10")]
    public void EditorJumpToLevel10() => JumpToLevel(10);

    /// <summary>
    /// Jump directly to a level. Sets internal counter so GenerateLevel builds that level next.
    /// </summary>
    public void JumpToLevel(int level)
    {
        if (level < 1) level = 1;
        // currentLevel increments inside GenerateLevel(), so set to level-1 here
        currentLevel = level - 1;
        GenerateLevel();
    }

    /// <summary>
    /// Toggle small map mode at runtime and regenerate current level.
    /// </summary>
    public void ToggleSmallMapAndRegen()
    {
        devSmallMap = !devSmallMap;
        // Re-run current level sizing: set to (currentLevel-1) then generate
        currentLevel = Mathf.Max(0, currentLevel - 1);
        GenerateLevel();
        Debug.Log($"[GameManager] devSmallMap now {(devSmallMap ? "ON" : "OFF")}");
    }
}
