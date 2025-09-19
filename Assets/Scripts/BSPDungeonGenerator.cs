using System.Collections.Generic;
using System.Linq;                  // for OrderByDescending
using UnityEngine;
using UnityEngine.Tilemaps;

public class BSPDungeonGenerator : MonoBehaviour
{
    [Header("Tilemap & Tiles (single tilemap setup)")]
    public Tilemap tilemap;         // single tilemap (floors + walls)
    public TileBase floorTile;
    public TileBase wallTile;

    [Header("Map Settings")]
    public int dungeonWidth = 50;
    public int dungeonHeight = 50;

    public int minRoomSize = 6;
    public int maxRoomSize = 15;
    public int maxDepth = 5;        // Controls how many splits

    // BSP tree & rooms
    private BSPNode rootNode;
    private List<RectInt> rooms;

    // Expose rooms (read-only)
    public IReadOnlyList<RectInt> Rooms => rooms;

    /// <summary>
    /// Returns a safe spawn in WORLD space:
    /// - Prefers a floor cell inside the largest room, padded away from walls.
    /// - Falls back to a clamped room center if no padded cell is found.
    /// Call this AFTER GenerateDungeon() (when tiles are painted).
    /// </summary>
    public Vector3 GetSpawnWorldPosition(int padding = 1)
    {
        if (rooms == null || rooms.Count == 0 || tilemap == null)
            return Vector3.zero;

        // pick the largest room to give the player space
        RectInt largest = rooms.OrderByDescending(r => r.width * r.height).First();

        // 1) Try to find a padded floor cell (not hugging walls)
        Vector3Int? paddedCell = FindPaddedFloorCell(largest, padding);
        if (paddedCell.HasValue)
        {
            Vector3 world = tilemap.CellToWorld(paddedCell.Value);
            return world + new Vector3(0.5f, 0.5f, 0f);
        }

        // 2) Fallback: clamped center cell inside room bounds
        var center = new Vector3Int(
            largest.xMin + largest.width  / 2,
            largest.yMin + largest.height / 2,
            0
        );

        center.x = Mathf.Clamp(center.x, largest.xMin + padding, largest.xMax - 1 - padding);
        center.y = Mathf.Clamp(center.y, largest.yMin + padding, largest.yMax - 1 - padding);

        Vector3 fallback = tilemap.CellToWorld(center);
        return fallback + new Vector3(0.5f, 0.5f, 0f);
    }

    /// <summary>
    /// Scans the given room for a floor tile with 'padding' tiles of clearance
    /// from walls (i.e., neighbors are also floor). Returns the first match.
    /// </summary>
    private Vector3Int? FindPaddedFloorCell(RectInt room, int padding)
    {
        // define a smaller interior rectangle to avoid edges
        int xStart = room.xMin + padding;
        int xEnd   = room.xMax - 1 - padding;
        int yStart = room.yMin + padding;
        int yEnd   = room.yMax - 1 - padding;

        if (xStart > xEnd || yStart > yEnd)
            return null; // room too small for requested padding

        for (int y = yStart; y <= yEnd; y++)
        {
            for (int x = xStart; x <= xEnd; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (!IsFloor(cell)) continue;

                // all 4-neighbors must be floor too (keeps us off walls)
                if (IsFloor(cell + Vector3Int.right) &&
                    IsFloor(cell + Vector3Int.left)  &&
                    IsFloor(cell + Vector3Int.up)    &&
                    IsFloor(cell + Vector3Int.down))
                {
                    return cell;
                }
            }
        }
        return null;
    }

    private bool IsFloor(Vector3Int cell)
    {
        var t = tilemap.GetTile(cell);
        return t != null && t == floorTile;
    }

    // ------------------ Generation ------------------

    public void GenerateDungeon()
    {
        tilemap.ClearAllTiles();
        rooms = new List<RectInt>();

        rootNode = new BSPNode(new RectInt(0, 0, dungeonWidth, dungeonHeight));
        Split(rootNode, 0);
        CreateRooms(rootNode);
        ConnectRooms(rooms);
        PaintTiles();
    }

