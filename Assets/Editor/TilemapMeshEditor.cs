using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(TilemapMesh))]
public class LevelScriptEditor : Editor
{
    private const string colliderObjectName = "Collider";
    private const float meshSegmentWidth = 1f;
    private const float meshSegmentHeight = 1f;
    private const float textureTileWidth = 8;
    private const float textureTileHeight = 8;
    private const int verticesPerTile = 4;
    private const int trianglesPerTile = 2;
    private const int verticesPerTriangle = 3;
    private const int triangleIndicesPerTile = trianglesPerTile * verticesPerTriangle;

    [MenuItem("My Tools/Create Mesh")]
    private static void CreateMesh()
    {
        GameObject gameObject = new GameObject("TilemapMesh");
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

        // NOTE: Maybe I should use DestroyImmediate() on the generated material when game object is deleted
        // to prevent "leaked objects" error?
        // See http://answers.unity3d.com/questions/38960/cleaning-up-leaked-objects-in-scene-since-no-game.html
        Mesh mesh = new Mesh();
        mesh.name = "TilemapMesh";
        Vector3[] vertices;
        int[] triangles;
        Vector2 centerPos;
        int meshCols = 1;
        int meshRows = 1;
        GenerateMeshData(meshCols, meshRows, out vertices, out triangles, out centerPos);
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        int vertexCount = meshRows * meshCols * verticesPerTile;
        mesh.uv = new Vector2[vertexCount];
        mesh.RecalculateNormals();
        
        meshFilter.mesh = mesh;
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        Material material = AssetDatabase.LoadAssetAtPath("Assets/Materials/test.mat", typeof(Material)) as Material;
        renderer.material = material;
        gameObject.AddComponent<TilemapMesh>();
        gameObject.AddComponent<BoxCollider2D>();
        gameObject.transform.position = centerPos;
    }

    private int guiMeshCols = 0;
    private int guiMeshRows = 0;

    private TilemapMesh tilemapMesh;
    private Texture2D texture;
    private MeshFilter meshFilter;
    private BoxCollider2D editorTilemapCollider;

    private Vector2 mouseStartPosition = new Vector2();
    private Vector2 mouseEndPosition = new Vector2();

    private void OnEnable()
    {
        tilemapMesh = (TilemapMesh)target;
        MeshRenderer meshRenderer = tilemapMesh.GetComponent<MeshRenderer>();
        texture = (Texture2D)meshRenderer.sharedMaterial.mainTexture;
        meshFilter = tilemapMesh.GetComponent<MeshFilter>();
        editorTilemapCollider = tilemapMesh.GetComponent<BoxCollider2D>();
        guiMeshCols = tilemapMesh.meshCols;
        guiMeshRows = tilemapMesh.meshRows;
        
        // If texture size has changed
        if ((tilemapMesh.textureWidth > 0 && tilemapMesh.textureHeight > 0) &&
            (texture.width != tilemapMesh.textureWidth || texture.height != tilemapMesh.textureHeight))
        {
            // Recalculate mesh UVs
            Vector2[] newUV = meshFilter.sharedMesh.uv;
            for (int i = 0; i < newUV.Length; ++i)
            {
                float kX = (float)tilemapMesh.textureWidth / (float)texture.width;
                float kY = (float)tilemapMesh.textureHeight / (float)texture.height;
                newUV[i] = new Vector2(kX * newUV[i].x, kY * newUV[i].y);
            }
            meshFilter.sharedMesh.uv = newUV;
        }
        
        tilemapMesh.textureWidth = texture.width;
        tilemapMesh.textureHeight = texture.height;
    }

