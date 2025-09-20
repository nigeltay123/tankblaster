using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Refs")]
    public BSPDungeonGenerator dungeon;   // drag your BSPDungeonGeneratorObject
    public GameObject enemyPrefab;        // drag the Enemy prefab

    [Header("Difficulty")]
    public int startCount = 2;            // Level 1 enemies
    public int addPerLevel = 2;           // + per level
    public int maxEnemiesCap = 60;

    [Header("Placement")]
    public int maxPlacementTries = 25;    // attempts to find a valid floor cell

    static int alive;
    public static System.Action OnAllEnemiesDefeated;

    readonly System.Random rng = new System.Random();
    readonly List<GameObject> _active = new List<GameObject>();

    public void Clear()
    {
        foreach (var e in _active) if (e) Destroy(e);
        _active.Clear();
        alive = 0;
    }

    public void SpawnForLevel(int level, Transform player)
    {
        Clear();
        if (!dungeon || !enemyPrefab || !player) return;
        var rooms = dungeon.Rooms;
        if (rooms == null || rooms.Count == 0) return;

        Vector3Int playerCell = dungeon.floorsTilemap.WorldToCell(player.position);
        int playerRoomIdx = IndexOfRoomContainingCell(rooms, playerCell);

        int count = Mathf.Clamp(startCount + (level - 1) * addPerLevel, 0, maxEnemiesCap);
        Debug.Log($"[EnemySpawner] Spawning {count} enemies at level {level}");

        for (int i = 0; i < count; i++)
        {
            int roomIdx = ChooseRandomRoomIndexExcluding(rooms.Count, playerRoomIdx);
            var room = rooms[roomIdx];

            if (!TryPickRandomFloorCellInRoom(room, out Vector3Int cell))
                cell = new Vector3Int(room.xMin + room.width / 2, room.yMin + room.height / 2, 0);

            Vector3 pos = dungeon.floorsTilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);
            var enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
            _active.Add(enemy);
        }

        alive = count;
        Debug.Log($"[EnemySpawner] Alive counter set to {alive}");

        if (alive == 0)
        {
            Debug.Log("[EnemySpawner] No enemies spawned, firing event immediately");
            OnAllEnemiesDefeated?.Invoke();
        }
    }

    int IndexOfRoomContainingCell(IReadOnlyList<RectInt> rooms, Vector3Int cell)
    {
        for (int i = 0; i < rooms.Count; i++)
            if (rooms[i].Contains(new Vector2Int(cell.x, cell.y)))
                return i;
        return -1;
    }

    int ChooseRandomRoomIndexExcluding(int roomCount, int excludeIndex)
    {
        if (roomCount <= 1 || excludeIndex < 0) return rng.Next(roomCount);
        int idx;
        do { idx = rng.Next(roomCount); } while (idx == excludeIndex);
        return idx;
    }

    bool TryPickRandomFloorCellInRoom(RectInt room, out Vector3Int cell)
    {
        for (int t = 0; t < maxPlacementTries; t++)
        {
            int x = rng.Next(room.xMin + 1, room.xMax - 1);
            int y = rng.Next(room.yMin + 1, room.yMax - 1);
            var c = new Vector3Int(x, y, 0);

            if (dungeon.floorsTilemap.GetTile(c) == dungeon.floorTile)
            {
                cell = c;
                return true;
            }
        }
        cell = default;
        return false;
    }

    public static void NotifyEnemyDied()
    {
        Debug.Log($"[EnemySpawner] Enemy died. Alive before decrement = {alive}");
        if (--alive <= 0)
        {
            alive = 0;
            Debug.Log("[EnemySpawner] All enemies dead, firing OnAllEnemiesDefeated");
            OnAllEnemiesDefeated?.Invoke();
        }
    }
}
