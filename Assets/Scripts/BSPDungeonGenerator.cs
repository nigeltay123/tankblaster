using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BSPDungeonGenerator : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase floorTile;
    public TileBase wallTile;

    public int dungeonWidth = 50;
    public int dungeonHeight = 50;

    public int minRoomSize = 6;
    public int maxRoomSize = 15;
    public int maxDepth = 5; // Controls how many splits

    private BSPNode rootNode;
    private List<RectInt> rooms;

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

        if (Random.value > 0.5f)
            splitHorizontally = !splitHorizontally;

        if (splitHorizontally)
        {
            int splitY = Random.Range(minRoomSize, node.Rect.height - minRoomSize);
            node.Left = new BSPNode(new RectInt(node.Rect.xMin, node.Rect.yMin, node.Rect.width, splitY));
            node.Right = new BSPNode(new RectInt(node.Rect.xMin, node.Rect.yMin + splitY, node.Rect.width, node.Rect.height - splitY));
        }
        else
        {
            int splitX = Random.Range(minRoomSize, node.Rect.width - minRoomSize);
            node.Left = new BSPNode(new RectInt(node.Rect.xMin, node.Rect.yMin, splitX, node.Rect.height));
            node.Right = new BSPNode(new RectInt(node.Rect.xMin + splitX, node.Rect.yMin, node.Rect.width - splitX, node.Rect.height));
        }

        Split(node.Left, depth + 1);
        Split(node.Right, depth + 1);
    }

    private void CreateRooms(BSPNode node)
    {
        if (node.IsLeaf)
        {
            int roomWidth = Random.Range(minRoomSize, Mathf.Min(maxRoomSize, node.Rect.width - 2));
            int roomHeight = Random.Range(minRoomSize, Mathf.Min(maxRoomSize, node.Rect.height - 2));

            int roomX = Random.Range(node.Rect.xMin + 1, node.Rect.xMax - roomWidth - 1);
            int roomY = Random.Range(node.Rect.yMin + 1, node.Rect.yMax - roomHeight - 1);

            RectInt room = new RectInt(roomX, roomY, roomWidth, roomHeight);
            node.Room = room;
            rooms.Add(room);
        }
        else
        {
            if (node.Left != null)
                CreateRooms(node.Left);
            if (node.Right != null)
                CreateRooms(node.Right);
        }
    }

    private void ConnectRooms(List<RectInt> rooms)
    {
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector2Int roomAPos = GetRoomCenter(rooms[i - 1]);
            Vector2Int roomBPos = GetRoomCenter(rooms[i]);

            if (Random.value < 0.5f)
            {
                CreateHorizontalCorridor(roomAPos.x, roomBPos.x, roomAPos.y);
                CreateVerticalCorridor(roomAPos.y, roomBPos.y, roomBPos.x);
            }
            else
            {
                CreateVerticalCorridor(roomAPos.y, roomBPos.y, roomAPos.x);
                CreateHorizontalCorridor(roomAPos.x, roomBPos.x, roomBPos.y);
            }
        }
    }

    private Vector2Int GetRoomCenter(RectInt room)
    {
        return new Vector2Int(room.xMin + room.width / 2, room.yMin + room.height / 2);
    }

    private void CreateHorizontalCorridor(int xStart, int xEnd, int y)
    {
        for (int x = Mathf.Min(xStart, xEnd); x <= Mathf.Max(xStart, xEnd); x++)
        {
            tilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
        }
    }

    private void CreateVerticalCorridor(int yStart, int yEnd, int x)
    {
        for (int y = Mathf.Min(yStart, yEnd); y <= Mathf.Max(yStart, yEnd); y++)
        {
            tilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
        }
    }

    private void PaintTiles()
    {
        // Paint floor
        foreach (RectInt room in rooms)
        {
            for (int x = room.xMin; x < room.xMax; x++)
            {
                for (int y = room.yMin; y < room.yMax; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
                }
            }
        }

        // Paint walls around floors
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
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0)
        };

        foreach (var dir in directions)
        {
            if (tilemap.GetTile(pos + dir) == floorTile)
                return true;
        }

        return false;
    }
}
