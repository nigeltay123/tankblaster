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
    public Button nextLevelButton;      // optional
    public Button restartButton;        // optional
    public TMP_Text levelText;

    [Header("Upgrades")]
    public UpgradeUI upgradeUI;         // drag the UpgradeUI object here

    [Header("Player & Camera")]
    public GameObject playerPrefab;
    public Camera mainCamera;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;          // Panel with “Game Over / Restart”
    public Button gameOverRestartButton;      // Hook this to RestartGame() below

    // ---------- DEV TESTING ----------
    [Header("Dev Testing")]
    public bool devSmallMap = false;
    public int devWidth  = 28;
    public int devHeight = 28;
    public int devDepth  = 3;

    private int currentLevel = 0;
    private GameObject playerInstance;
    private bool isGameOver = false;

    private void Start()
    {
        if (nextLevelButton) nextLevelButton.onClick.AddListener(GenerateLevel);
        if (restartButton)   restartButton.onClick.AddListener(RestartGame);
        if (gameOverPanel)   gameOverPanel.SetActive(false);
        if (gameOverRestartButton) gameOverRestartButton.onClick.AddListener(RestartGame);

        EnemySpawner.OnAllEnemiesDefeated += HandleLevelCleared;
        GenerateLevel();
    }

    private void OnDestroy()
    {
        EnemySpawner.OnAllEnemiesDefeated -= HandleLevelCleared;
    }

    public void GenerateLevel()
    {
        if (isGameOver) return; // don’t generate while game over is showing

        currentLevel++;

        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.currentLevel = currentLevel;

        // map sizing
        if (devSmallMap)
        {
            dungeonGenerator.dungeonWidth  = devWidth;
            dungeonGenerator.dungeonHeight = devHeight;
            dungeonGenerator.maxDepth      = devDepth;
        }
        else
        {
            dungeonGenerator.dungeonWidth  = 50 + (currentLevel * 10);
            dungeonGenerator.dungeonHeight = 50 + (currentLevel * 10);
            dungeonGenerator.maxDepth      = 4  + currentLevel;
        }

        dungeonGenerator.GenerateDungeon();

        if (levelText) levelText.text = $"Level {currentLevel}";

        SpawnPlayer();

        if (enemySpawner && playerInstance)
            enemySpawner.SpawnForLevel(currentLevel, playerInstance.transform);

        if (nextLevelButton) nextLevelButton.interactable = false;
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

        // apply persistent upgrades (HP bonus, etc.)
        PlayerUpgrades.ApplyToSpawnedPlayer(playerInstance);

        // top up to max
        var hp = playerInstance.GetComponent<Health>();
        if (hp != null) hp.Heal(9999);

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
        if (isGameOver) return; // ignore clears if already dead

        // Every 5 levels, show upgrade choices
        if (DifficultyManager.IsMilestoneLevel() && upgradeUI != null)
        {
            upgradeUI.Show();
            return;
        }

        // Otherwise auto-advance after 1 second
        Debug.Log("[GameManager] All enemies cleared, advancing to next level...");
        Invoke(nameof(GenerateLevel), 1.0f);
    }

    // --------- called by Health when player dies ---------
    public void OnPlayerDied()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("[GameManager] Player died. Stopping progression and showing Game Over.");

        // kill any pending auto-advance
        CancelInvoke(nameof(GenerateLevel));

        // hide upgrade UI if it was visible, and KEEP time paused afterwards
        if (upgradeUI) upgradeUI.HideImmediate();

        // Pause and show overlay
        Time.timeScale = 0f;
        if (gameOverPanel) gameOverPanel.SetActive(true);
        else
        {
            // fallback: auto restart after 1s (unpaused)
            Time.timeScale = 1f;
            Invoke(nameof(RestartGame), 1f);
        }
    }

    public void RestartGame()
    {
        // hide overlay & unpause
        if (gameOverPanel) gameOverPanel.SetActive(false);
        Time.timeScale = 1f;

        isGameOver = false;
        currentLevel = 0;

        if (enemySpawner) enemySpawner.Clear();
        GenerateLevel();
    }

    // ---------- DEV HELPERS ----------
    [ContextMenu("DEV: Jump To Level 5")]
    public void EditorJumpToLevel5() => JumpToLevel(5);

    [ContextMenu("DEV: Jump To Level 10")]
    public void EditorJumpToLevel10() => JumpToLevel(10);

    public void JumpToLevel(int level)
    {
        if (level < 1) level = 1;
        currentLevel = level - 1;
        GenerateLevel();
    }

    public void ToggleSmallMapAndRegen()
    {
        devSmallMap = !devSmallMap;
        currentLevel = Mathf.Max(0, currentLevel - 1);
        GenerateLevel();
        Debug.Log($"[GameManager] devSmallMap now {(devSmallMap ? "ON" : "OFF")}");
    }
}