    public override void OnInspectorGUI()
    {
        guiMeshCols = EditorGUILayout.IntField("Mesh Columns", guiMeshCols);
        guiMeshRows = EditorGUILayout.IntField("Mesh Rows", guiMeshRows);
        if (guiMeshCols < 1)
        {
            guiMeshCols = 1;
        }
        if (guiMeshRows < 1)
        {
            guiMeshRows = 1;
        }
        
        if (GUILayout.Button("Resize Mesh"))
        {
            Vector3[] newVertices;
            int[] newTriangles;
            Vector2 centerPos;
            GenerateMeshData(guiMeshCols, guiMeshRows, out newVertices, out newTriangles, out centerPos);
            
            // Copy UV data.
            int vertexCount = guiMeshCols * guiMeshRows * verticesPerTile;
            Vector2[] newUV = new Vector2[vertexCount];
            int minCols = Mathf.Min(guiMeshCols, tilemapMesh.meshCols);
            int minRows = Mathf.Min(guiMeshRows, tilemapMesh.meshRows);
            for (int row = 0; row < minRows; ++row)
            {
                for (int col = 0; col < minCols; ++col)
                {
                    int newTileI = col + row * guiMeshCols;
                    int oldTileI = col + row * tilemapMesh.meshCols;
                    for (int i = 0; i < verticesPerTile; ++i)
                    {
                        int i1 = newTileI*verticesPerTile + i;
                        int i2 = oldTileI*verticesPerTile + i;
                        newUV[i1] = meshFilter.sharedMesh.uv[i2];
                    }
                }
            }

            meshFilter.sharedMesh.Clear();
            meshFilter.sharedMesh.vertices = newVertices;
            meshFilter.sharedMesh.triangles = newTriangles;
            meshFilter.sharedMesh.uv = newUV;

            editorTilemapCollider.size = new Vector3(guiMeshCols * meshSegmentWidth, guiMeshRows * meshSegmentHeight, 0);
            tilemapMesh.gameObject.transform.position = centerPos;
            
            tilemapMesh.meshCols = guiMeshCols;
            tilemapMesh.meshRows = guiMeshRows;

            GenerateCollider();
        }

        if (GUILayout.Button("Generate Collider"))
        {
            GenerateCollider();
        }

        if (GUILayout.Button("Clear"))
        {
            meshFilter.sharedMesh.uv = new Vector2[meshFilter.sharedMesh.uv.Length]; // Reset UVs to (0, 0)
            DeleteColliderIfExists();
        }

        // Draw a tileset.

        float tilesetScale = 8;
        float tilesetTileWidth = textureTileWidth * tilesetScale;
        float tilesetTileHeight = textureTileHeight * tilesetScale;
        float tilesetWidth = texture.width * tilesetScale;
        float tilesetHeight = texture.height * tilesetScale;

        GUILayout.Space(8);
        GUILayout.Space(tilesetHeight);
        Rect tilesetRect = GUILayoutUtility.GetLastRect();
        tilesetRect.width = tilesetWidth;
        GUI.DrawTexture(new Rect(tilesetRect.xMin, tilesetRect.yMin, tilesetWidth, tilesetHeight), texture, ScaleMode.ScaleToFit);

        Texture2D overlayTexture = new Texture2D(1, 1);
        overlayTexture.SetPixel(0, 0, new Color(1, 1, 1, 0.5f));

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && tilesetRect.Contains(Event.current.mousePosition))
        {
            mouseStartPosition = Event.current.mousePosition;
        }

        if ((Event.current.type == EventType.MouseDrag) ||
            (Event.current.type == EventType.MouseUp && Event.current.button == 0 && tilesetRect.Contains(Event.current.mousePosition)))
        {
            mouseEndPosition = Event.current.mousePosition;
            int col1 = (int)((mouseStartPosition.x - tilesetRect.x) / tilesetTileWidth);
            int row1 = (int)((mouseStartPosition.y - tilesetRect.y) / tilesetTileHeight);
            int col2 = (int)((mouseEndPosition.x - tilesetRect.x) / tilesetTileWidth);
            int row2 = (int)((mouseEndPosition.y - tilesetRect.y) / tilesetTileHeight);
            if (col1 < col2)
            {
                tilemapMesh.brushStartTileCol = col1;
                tilemapMesh.brushEndTileCol = col2;
            }
            else
            {
                tilemapMesh.brushStartTileCol = col2;
                tilemapMesh.brushEndTileCol = col1;
            }
            if (row1 < row2)
            {
                tilemapMesh.brushStartTileRow = row1;
                tilemapMesh.brushEndTileRow = row2;
            }
            else
            {
                tilemapMesh.brushStartTileRow = row2;
                tilemapMesh.brushEndTileRow = row1;
            }
            Repaint();
        }

