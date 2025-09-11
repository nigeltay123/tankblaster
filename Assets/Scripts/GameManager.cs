using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // for text

public class GameManager : MonoBehaviour
{
    public BSPDungeonGenerator dungeonGenerator;
    public Button nextLevelButton;
    public Button restartButton;
    public TMP_Text levelText;

    private int currentLevel = 0;

    void Start()
    {
        nextLevelButton.onClick.AddListener(GenerateLevel);
        restartButton.onClick.AddListener(RestartGame);

        GenerateLevel();
    }

    public void GenerateLevel()
    {
        currentLevel++;

        // Progressively increase dungeon size and complexity
        dungeonGenerator.dungeonWidth = 50 + (currentLevel * 10);
        dungeonGenerator.dungeonHeight = 50 + (currentLevel * 10);
        dungeonGenerator.maxDepth = 4 + currentLevel;

        dungeonGenerator.GenerateDungeon();

        // Update UI level text
        if (levelText != null)
        {
            levelText.text = "Level " + currentLevel;
        }

        Debug.Log("Generated Level " + currentLevel);
    }

    public void RestartGame()
    {
        currentLevel = 0;
        GenerateLevel();
    }
}
