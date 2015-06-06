using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TilemapMesh : MonoBehaviour
{
    public int meshCols = 1;
    public int meshRows = 1;

    public int textureWidth = 0;
    public int textureHeight = 0;

    public int brushStartTileCol = 0;
    public int brushStartTileRow = 0;
    public int brushEndTileCol = 0;
    public int brushEndTileRow = 0;

    // Data that is saved when previewing the tile brush in the scene view to be restored later.
    public List<Vector2> savedUVs = new List<Vector2>();
    public int savedMeshCol = 0;
    public int savedMeshRow = 0;

    public int BrushWidth
    {
        get { return brushEndTileCol - brushStartTileCol + 1; }
    }

    public int BrushHeight
    {
        get { return brushEndTileRow - brushStartTileRow + 1; }
    }
}