        Handles.DrawLine(new Vector2(mouseStartPosition.x, mouseStartPosition.y), new Vector2(mouseEndPosition.x, mouseStartPosition.y));
        Handles.DrawLine(new Vector2(mouseEndPosition.x, mouseStartPosition.y), new Vector2(mouseEndPosition.x, mouseEndPosition.y));
        Handles.DrawLine(new Vector2(mouseEndPosition.x, mouseEndPosition.y), new Vector2(mouseStartPosition.x, mouseEndPosition.y));
        Handles.DrawLine(new Vector2(mouseStartPosition.x, mouseEndPosition.y), new Vector2(mouseStartPosition.x, mouseStartPosition.y));

        Rect overlayRect = new Rect(tilesetRect.xMin + tilemapMesh.brushStartTileCol * tilesetTileWidth, tilesetRect.yMin + tilemapMesh.brushStartTileRow * tilesetTileHeight, BrushWidth * tilesetTileWidth, BrushHeight * tilesetTileHeight);
        GUI.DrawTexture(overlayRect, overlayTexture, ScaleMode.StretchToFill, true);
    }

    private int BrushWidth
    {
        get { return tilemapMesh.brushEndTileCol - tilemapMesh.brushStartTileCol + 1; }
    }

    private int BrushHeight
    {
        get { return tilemapMesh.brushEndTileRow - tilemapMesh.brushStartTileRow + 1; }
    }

    enum Corner
    {
        BottomLeft,
        BottomRight,
        TopLeft,
        TopRight,
    }

    enum Direction
    {
        Right,
        Left,
        Up,
        Down,
    }

    private int TilesetCols
    {
        get { return (int)(texture.width / textureTileWidth); }
    }

    private int TilesetRows
    {
        get { return (int)(texture.height / textureTileHeight); }
    }

    private float UVTileWidth
    {
        get { return 1.0f / (float)TilesetCols; }
    }

    private float UVTileHeight
    {
        get { return 1.0f / (float)TilesetRows; }
    }

    private bool IsSolidTile(int col, int row)
    {
        bool result = false;
        if (col >= 0 && col < tilemapMesh.meshCols && row >= 0 && row < tilemapMesh.meshRows)
        {
            int uvI = (col + tilemapMesh.meshCols * row) * verticesPerTile;
            Vector2 uv = meshFilter.sharedMesh.uv[uvI];
            int tilesetCol = (int)(uv.x / UVTileWidth);
            int tilesetRow = (int)(uv.y / UVTileHeight);
            // For now we just hardcode second bottom tile in the tileset as a single solid tile
            result = (tilesetCol == 1 && tilesetRow == 0);
        }
        return result;
    }

    private bool IsSolidTile(Vector2 tile)
    {
        return IsSolidTile((int)tile.x, (int)tile.y);
    }

    private Vector2 MeshTileCornerPoint(int col, int row, Corner corner)
    {
        Vector2 result = new Vector2();
        switch (corner)
        {
            case Corner.BottomRight:
                ++col;
                break;

            case Corner.TopLeft:
                ++row;
                break;

            case Corner.TopRight:
                ++col;
                ++row;
                break;
        }
        result.x = editorTilemapCollider.bounds.min.x + col * meshSegmentWidth;
        result.y = editorTilemapCollider.bounds.min.y + row * meshSegmentHeight;
        return result;
    }

    private Vector2 MeshTileCornerPoint(Vector2 tile, Corner corner)
    {
        return MeshTileCornerPoint((int)tile.x, (int)tile.y, corner);
    }

    private static void GenerateMeshData(int cols, int rows, out Vector3[] vertices, out int[] triangles, out Vector2 centerPos)
    {
        int tilesCount = rows * cols;
        int vertexCount = tilesCount * verticesPerTile;
        float meshHalfWidth = cols * meshSegmentWidth / 2f;
        float meshHalfHeight = rows * meshSegmentHeight / 2f;

        vertices = new Vector3[vertexCount];
        triangles = new int[tilesCount * triangleIndicesPerTile];
        centerPos = new Vector2(meshHalfWidth, meshHalfHeight);

        for (int i = 0; i < tilesCount; ++i)
        {
            int col = i % cols;
            int row = i / cols;
            float x = -meshHalfWidth + col * meshSegmentWidth;
            float y = -meshHalfHeight + row * meshSegmentHeight;
            int vertexI = i * verticesPerTile;

            const float z = 0;
            vertices[vertexI + 0] = new Vector3(x, y, z);
            vertices[vertexI + 1] = new Vector3(x, y + meshSegmentHeight, z);
            vertices[vertexI + 2] = new Vector3(x + meshSegmentWidth, y + meshSegmentHeight, z);
            vertices[vertexI + 3] = new Vector3(x + meshSegmentWidth, y, z);

            int trianglesI = i * triangleIndicesPerTile;
            triangles[trianglesI + 0] = vertexI + 0;
            triangles[trianglesI + 1] = vertexI + 1;
            triangles[trianglesI + 2] = vertexI + 2;
            triangles[trianglesI + 3] = vertexI + 0;
            triangles[trianglesI + 4] = vertexI + 2;
            triangles[trianglesI + 5] = vertexI + 3;
        }
    }

    private void OnSceneGUI()
    {
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Vector2 mousePos = new Vector2(mouseRay.origin.x, mouseRay.origin.y);

        if (Event.current.type == EventType.MouseMove)
        {
            RestoreTiles();
            if (editorTilemapCollider.OverlapPoint(mousePos))
            {
                PaintTiles(mousePos, true);
            }
        }
        
        if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) &&
            Event.current.button == 0 &&
            editorTilemapCollider.OverlapPoint(mousePos))
        {
            PaintTiles(mousePos, false);

            // Prevent other instruments from gaining focus while painting
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            GUIUtility.hotControl = controlId;
            Event.current.Use();
        }
    }

    // Data that is saved when previewing the tile brush in the scene view to be restored later.
    private List<Vector2> savedUVs = new List<Vector2>();
    private int savedMeshCol = 0;
    private int savedMeshRow = 0;

    // Paint tiles using a rectangular brush of tiles selected in the tileset.
    private void PaintTiles(Vector2 mousePos, bool saveUVs)
    {
        Vector2[] newUV = meshFilter.sharedMesh.uv;

        int meshBaseCol = (int)((mousePos.x - editorTilemapCollider.bounds.min.x) / meshSegmentWidth);
        int meshBaseRow = (int)((mousePos.y - editorTilemapCollider.bounds.min.y) / meshSegmentWidth);

        savedMeshCol = meshBaseCol;
        savedMeshRow = meshBaseRow;
        savedUVs.Clear();

        for (int meshRow = meshBaseRow, tilesetRow = tilemapMesh.brushStartTileRow;
            meshRow > (meshBaseRow - BrushHeight) && meshRow >= 0 && meshRow < tilemapMesh.meshRows;
            --meshRow, ++tilesetRow)
        {
            for (int meshCol = meshBaseCol, tilesetCol = tilemapMesh.brushStartTileCol;
                meshCol < (meshBaseCol + BrushWidth) && meshCol >= 0 && meshCol < tilemapMesh.meshCols;
                ++meshCol, ++tilesetCol)
            {
                int tileIndex = meshCol + meshRow * tilemapMesh.meshCols;
                int brushTileRowConverted = TilesetRows - tilesetRow - 1; // Convert so that bottom row is zero
                int vertexI = tileIndex * verticesPerTile;

                if (saveUVs)
                {
                    savedUVs.Add(newUV[vertexI + 0]);
                    savedUVs.Add(newUV[vertexI + 1]);
                    savedUVs.Add(newUV[vertexI + 2]);
                    savedUVs.Add(newUV[vertexI + 3]);
                }

                newUV[vertexI + 0] = new Vector2(tilesetCol * UVTileWidth, brushTileRowConverted * UVTileHeight);
                newUV[vertexI + 1] = new Vector2(tilesetCol * UVTileWidth, brushTileRowConverted * UVTileHeight + UVTileHeight);
                newUV[vertexI + 2] = new Vector2(tilesetCol * UVTileWidth + UVTileWidth, brushTileRowConverted * UVTileHeight + UVTileHeight);
                newUV[vertexI + 3] = new Vector2(tilesetCol * UVTileWidth + UVTileWidth, brushTileRowConverted * UVTileHeight);
            }
        }

        meshFilter.sharedMesh.uv = newUV;
    }

    private void RestoreTiles()
    {
        if (savedUVs.Count <= 0)
        {
            return;
        }

        DebugUtils.Assert(savedUVs.Count % verticesPerTile == 0);

        Vector2[] newUV = meshFilter.sharedMesh.uv;

        int meshBaseCol = savedMeshCol;
        int meshBaseRow = savedMeshRow;

        for (int meshRow = meshBaseRow;
            meshRow > (meshBaseRow - BrushHeight) && meshRow >= 0 && meshRow < tilemapMesh.meshRows;
            --meshRow)
        {
            for (int meshCol = meshBaseCol;
                meshCol < (meshBaseCol + BrushWidth) && meshCol >= 0 && meshCol < tilemapMesh.meshCols;
                ++meshCol)
            {
                int tileIndex = meshCol + meshRow * tilemapMesh.meshCols;
                int vertexI = tileIndex * verticesPerTile;
                for (int i = 0; i < verticesPerTile; ++i)
                {
                    newUV[vertexI + i] = savedUVs[i];
                }
                savedUVs.RemoveRange(0, verticesPerTile);
            }
        }

        meshFilter.sharedMesh.uv = newUV;
    }

    private void GenerateCollider()
    {
        // Tilemap game object has a child game object that contains one polygon collider
        // that encompasses all solid tiles.

        // Gather all solid tiles.
        List<Vector2> unvisitedSolidTiles = new List<Vector2>();
        for (int row = 0; row < tilemapMesh.meshRows; ++row)
        {
            for (int col = 0; col < tilemapMesh.meshCols; ++col)
            {
                if (IsSolidTile(col, row))
                {
                    unvisitedSolidTiles.Add(new Vector2(col, row));
                }
            }
        }

        // Gather "islands" of contiguous tiles.
        List<List<Vector2>> solidIslands = new List<List<Vector2>>();
        while (unvisitedSolidTiles.Count > 0)
        {
            List<Vector2> island = new List<Vector2>();
            List<Vector2> tilesToVisit = new List<Vector2>();
            tilesToVisit.Add(unvisitedSolidTiles[0]);
            unvisitedSolidTiles.RemoveAt(0);
            while (tilesToVisit.Count > 0)
            {
                Vector2 tile = tilesToVisit[0];
                island.Add(tile);
                tilesToVisit.RemoveAt(0);
                Vector2[] neighbours = new Vector2[]
                {
                    new Vector2(tile.x + 1, tile.y),
                    new Vector2(tile.x - 1, tile.y),
                    new Vector2(tile.x, tile.y + 1),
                    new Vector2(tile.x, tile.y - 1),
                };
                foreach (Vector2 neighbour in neighbours)
                {
                    if (unvisitedSolidTiles.Contains(neighbour))
                    {
                        tilesToVisit.Add(neighbour);
                        unvisitedSolidTiles.Remove(neighbour);
                    }
                }
            }
            solidIslands.Add(island);
        }

        // Create paths for the polygon collider.
        if (solidIslands.Count > 0)
        {
            DeleteColliderIfExists();
            GameObject gameObject = new GameObject(colliderObjectName);
            gameObject.transform.parent = tilemapMesh.transform;
            PolygonCollider2D polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
            polygonCollider.pathCount = solidIslands.Count;
            
            for (int islandI = 0; islandI < solidIslands.Count; ++islandI)
            {
                // We traverse island's outline and create new path points where outline has corners.

                List<Vector2> island = solidIslands[islandI];
                List<Vector2> pathPoints = new List<Vector2>();
                Vector2 currentTile = island[0];
                pathPoints.Add(MeshTileCornerPoint(currentTile, Corner.BottomLeft));
                Direction direction = Direction.Right;
                bool working = true;
                while (working)
                {
                    Vector2? newPathPoint = null;

                    Vector2 rightNeighbor = new Vector2(currentTile.x + 1, currentTile.y);
                    Vector2 bottomRightNeighbor = new Vector2(currentTile.x + 1, currentTile.y - 1);
                    Vector2 topRightNeighbor = new Vector2(currentTile.x + 1, currentTile.y + 1);
                    Vector2 leftNeighbor = new Vector2(currentTile.x - 1, currentTile.y);
                    Vector2 bottomLeftNeighbor = new Vector2(currentTile.x - 1, currentTile.y - 1);
                    Vector2 topLeftNeighbor = new Vector2(currentTile.x - 1, currentTile.y + 1);
                    Vector2 topNeighbor = new Vector2(currentTile.x, currentTile.y + 1);
                    Vector2 bottomNeighbor = new Vector2(currentTile.x, currentTile.y - 1);

                    switch (direction)
                    {
                        // Moving right along the bottom edge
                        case Direction.Right:
                            if (island.Contains(rightNeighbor))
                            {
                                if (island.Contains(bottomRightNeighbor))
                                {
                                    newPathPoint = MeshTileCornerPoint(currentTile, Corner.BottomRight);
                                    direction = Direction.Down;
                                    currentTile = bottomRightNeighbor;
                                }
                                else
                                {
                                    currentTile = rightNeighbor;
                                }
                            }
                            else
                            {
                                newPathPoint = MeshTileCornerPoint(currentTile, Corner.BottomRight);
                                direction = Direction.Up;
                            }
                            break;

                        // Moving left along the top edge
                        case Direction.Left:
                            if (island.Contains(leftNeighbor))
                            {
                                if (island.Contains(topLeftNeighbor))
                                {
                                    newPathPoint = MeshTileCornerPoint(currentTile, Corner.TopLeft);
                                    direction = Direction.Up;
                                    currentTile = topLeftNeighbor;
                                }
                                else
                                {
                                    currentTile = leftNeighbor;
                                }
                            }
                            else
                            {
                                newPathPoint = MeshTileCornerPoint(currentTile, Corner.TopLeft);
                                direction = Direction.Down;
                            }
                            break;

                        // Moving up along the right edge
                        case Direction.Up:
                            if (island.Contains(topNeighbor))
                            {
                                if (island.Contains(topRightNeighbor))
                                {
                                    newPathPoint = MeshTileCornerPoint(currentTile, Corner.TopRight);
                                    direction = Direction.Right;
                                    currentTile = topRightNeighbor;
                                }
                                else
                                {
                                    currentTile = topNeighbor;
                                }
                            }
                            else
                            {
                                newPathPoint = MeshTileCornerPoint(currentTile, Corner.TopRight);
                                direction = Direction.Left;
                            }
                            break;

                        // Moving down along the left edge
                        case Direction.Down:
                            if (island.Contains(bottomNeighbor))
                            {
                                if (island.Contains(bottomLeftNeighbor))
                                {
                                    newPathPoint = MeshTileCornerPoint(currentTile, Corner.BottomLeft);
                                    direction = Direction.Left;
                                    currentTile = bottomLeftNeighbor;
                                }
                                else
                                {
                                    currentTile = bottomNeighbor;
                                }
                            }
                            else
                            {
                                newPathPoint = MeshTileCornerPoint(currentTile, Corner.BottomLeft);
                                direction = Direction.Right;
                            }
                            break;
                    }

                    if (newPathPoint != null)
                    {
                        if (newPathPoint == pathPoints[0])
                        {
                            working = false;
                        }
                        else
                        {
                            pathPoints.Add((Vector2)newPathPoint);
                        }
                    }
                }
                polygonCollider.SetPath(islandI, pathPoints.ToArray());
            }
        }
    }

    private void DeleteColliderIfExists()
    {
        Transform colliderObject = tilemapMesh.transform.Find(colliderObjectName);
        if (colliderObject)
        {
            DestroyImmediate(colliderObject.gameObject);
        }
    }
}
