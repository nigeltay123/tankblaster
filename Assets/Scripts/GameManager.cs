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

    [Header("Player & Camera")]
    public GameObject playerPrefab;
    public Camera mainCamera;

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

        // grow map a bit each level (optional)
        dungeonGenerator.dungeonWidth  = 50 + (currentLevel * 10);
        dungeonGenerator.dungeonHeight = 50 + (currentLevel * 10);
        dungeonGenerator.maxDepth      = 4  + currentLevel;

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

        // OPTIONAL: reset player health each level
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
        Debug.Log("[GameManager] All enemies cleared, advancing to next level...");
        // Auto-advance after 1 second
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
}
