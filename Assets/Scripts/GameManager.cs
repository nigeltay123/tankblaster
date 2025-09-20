using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Generation")]
    public BSPDungeonGenerator dungeonGenerator;

    [Header("Enemies")]
    public EnemySpawner enemySpawner;   // <-- NEW: hook to EnemySpawner component

    [Header("UI")]
    public Button nextLevelButton;
    public Button restartButton;
    public TMP_Text levelText;

    [Header("Player & Camera")]
    public GameObject playerPrefab;     // <- your PlayerTank prefab
    public Camera mainCamera;           // <- assign Main Camera from scene

    private int currentLevel = 0;
    private GameObject playerInstance;  // runtime-spawned tank

    private void Start()
    {
        if (nextLevelButton) nextLevelButton.onClick.AddListener(GenerateLevel);
        if (restartButton)   restartButton.onClick.AddListener(RestartGame);

        // NEW: listen for wave cleared
        EnemySpawner.OnAllEnemiesDefeated += HandleLevelCleared;

        GenerateLevel();
    }

    private void OnDestroy()
    {
        // NEW: unsubscribe
        EnemySpawner.OnAllEnemiesDefeated -= HandleLevelCleared;
    }

    public void GenerateLevel()
    {
        currentLevel++;

        // Grow map slightly each level (optional)
        dungeonGenerator.dungeonWidth  = 50 + (currentLevel * 10);
        dungeonGenerator.dungeonHeight = 50 + (currentLevel * 10);
        dungeonGenerator.maxDepth      = 4  + currentLevel;

        // Build dungeon
        dungeonGenerator.GenerateDungeon();

        // Update UI
        if (levelText != null) levelText.text = $"Level {currentLevel}";

        // (Re)spawn the player safely
        SpawnPlayer();

        // NEW: spawn enemies for this level AFTER player exists
        if (enemySpawner && playerInstance)
        {
            enemySpawner.SpawnForLevel(currentLevel, playerInstance.transform);
        }

        // NEW: lock Next Level button until wave cleared (optional)
        if (nextLevelButton) nextLevelButton.interactable = false;
    }

    private void SpawnPlayer()
    {
        // Clean up old player (if any)
        if (playerInstance != null)
        {
            Destroy(playerInstance);
            playerInstance = null;
        }

        // Safe spawn world position:
        Vector3 spawnPos = Vector3.zero;

        try
        {
            // Use BSPDungeonGenerator helper
            spawnPos = dungeonGenerator.GetSpawnWorldPosition(1);
        }
        catch
        {
            // Fallback: first-room center
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

        // Spawn the tank
        playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        // Tell the camera who to follow
        if (mainCamera != null)
        {
            var follow = mainCamera.GetComponent<CameraFollow2D>();
            if (follow != null)
            {
                follow.target = playerInstance.transform;
            }

            // Make sure camera z is behind the scene
            var camPos = mainCamera.transform.position;
            if (camPos.z > -5f)
                mainCamera.transform.position = new Vector3(camPos.x, camPos.y, -10f);
        }
    }

    // NEW: wave cleared → enable Next or auto-advance
    private void HandleLevelCleared()
    {
        if (nextLevelButton) nextLevelButton.interactable = true;
        // Or auto-advance instead:
        // Invoke(nameof(GenerateLevel), 1.0f);
    }

    public void RestartGame()
    {
        currentLevel = 0;

        // NEW: clear any remaining enemies before regenerating
        if (enemySpawner) enemySpawner.Clear();

        GenerateLevel();
    }
}
