using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(TilemapMesh))]
public class LevelScriptEditor : Editor
{
    private const string normalColliderObjectName = "Collider";
    private const string oneWayColliderObjectName = "OneWay";
    private const float meshSegmentWidth = 1f;
    private const float meshSegmentHeight = 1f;
    private const float textureTileWidth = 8;
    private const float textureTileHeight = 8;
    private const int verticesPerTile = 4;
    private const int trianglesPerTile = 2;
    private const int verticesPerTriangle = 3;
    private const int triangleIndicesPerTile = trianglesPerTile * verticesPerTriangle;
    
    // UV coordinates of the empty point in the texture (left top corner)
    private const float emptyU = 0;
    private const float emptyV = 1;

    [MenuItem("My Tools/Create Mesh")]
    private static void CreateMesh()
    {
        GameObject gameObject = new GameObject("TilemapMesh");
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        
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
        Vector2[] uv = new Vector2[vertexCount];
        for (int i = 0; i < uv.Length; ++i)
        {
            uv[i].Set(emptyU, emptyV);
        }
        mesh.uv = uv;
        
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.SetFloat("PixelSnap", 1);
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

    private Vector2 sceneMouseStartPosition = new Vector2();
    private Vector2 sceneMouseEndPosition = new Vector2();

    private void OnEnable()
    {
        tilemapMesh = (TilemapMesh)target;
        MeshRenderer meshRenderer = tilemapMesh.GetComponent<MeshRenderer>();
        meshFilter = tilemapMesh.GetComponent<MeshFilter>();
        editorTilemapCollider = tilemapMesh.GetComponent<BoxCollider2D>();
        guiMeshCols = tilemapMesh.meshCols;
        guiMeshRows = tilemapMesh.meshRows;

        texture = (Texture2D)meshRenderer.sharedMaterial.mainTexture;
        if (texture)
        {
            // If texture size has changed
            if ((tilemapMesh.textureWidth > 0 && tilemapMesh.textureHeight > 0) &&
                (texture.width != tilemapMesh.textureWidth || texture.height != tilemapMesh.textureHeight))
            {
                // Recalculate mesh UVs.
                // We assume that the image expands down and right.
                Vector2[] newUV = meshFilter.sharedMesh.uv;
                for (int i = 0; i < newUV.Length; ++i)
                {
                    float newU = newUV[i].x * ((float)tilemapMesh.textureWidth / (float)texture.width);
                    float newV = 1 - (1 - newUV[i].y) * ((float)tilemapMesh.textureHeight / (float)texture.height);
                    newUV[i].Set(newU, newV);
                }
                meshFilter.sharedMesh.uv = newUV;
            }

            tilemapMesh.textureWidth = texture.width;
            tilemapMesh.textureHeight = texture.height;
        }

        RestoreTiles();
    }

    private void OnDisable()
    {
        RestoreTiles();
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

        if (tilemapMesh.isSelectionMode)
        {
            if (GUILayout.Button("Selection Mode Off"))
            {
                RestoreTiles();
                tilemapMesh.isSelectionMode = false;
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("Copy selection"))
            {
                tilemapMesh.ResetTilesetSelection();
                MakeBrushFromSceneSelection(false);
                tilemapMesh.isSelectionMode = false;
            }
            if (GUILayout.Button("Cut selection"))
            {
                tilemapMesh.ResetTilesetSelection();
                MakeBrushFromSceneSelection(true);
                tilemapMesh.isSelectionMode = false;
            }
        }
        else
        {
            if (GUILayout.Button("Selection Mode On"))
            {
                tilemapMesh.brushUVs.Clear();
                tilemapMesh.isSelectionMode = true;
                SceneView.RepaintAll();
            }
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
            for (int i = 0; i < newUV.Length; ++i)
            {
                newUV[i].Set(emptyU, emptyV);
            }
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

            GenerateColliders();
        }

        if (GUILayout.Button("Generate Colliders"))
        {
            GenerateColliders();
        }

        if (GUILayout.Button("Clear"))
        {
            meshFilter.sharedMesh.uv = new Vector2[meshFilter.sharedMesh.uv.Length]; // Reset UVs to (0, 0)
            DeleteCollidersIfExist();
        }

        if (texture)
        {
            // Draw a tileset.

            float tilesetScale = 4;
            float tilesetTileWidth = textureTileWidth * tilesetScale;
            float tilesetTileHeight = textureTileHeight * tilesetScale;
            float tilesetWidth = texture.width * tilesetScale;
            float tilesetHeight = texture.height * tilesetScale;

            GUILayout.Space(8);
            GUILayout.Space(tilesetHeight);
            Rect tilesetRect = GUILayoutUtility.GetLastRect();
            tilesetRect.width = tilesetWidth;
            GUI.DrawTexture(new Rect(tilesetRect.xMin, tilesetRect.yMin, tilesetWidth, tilesetHeight), texture, ScaleMode.ScaleToFit);

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
                
                tilemapMesh.tilesetSelectionMinCol = Mathf.Clamp(Mathf.Min(col1, col2), 0, TilesetCols - 1);
                tilemapMesh.tilesetSelectionMinRow = Mathf.Clamp(Mathf.Min(row1, row2), 0, TilesetRows - 1);
                tilemapMesh.tilesetSelectionMaxCol = Mathf.Clamp(Mathf.Max(col1, col2), 0, TilesetCols - 1);
                tilemapMesh.tilesetSelectionMaxRow = Mathf.Clamp(Mathf.Max(row1, row2), 0, TilesetRows - 1);

                if (Event.current.type == EventType.MouseUp)
                {
                    MakeBrushFromTilesetSelection();
                }
                
                Repaint();
            }

#if false
            // Draw selection rectangle
            Handles.DrawLine(new Vector2(mouseStartPosition.x, mouseStartPosition.y), new Vector2(mouseEndPosition.x, mouseStartPosition.y));
            Handles.DrawLine(new Vector2(mouseEndPosition.x, mouseStartPosition.y), new Vector2(mouseEndPosition.x, mouseEndPosition.y));
            Handles.DrawLine(new Vector2(mouseEndPosition.x, mouseEndPosition.y), new Vector2(mouseStartPosition.x, mouseEndPosition.y));
            Handles.DrawLine(new Vector2(mouseStartPosition.x, mouseEndPosition.y), new Vector2(mouseStartPosition.x, mouseStartPosition.y));
#endif
            // Draw a border around the selected tiles.
            float selectionLeft = tilesetRect.xMin + tilemapMesh.tilesetSelectionMinCol * tilesetTileWidth;
            float selectionTop = tilesetRect.yMin + tilemapMesh.tilesetSelectionMinRow * tilesetTileHeight;
            float selectionRight = selectionLeft + tilemapMesh.TilesetSelectionWidth * tilesetTileWidth;
            float selectionBottom = selectionTop + tilemapMesh.TilesetSelectionHeight * tilesetTileHeight;
            Handles.DrawAAPolyLine(10f,
                new Vector3(selectionLeft, selectionBottom),
                new Vector3(selectionRight, selectionBottom),
                new Vector3(selectionRight, selectionTop),
                new Vector3(selectionLeft, selectionTop),
                new Vector3(selectionLeft, selectionBottom));
        }
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

    // Coordinates (column, row) of the solid tiles in the tileset.
    // (0, 0) is the left top tile of the tileset.
    private Vector2[] solidTiles = new Vector2[]
    {
        new Vector2(6, 0), // grass
        new Vector2(0, 1), // wall
        new Vector2(5, 1), // bricks
        new Vector2(6, 1), // ground
    };

    private Vector2[] oneWayTiles = new Vector2[]
    {
        new Vector2(3, 0), // crate 1
        new Vector2(4, 0), // crate 2
        new Vector2(5, 0), // ladder
    };

    private bool IsSolidTile(int meshCol, int meshRow)
    {
        return IsTileInArray(meshCol, meshRow, solidTiles);
    }

    private bool IsOneWayTile(int meshCol, int meshRow)
    {
        return IsTileInArray(meshCol, meshRow, oneWayTiles);
    }

    private bool IsTileInArray(int meshCol, int meshRow, Vector2[] tiles)
    {
        if (meshCol >= 0 && meshCol < tilemapMesh.meshCols && meshRow >= 0 && meshRow < tilemapMesh.meshRows)
        {
            int uvI = (meshCol + tilemapMesh.meshCols * meshRow) * verticesPerTile;
            Vector2 uv = meshFilter.sharedMesh.uv[uvI];
            int tilesetCol = (int)(uv.x / UVTileWidth);
            int tilesetRow = (TilesetRows - 1) - (int)(uv.y / UVTileHeight);
            for (int i = 0; i < tiles.Length; ++i)
            {
                if (tilesetCol == tiles[i].x && tilesetRow == tiles[i].y)
                {
                    return true;
                }
            }
        }
        return false;
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

        if (tilemapMesh.isSelectionMode)
        {
            // SELECTION MODE

            if (editorTilemapCollider.OverlapPoint(mousePos))
            {
                if (Event.current.button == 0)
                {
                    if (Event.current.type == EventType.MouseDown)
                    {
                        sceneMouseStartPosition = sceneMouseEndPosition = mousePos;
                        GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                        SceneView.RepaintAll();
                    }
                    else if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseUp)
                    {
                        sceneMouseEndPosition = mousePos;
                        GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                        SceneView.RepaintAll();
                    }
                }
            }

            Handles.DrawLine(new Vector2(sceneMouseStartPosition.x, sceneMouseStartPosition.y), new Vector2(sceneMouseEndPosition.x, sceneMouseStartPosition.y));
            Handles.DrawLine(new Vector2(sceneMouseEndPosition.x, sceneMouseStartPosition.y), new Vector2(sceneMouseEndPosition.x, sceneMouseEndPosition.y));
            Handles.DrawLine(new Vector2(sceneMouseEndPosition.x, sceneMouseEndPosition.y), new Vector2(sceneMouseStartPosition.x, sceneMouseEndPosition.y));
            Handles.DrawLine(new Vector2(sceneMouseStartPosition.x, sceneMouseEndPosition.y), new Vector2(sceneMouseStartPosition.x, sceneMouseStartPosition.y));

            float selectionMinX = Mathf.Min(sceneMouseStartPosition.x, sceneMouseEndPosition.x);
            float selectionMaxX = Mathf.Max(sceneMouseStartPosition.x, sceneMouseEndPosition.x);
            float selectionMinY = Mathf.Min(sceneMouseStartPosition.y, sceneMouseEndPosition.y);
            float selectionMaxY = Mathf.Max(sceneMouseStartPosition.y, sceneMouseEndPosition.y);

            int meshMinCol = (int)((selectionMinX - editorTilemapCollider.bounds.min.x) / meshSegmentWidth);
            int meshMinRow = (int)((selectionMinY - editorTilemapCollider.bounds.min.y) / meshSegmentWidth);

            int meshMaxCol = (int)((selectionMaxX - editorTilemapCollider.bounds.min.x) / meshSegmentWidth);
            int meshMaxRow = (int)((selectionMaxY - editorTilemapCollider.bounds.min.y) / meshSegmentWidth);

            tilemapMesh.sceneSelectionMinCol = meshMinCol;
            tilemapMesh.sceneSelectionMinRow = meshMinRow;
            tilemapMesh.sceneSelectionMaxCol = meshMaxCol;
            tilemapMesh.sceneSelectionMaxRow = meshMaxRow;

            int selectionWidthInTiles = meshMaxCol - meshMinCol + 1;
            int selectionHeightInTiles = meshMaxRow - meshMinRow + 1;

            //Debug.Log("minCol = " + meshMinCol + ", minRow = " + meshMinRow + ", maxCol = " + meshMaxCol + ", maxRow = " + meshMaxRow);

            float snapSelectionLeft = editorTilemapCollider.bounds.min.x + meshMinCol * meshSegmentWidth;
            float snapSelectionTop = editorTilemapCollider.bounds.min.y + (meshMaxRow + 1) * meshSegmentHeight;
            float snapSelectionRight = snapSelectionLeft + selectionWidthInTiles * meshSegmentWidth;
            float snapSelectionBottom = snapSelectionTop - selectionHeightInTiles * meshSegmentHeight;

            Handles.DrawAAPolyLine(10f,
                new Vector3(snapSelectionLeft, snapSelectionBottom),
                new Vector3(snapSelectionRight, snapSelectionBottom),
                new Vector3(snapSelectionRight, snapSelectionTop),
                new Vector3(snapSelectionLeft, snapSelectionTop),
                new Vector3(snapSelectionLeft, snapSelectionBottom));
        }
        else
        {
            // PAINT MODE

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
                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
            }
        }

