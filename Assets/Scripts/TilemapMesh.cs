using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TilemapMesh : MonoBehaviour
{
    public bool isSelectionMode = false;

    public int meshCols = 1;
    public int meshRows = 1;

    public int textureWidth = 0;
    public int textureHeight = 0;

    public int tilesetSelectionMinCol = 0;
    public int tilesetSelectionMinRow = 0;
    public int tilesetSelectionMaxCol = 0;
    public int tilesetSelectionMaxRow = 0;
    public int TilesetSelectionWidth
    {
        get { return tilesetSelectionMaxCol - tilesetSelectionMinCol + 1; }
    }
    public int TilesetSelectionHeight
    {
        get { return tilesetSelectionMaxRow - tilesetSelectionMinRow + 1; }
    }
    public void ResetTilesetSelection()
    {
        tilesetSelectionMinCol = 0;
        tilesetSelectionMinRow = 0;
        tilesetSelectionMaxCol = 0;
        tilesetSelectionMaxRow = 0;
    }

    public int sceneSelectionMinCol = 0;
    public int sceneSelectionMinRow = 0;
    public int sceneSelectionMaxCol = 0;
    public int sceneSelectionMaxRow = 0;
    public int SceneSelectionWidth
    {
        get { return sceneSelectionMaxCol - sceneSelectionMinCol + 1; }
    }
    public int SceneSelectionHeight
    {
        get { return sceneSelectionMaxRow - sceneSelectionMinRow + 1; }
    }

    public List<Vector2> previewSavedUVs = new List<Vector2>();
    public int previewSavedMeshCol = 0;
    public int previewSavedMeshRow = 0;
    public int previewSavedWidthInTiles = 0;
    public int previewSavedHeightInTiles = 0;

    public List<Vector2> brushUVs = new List<Vector2>();
    public int brushWidth = 0;
    public int brushHeight = 0;
}
