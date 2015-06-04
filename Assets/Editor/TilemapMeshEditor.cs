using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(TilemapMesh))]
public class LevelScriptEditor : Editor
{
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

        // TODO: Maybe I should use DestroyImmediate() on the generated material when game object is deleted
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

    private int selectedTileCol = 0;
    private int selectedTileRow = 0;
    private int guiMeshCols = 0;
    private int guiMeshRows = 0;

    private TilemapMesh tilemapMesh;
    private Texture2D texture;
    private MeshFilter meshFilter;
    private BoxCollider2D editorTilemapCollider;

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
        }

        if (GUILayout.Button("Generate collider"))
        {
            // Tilemap game object has a child game object that contains one polygon collider
            // that encompasses all solid tiles.
            
            const string colliderObjectName = "Collider";
            PolygonCollider2D polygonCollider;
            Transform colliderObject = tilemapMesh.transform.Find(colliderObjectName);
            if (colliderObject)
            {
                polygonCollider = colliderObject.GetComponent<PolygonCollider2D>();
            }
            else
            {
                GameObject gameObject = new GameObject(colliderObjectName);
                gameObject.transform.parent = tilemapMesh.transform;
                polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
            }

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
                while (tilesToVisit.Count > 0)
                {
                    Vector2 tile = tilesToVisit[0];
                    island.Add(tile);
                    tilesToVisit.RemoveAt(0);
                    unvisitedSolidTiles.RemoveAt(0);
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
                        }
                    }
                }
                solidIslands.Add(island);
            }

            // Create paths for the polygon collider.
            if (solidIslands.Count > 0)
            {
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

        // Draw a tileset.

        float tilesetScale = 8;
        float tilesetTileWidth = textureTileWidth * tilesetScale;
        float tilesetTileHeight = textureTileHeight * tilesetScale;
        float tilesetWidth = texture.width * tilesetScale;
        float tilesetHeight = texture.height * tilesetScale;

        GUILayout.Space(8);
        GUILayout.Space(tilesetHeight);
        Rect tilesetRect = new Rect(GUILayoutUtility.GetLastRect());
        tilesetRect.width = tilesetWidth;
        GUI.DrawTexture(new Rect(tilesetRect.xMin, tilesetRect.yMin, tilesetWidth, tilesetHeight), texture, ScaleMode.ScaleToFit);

        Texture2D overlayTexture = new Texture2D(1, 1);
        overlayTexture.SetPixel(0, 0, new Color(1, 1, 1, 0.5f));

        // Can draw lines like this:
        // Handles.DrawLine(new Vector3(lastRect.x, lastRect.y, 0), new Vector3(lastRect.x + 100, lastRect.y + 100, 0));
        
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && tilesetRect.Contains(Event.current.mousePosition))
        {
            selectedTileCol = (int)((Event.current.mousePosition.x - tilesetRect.x) / tilesetTileWidth);
            selectedTileRow = (int)((Event.current.mousePosition.y - tilesetRect.y) / tilesetTileHeight);
            Repaint();
        }
        
        GUI.DrawTexture(new Rect(tilesetRect.xMin + selectedTileCol*tilesetTileWidth, tilesetRect.yMin + selectedTileRow*tilesetTileHeight, tilesetTileWidth, tilesetTileHeight), overlayTexture, ScaleMode.ScaleToFit, true);
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
            int tileCol = (int)(uv.x / UVTileWidth);
            int tileRow = (int)(uv.y / UVTileHeight);
            result = (tileCol == 1 && tileRow == 0);
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
        if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && Event.current.button == 0)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Vector2 mousePos = new Vector2(mouseRay.origin.x, mouseRay.origin.y);
            if (editorTilemapCollider.OverlapPoint(mousePos))
            {
                // User has clicked on the mesh
                int col = (int)((mousePos.x - editorTilemapCollider.bounds.min.x) / meshSegmentWidth);
                int row = (int)((mousePos.y - editorTilemapCollider.bounds.min.y) / meshSegmentWidth);
                int tileIndex = col + row * tilemapMesh.meshCols;

                int selectedTileRowConverted = TilesetRows - selectedTileRow - 1; // Convert so that bottom row is zero
                Vector2[] newUV = meshFilter.sharedMesh.uv;
                int vertexI = tileIndex * verticesPerTile;
                newUV[vertexI + 0] = new Vector2(selectedTileCol * UVTileWidth, selectedTileRowConverted * UVTileHeight);
                newUV[vertexI + 1] = new Vector2(selectedTileCol * UVTileWidth, selectedTileRowConverted * UVTileHeight + UVTileHeight);
                newUV[vertexI + 2] = new Vector2(selectedTileCol * UVTileWidth + UVTileWidth, selectedTileRowConverted * UVTileHeight + UVTileHeight);
                newUV[vertexI + 3] = new Vector2(selectedTileCol * UVTileWidth + UVTileWidth, selectedTileRowConverted * UVTileHeight);
                meshFilter.sharedMesh.uv = newUV;

                int controlId = GUIUtility.GetControlID(FocusType.Passive);
                GUIUtility.hotControl = controlId; // Prevent other instruments from gaining focus while painting
                Event.current.Use();
            }
        }
    }

#if false
    [MenuItem("CONTEXT/MeshFilter/Save Mesh...")]
    public static void SaveMeshInPlace(MenuCommand menuCommand)
    {
        MeshFilter mf = menuCommand.context as MeshFilter;
        Mesh m = mf.sharedMesh;
        SaveMesh(m, m.name, false, true);
    }

    [MenuItem("CONTEXT/MeshFilter/Save Mesh As New Instance...")]
    public static void SaveMeshNewInstanceItem(MenuCommand menuCommand)
    {
        MeshFilter mf = menuCommand.context as MeshFilter;
        Mesh m = mf.sharedMesh;
        SaveMesh(m, m.name, true, true);
    }

    public static void SaveMesh(Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh)
    {
        string path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", name, "asset");
        if (string.IsNullOrEmpty(path)) return;

        path = FileUtil.GetProjectRelativePath(path);

        Mesh meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;

        if (optimizeMesh)
            meshToSave.Optimize();

        AssetDatabase.CreateAsset(meshToSave, path);
        AssetDatabase.SaveAssets();
    }
#endif
}