        if (SceneView.mouseOverWindow != SceneView.currentDrawingSceneView)
        {
            // Mouse has left the scene window.
            RestoreTiles();
        }
    }

    private void MakeBrushFromTilesetSelection()
    {
        tilemapMesh.brushUVs.Clear();
        tilemapMesh.brushWidth = tilemapMesh.TilesetSelectionWidth;
        tilemapMesh.brushHeight = tilemapMesh.TilesetSelectionHeight;
        for (int tilesetRow = tilemapMesh.tilesetSelectionMinRow; tilesetRow <= tilemapMesh.tilesetSelectionMaxRow; ++tilesetRow)
        {
            for (int tilesetCol = tilemapMesh.tilesetSelectionMinCol; tilesetCol <= tilemapMesh.tilesetSelectionMaxCol; ++tilesetCol)
            {
                int brushTileRowConverted = TilesetRows - tilesetRow - 1; // Convert so that bottom row is zero
                tilemapMesh.brushUVs.Add(new Vector2(tilesetCol * UVTileWidth, brushTileRowConverted * UVTileHeight));
                tilemapMesh.brushUVs.Add(new Vector2(tilesetCol * UVTileWidth, (brushTileRowConverted + 1 ) * UVTileHeight));
                tilemapMesh.brushUVs.Add(new Vector2((tilesetCol + 1) * UVTileWidth, (brushTileRowConverted + 1) * UVTileHeight));
                tilemapMesh.brushUVs.Add(new Vector2((tilesetCol + 1) * UVTileWidth, brushTileRowConverted * UVTileHeight));
            }
        }
    }

    private void MakeBrushFromSceneSelection(bool cut)
    {
        Vector2[] newUV = meshFilter.sharedMesh.uv;

        tilemapMesh.brushUVs.Clear();
        tilemapMesh.brushWidth = tilemapMesh.SceneSelectionWidth;
        tilemapMesh.brushHeight = tilemapMesh.SceneSelectionHeight;

        for (int meshRow = tilemapMesh.sceneSelectionMaxRow; meshRow >= tilemapMesh.sceneSelectionMinRow; --meshRow)
        {
            for (int meshCol = tilemapMesh.sceneSelectionMinCol; meshCol <= tilemapMesh.sceneSelectionMaxCol; ++meshCol)
            {
                int tileIndex = meshCol + meshRow * tilemapMesh.meshCols;
                int vertexI = tileIndex * verticesPerTile;
                for (int i = 0; i < verticesPerTile; ++i)
                {
                    tilemapMesh.brushUVs.Add(newUV[vertexI + i]);
                    if (cut)
                    {
                        newUV[vertexI + i] = new Vector2(0, 0);
                    }
                }
            }
        }

        meshFilter.sharedMesh.uv = newUV;
    }

    private void PaintTiles(Vector2 mousePos, bool saveUVs)
    {
        Vector2[] newUV = meshFilter.sharedMesh.uv;

        int meshBaseCol = (int)((mousePos.x - editorTilemapCollider.bounds.min.x) / meshSegmentWidth);
        int meshBaseRow = (int)((mousePos.y - editorTilemapCollider.bounds.min.y) / meshSegmentWidth);

        tilemapMesh.previewSavedMeshCol = meshBaseCol;
        tilemapMesh.previewSavedMeshRow = meshBaseRow;
        tilemapMesh.previewSavedUVs.Clear();

        int selectionUVIndex = 0;

        if (saveUVs)
        {
            tilemapMesh.previewSavedHeightInTiles = tilemapMesh.brushHeight;
            tilemapMesh.previewSavedWidthInTiles = tilemapMesh.brushWidth;
        }

        for (int meshRow = meshBaseRow;
            meshRow > (meshBaseRow - tilemapMesh.brushHeight) && meshRow >= 0 && meshRow < tilemapMesh.meshRows;
            --meshRow)
        {
            for (int meshCol = meshBaseCol;
                meshCol < (meshBaseCol + tilemapMesh.brushWidth) && meshCol >= 0 && meshCol < tilemapMesh.meshCols;
                ++meshCol)
            {
                int tileIndex = meshCol + meshRow * tilemapMesh.meshCols;
                int vertexI = tileIndex * verticesPerTile;

                for (int i = 0; i < verticesPerTile; ++i)
                {
                    if (saveUVs)
                    {
                        tilemapMesh.previewSavedUVs.Add(newUV[vertexI + i]);
                    }
                    newUV[vertexI + i] = tilemapMesh.brushUVs[selectionUVIndex++];
                }
            }
        }

        meshFilter.sharedMesh.uv = newUV;
    }

    private void RestoreTiles()
    {
        if (tilemapMesh.previewSavedUVs.Count <= 0)
        {
            return;
        }

        if (tilemapMesh.previewSavedUVs.Count % verticesPerTile != 0)
        {
            Debug.LogError("previewSavedUVs.Count is incorrect");
        }

        Vector2[] newUV = meshFilter.sharedMesh.uv;

        int meshBaseCol = tilemapMesh.previewSavedMeshCol;
        int meshBaseRow = tilemapMesh.previewSavedMeshRow;

        for (int meshRow = meshBaseRow;
            meshRow > (meshBaseRow - tilemapMesh.previewSavedHeightInTiles) && meshRow >= 0 && meshRow < tilemapMesh.meshRows;
            --meshRow)
        {
            for (int meshCol = meshBaseCol;
                meshCol < (meshBaseCol + tilemapMesh.previewSavedWidthInTiles) && meshCol >= 0 && meshCol < tilemapMesh.meshCols;
                ++meshCol)
            {
                int tileIndex = meshCol + meshRow * tilemapMesh.meshCols;
                int vertexI = tileIndex * verticesPerTile;
                for (int i = 0; i < verticesPerTile; ++i)
                {
                    newUV[vertexI + i] = tilemapMesh.previewSavedUVs[i];
                }
                tilemapMesh.previewSavedUVs.RemoveRange(0, verticesPerTile);
            }
        }

        meshFilter.sharedMesh.uv = newUV;
    }

    private void GenerateColliders()
    {
        RestoreTiles();
        DeleteCollidersIfExist();
        GenerateNormalCollider();
        GenerateOneWayCollider();
    }

    private void GenerateOneWayCollider()
    {
        // Find start one-way tile, start the collider path
        // Read next tiles in the row
        // If reached the end of the row or a non-one-way tile, end the collider path


        // Gather all one-way tiles.
        List<Vector2> unvisitedOneWayTiles = new List<Vector2>();
        for (int row = 0; row < tilemapMesh.meshRows; ++row)
        {
            for (int col = 0; col < tilemapMesh.meshCols; ++col)
            {
                if (IsOneWayTile(col, row))
                {
                    unvisitedOneWayTiles.Add(new Vector2(col, row));
                }
            }
        }

        // Gather contiguous stripes of one-way tiles.
        List<List<Vector2>> stripes = new List<List<Vector2>>();
        while (unvisitedOneWayTiles.Count > 0)
        {
            List<Vector2> stripe = new List<Vector2>();
            do
            {
                stripe.Add(unvisitedOneWayTiles[0]);
                unvisitedOneWayTiles.RemoveAt(0);
            } while (unvisitedOneWayTiles.Count > 0 && unvisitedOneWayTiles[0].x == (stripe[stripe.Count - 1].x + 1) && unvisitedOneWayTiles[0].y == stripe[0].y);
            stripes.Add(stripe);
        }

         // Create paths for the polygon collider.
        if (stripes.Count > 0)
        {
            GameObject gameObject = new GameObject(oneWayColliderObjectName);
            gameObject.layer = LayerMask.NameToLayer("OneWayPlatform");
            gameObject.transform.parent = tilemapMesh.transform;
            PolygonCollider2D polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
            polygonCollider.pathCount = stripes.Count;

            for (int stripeI = 0; stripeI < stripes.Count; ++stripeI)
            {
                List<Vector2> stripe = stripes[stripeI];
                List<Vector2> pathPoints = new List<Vector2>();
                
                Vector2 topLeft = MeshTileCornerPoint(stripe[0], Corner.TopLeft);
                Vector2 topRight = MeshTileCornerPoint(stripe[stripe.Count - 1], Corner.TopRight);
                const float platformHeight = 0.1f;
                float bottomY = topRight.y - platformHeight;
                Vector2 bottomRight = new Vector2(topRight.x, bottomY);
                Vector2 bottomLeft = new Vector2(topLeft.x, bottomY);
                
                pathPoints.Add(topLeft);
                pathPoints.Add(topRight);
                pathPoints.Add(bottomRight);
                pathPoints.Add(bottomLeft);
                
                polygonCollider.SetPath(stripeI, pathPoints.ToArray());
            }
        }
    }

    private void GenerateNormalCollider()
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
            GameObject gameObject = new GameObject(normalColliderObjectName);
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

    private void DeleteCollidersIfExist()
    {
        Transform normalColliderObject = tilemapMesh.transform.Find(normalColliderObjectName);
        if (normalColliderObject)
            DestroyImmediate(normalColliderObject.gameObject);

        Transform oneWayColliderObject = tilemapMesh.transform.Find(oneWayColliderObjectName);
        if (oneWayColliderObject)
            DestroyImmediate(oneWayColliderObject.gameObject);
    }
}
