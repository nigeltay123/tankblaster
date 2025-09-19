using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Generation")]
    public BSPDungeonGenerator dungeonGenerator;

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

        GenerateLevel();
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
        // Prefer BSPDungeonGenerator.GetSpawnWorldPosition if present,
        // otherwise fallback to the center of the first room.
        Vector3 spawnPos = Vector3.zero;

        // Try to use helper if your generator has it
        try
        {
            spawnPos = dungeonGenerator.GetSpawnWorldPosition(1);
        }
        catch
        {
            // Fallback: first-room center (grid coords -> world center-of-cell)
            IReadOnlyList<RectInt> rooms = dungeonGenerator.Rooms;
            if (rooms != null && rooms.Count > 0)
            {
                RectInt r = rooms[0];
                Vector2 center = new Vector2(r.x + r.width / 2f, r.y + r.height / 2f);
                spawnPos = dungeonGenerator.tilemap.CellToWorld(new Vector3Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y), 0)) + new Vector3(0.5f, 0.5f, 0f);
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
                follow.target = playerInstance.transform;   // <— key line (no SetTarget needed)
            }

            // Make sure camera z is behind the scene
            var camPos = mainCamera.transform.position;
            if (camPos.z > -5f) mainCamera.transform.position = new Vector3(camPos.x, camPos.y, -10f);
        }
    }

    public void RestartGame()
    {
        currentLevel = 0;
        GenerateLevel();
    }
}