    private void Split(BSPNode node, int depth)
    {
        if (depth >= maxDepth || node.Rect.width < 2 * minRoomSize || node.Rect.height < 2 * minRoomSize)
            return;

        bool splitHorizontally = node.Rect.width < node.Rect.height;
        if (Random.value > 0.5f) splitHorizontally = !splitHorizontally;

        if (splitHorizontally)
        {
            int splitY = Random.Range(minRoomSize, node.Rect.height - minRoomSize);
            node.Left  = new BSPNode(new RectInt(node.Rect.xMin, node.Rect.yMin, node.Rect.width, splitY));
            node.Right = new BSPNode(new RectInt(node.Rect.xMin, node.Rect.yMin + splitY, node.Rect.width, node.Rect.height - splitY));
        }
        else
        {
            int splitX = Random.Range(minRoomSize, node.Rect.width - minRoomSize);
            node.Left  = new BSPNode(new RectInt(node.Rect.xMin, node.Rect.yMin, splitX, node.Rect.height));
            node.Right = new BSPNode(new RectInt(node.Rect.xMin + splitX, node.Rect.yMin, node.Rect.width - splitX, node.Rect.height));
        }

        Split(node.Left,  depth + 1);
        Split(node.Right, depth + 1);
    }

    private void CreateRooms(BSPNode node)
    {
        if (node.IsLeaf)
        {
            int roomWidth  = Random.Range(minRoomSize, Mathf.Min(maxRoomSize, node.Rect.width  - 2));
            int roomHeight = Random.Range(minRoomSize, Mathf.Min(maxRoomSize, node.Rect.height - 2));

            int roomX = Random.Range(node.Rect.xMin + 1, node.Rect.xMax - roomWidth  - 1);
            int roomY = Random.Range(node.Rect.yMin + 1, node.Rect.yMax - roomHeight - 1);

            RectInt room = new RectInt(roomX, roomY, roomWidth, roomHeight);
            node.Room = room;
            rooms.Add(room);
        }
        else
        {
            if (node.Left  != null) CreateRooms(node.Left);
            if (node.Right != null) CreateRooms(node.Right);
        }
    }

    private void ConnectRooms(List<RectInt> allRooms)
    {
        for (int i = 1; i < allRooms.Count; i++)
        {
            Vector2Int a = GetRoomCenter(allRooms[i - 1]);
            Vector2Int b = GetRoomCenter(allRooms[i]);

            if (Random.value < 0.5f)
            {
                CreateHorizontalCorridor(a.x, b.x, a.y);
                CreateVerticalCorridor(a.y, b.y, b.x);
            }
            else
            {
                CreateVerticalCorridor(a.y, b.y, a.x);
                CreateHorizontalCorridor(a.x, b.x, b.y);
            }
        }
    }

    private Vector2Int GetRoomCenter(RectInt room)
        => new Vector2Int(room.xMin + room.width / 2, room.yMin + room.height / 2);

    private void CreateHorizontalCorridor(int xStart, int xEnd, int y)
    {
        for (int x = Mathf.Min(xStart, xEnd); x <= Mathf.Max(xStart, xEnd); x++)
            tilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
    }

    private void CreateVerticalCorridor(int yStart, int yEnd, int x)
    {
        for (int y = Mathf.Min(yStart, yEnd); y <= Mathf.Max(yStart, yEnd); y++)
            tilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
    }

    private void PaintTiles()
    {
        // paint floors (rooms)
        foreach (RectInt room in rooms)
        {
            for (int x = room.xMin; x < room.xMax; x++)
                for (int y = room.yMin; y < room.yMax; y++)
                    tilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
        }

        // paint walls around floors
        BoundsInt bounds = tilemap.cellBounds;

        for (int x = bounds.xMin - 1; x <= bounds.xMax + 1; x++)
        {
            for (int y = bounds.yMin - 1; y <= bounds.yMax + 1; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                if (tilemap.GetTile(pos) == null && HasFloorNeighbor(pos))
                {
                    tilemap.SetTile(pos, wallTile);
                }
            }
        }
    }

    private bool HasFloorNeighbor(Vector3Int pos)
    {
        Vector3Int[] directions = {
            new Vector3Int( 1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int( 0, 1, 0),
            new Vector3Int( 0,-1, 0)
        };

        foreach (var dir in directions)
        {
            if (tilemap.GetTile(pos + dir) == floorTile)
                return true;
        }
        return false;
    }
}
