using UnityEngine;

public class BSPNode
{
    public RectInt Rect;       // The space this node represents
    public RectInt? Room;      // The room inside this node (nullable RectInt)

    public BSPNode Left;
    public BSPNode Right;

    public BSPNode(RectInt rect)
    {
        Rect = rect;
    }

    public bool IsLeaf => Left == null && Right == null;
}
