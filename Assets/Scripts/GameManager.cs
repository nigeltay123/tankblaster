using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Generation")]
    public BSPDungeonGenerator dungeonGenerator;

    [Header("Player & Camera")]
    public GameObject playerPrefab;   // <-- drag PlayerTank prefab here
    public Camera mainCamera;         // <-- drag Main Camera here

    [Header("UI")]
    public Button nextLevelButton;
    public Button restartButton;
    public TMP_Text levelText;

    private int currentLevel = 0;
    private GameObject playerInstance;  // runtime-spawned player

    private void Start()
    {
        if (nextLevelButton) nextLevelButton.onClick.AddListener(GenerateLevel);
        if (restartButton)   restartButton.onClick.AddListener(RestartGame);

        GenerateLevel();
    }

    public void GenerateLevel()
    {
        currentLevel++;

        // Scale dungeon
        dungeonGenerator.dungeonWidth  = 50 + (currentLevel * 10);
        dungeonGenerator.dungeonHeight = 50 + (currentLevel * 10);
        dungeonGenerator.maxDepth      = 4  + currentLevel;

        // Build dungeon
        dungeonGenerator.GenerateDungeon();

        // UI
        if (levelText) levelText.text = "Level " + currentLevel;

        // Spawn or move player
        PlacePlayerInRoom();
    }

    private void PlacePlayerInRoom()
    {
        // Make sure Rooms exists & has entries
        if (dungeonGenerator.Rooms == null || dungeonGenerator.Rooms.Count == 0)
        {
            Debug.LogWarning("[GameManager] No rooms available for spawning player.");
            return;
        }

        // Choose a room (first or random)
        RectInt chosen = dungeonGenerator.Rooms[0];
        // If you prefer random: RectInt chosen = dungeonGenerator.Rooms[Random.Range(0, dungeonGenerator.Rooms.Count)];

        // Tile/world position (tile center)
        Vector2 roomCenter = new Vector2(
            chosen.x + chosen.width  / 2f,
            chosen.y + chosen.height / 2f
        );
        Vector3 spawnPos = new Vector3(roomCenter.x, roomCenter.y, 0f);

        // Instantiate if needed, otherwise move existing instance
        if (playerInstance == null)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[GameManager] Player prefab not assigned.");
                return;
            }
            playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            playerInstance.transform.position = spawnPos;
        }

        // Center camera on player (orthographic)
        if (mainCamera != null)
        {
            if (!mainCamera.orthographic) mainCamera.orthographic = true;
            Vector3 camPos = mainCamera.transform.position;
            camPos.x = spawnPos.x;
            camPos.y = spawnPos.y;
            camPos.z = -10f; // keep camera in front
            mainCamera.transform.position = camPos;
        }
    }

    public void RestartGame()
    {
        currentLevel = 0;
        GenerateLevel();
    }
}