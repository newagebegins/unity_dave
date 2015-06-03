using UnityEngine;
using System.Collections;
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
    private BoxCollider2D collider;

    private void OnEnable()
    {
        tilemapMesh = (TilemapMesh)target;
        MeshRenderer meshRenderer = tilemapMesh.GetComponent<MeshRenderer>();
        texture = (Texture2D)meshRenderer.sharedMaterial.mainTexture;
        meshFilter = tilemapMesh.GetComponent<MeshFilter>();
        collider = tilemapMesh.GetComponent<BoxCollider2D>();
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

            collider.size = new Vector3(guiMeshCols * meshSegmentWidth, guiMeshRows * meshSegmentHeight, 0);
            tilemapMesh.gameObject.transform.position = centerPos;
            
            tilemapMesh.meshCols = guiMeshCols;
            tilemapMesh.meshRows = guiMeshRows;
        }

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
        switch (Event.current.type)
        {
            case EventType.MouseDown:
            case EventType.MouseDrag:
                int tilesCountX = (int)(texture.width / textureTileWidth);
                int tilesCountY = (int)(texture.height / textureTileHeight);
                Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                Vector2 mousePos = new Vector2(mouseRay.origin.x, mouseRay.origin.y);
                if (Event.current.button == 0 && collider.OverlapPoint(mousePos))
                {
                    // User has clicked on the mesh
                    int col = (int)((mousePos.x - collider.bounds.min.x) / meshSegmentWidth);
                    int row = (int)((mousePos.y - collider.bounds.min.y) / meshSegmentWidth);
                    int tileIndex = col + row * tilemapMesh.meshCols;

                    float uvTileWidth = 1.0f / tilesCountX;
                    float uvTileHeight = 1.0f / tilesCountY;

                    int selectedTileRowConverted = tilesCountY - selectedTileRow - 1; // Convert so that bottom row is zero
                    Vector2[] newUV = meshFilter.sharedMesh.uv;
                    int vertexI = tileIndex * verticesPerTile;
                    newUV[vertexI + 0] = new Vector2(selectedTileCol * uvTileWidth, selectedTileRowConverted * uvTileHeight);
                    newUV[vertexI + 1] = new Vector2(selectedTileCol * uvTileWidth, selectedTileRowConverted * uvTileHeight + uvTileHeight);
                    newUV[vertexI + 2] = new Vector2(selectedTileCol * uvTileWidth + uvTileWidth, selectedTileRowConverted * uvTileHeight + uvTileHeight);
                    newUV[vertexI + 3] = new Vector2(selectedTileCol * uvTileWidth + uvTileWidth, selectedTileRowConverted * uvTileHeight);
                    meshFilter.sharedMesh.uv = newUV;

                    int controlId = GUIUtility.GetControlID(FocusType.Passive);
                    GUIUtility.hotControl = controlId; // Prevent other instruments from gaining focus while painting
                    Event.current.Use();
                }
                break;
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
