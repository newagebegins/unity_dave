using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(TilemapMesh))]
public class LevelScriptEditor : Editor
{
    private int selectedTileCol = 0;
    private int selectedTileRow = 0;
    private static int cols = 2;
    private static int rows = 2;
    private static float segW = 1f;
    private static float segH = 1f;
    private static float textureTileWidth = 8;
    private static float textureTileHeight = 8;

    [MenuItem("My Tools/Create Mesh")]
    private static void CreateMesh()
    {
        float z = 0;

        Mesh mesh = new Mesh();
        mesh.name = "TilemapMesh";

        for (int row = 0; row < rows; ++row)
        {
            for (int col = 0; col < cols; ++col)
            {

            }
        }

        mesh.vertices = new Vector3[]
        {      
            new Vector3(-segW, -segH, z),
            new Vector3(-segW, 0, z),
            new Vector3(0, 0, z),
            new Vector3(0, -segH, z),

            new Vector3(0, -segH, z),
            new Vector3(0, 0, z),
            new Vector3(segW, 0, z),
            new Vector3(segW, -segH, z),

            new Vector3(-segW, 0, z),
            new Vector3(-segW, segH, z),
            new Vector3(0, segH, z),
            new Vector3(0, 0, z),

            new Vector3(0, 0, z),
            new Vector3(0, segH, z),
            new Vector3(segW, segH, z),
            new Vector3(segW, 0, z),
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0.0f, 0.0f),
            new Vector2(0.0f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.0f),

            new Vector2(0.5f, 0.0f),
            new Vector2(0.5f, 0.5f),
            new Vector2(1.0f, 0.5f),
            new Vector2(1.0f, 0.0f),

            new Vector2(0.0f, 0.5f),
            new Vector2(0.0f, 1.0f),
            new Vector2(0.5f, 1.0f),
            new Vector2(0.5f, 0.5f),

            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 1.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(1.0f, 0.5f),
        };
        mesh.triangles = new int[]
        {
            0, 1, 2,
            0, 2, 3,

            4, 5, 6,
            4, 6, 7,
            
            8, 9, 10,
            8, 10, 11,

            12, 13, 14,
            12, 14, 15,
        };
        mesh.RecalculateNormals();

        GameObject go = new GameObject("TilemapMesh");
        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        Material material = AssetDatabase.LoadAssetAtPath("Assets/Materials/test.mat", typeof(Material)) as Material;
        renderer.material = material;
        go.AddComponent<TilemapMesh>();
        go.AddComponent<BoxCollider2D>();
    }

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

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Resize"))
        {
            // TODO: Resize
        }

        TilemapMesh tilemapMesh = (TilemapMesh)target;
        MeshRenderer meshRenderer = tilemapMesh.GetComponent<MeshRenderer>();
        Texture2D texture = (Texture2D)meshRenderer.sharedMaterial.mainTexture;

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

    void OnSceneGUI()
    {
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        switch (Event.current.type)
        {
            case EventType.MouseDown:
            case EventType.MouseDrag:
                TilemapMesh tilemapMesh = (TilemapMesh)target;
                BoxCollider2D collider = tilemapMesh.gameObject.GetComponent<BoxCollider2D>();
                MeshFilter meshFilter = tilemapMesh.gameObject.GetComponent<MeshFilter>();
                Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                Vector2 mousePos = new Vector2(mouseRay.origin.x, mouseRay.origin.y);
                if (Event.current.button == 0 && collider.OverlapPoint(mousePos))
                {
                    // User has clicked on the mesh
                    int col = (int)((mousePos.x - collider.bounds.min.x) / segW);
                    int row = (int)((mousePos.y - collider.bounds.min.y) / segW);
                    int tileIndex = col + row * cols;

                    float uvTileWidth = 1.0f / cols;
                    float uvTileHeight = 1.0f / rows;

                    int selectedTileRowConverted = rows - selectedTileRow - 1; // Convert so that bottom row is zero
                    Vector2[] newUV = meshFilter.sharedMesh.uv;
                    newUV[tileIndex * 4 + 0] = new Vector2(selectedTileCol * uvTileWidth, selectedTileRowConverted * uvTileHeight);
                    newUV[tileIndex * 4 + 1] = new Vector2(selectedTileCol * uvTileWidth, selectedTileRowConverted * uvTileHeight + uvTileHeight);
                    newUV[tileIndex * 4 + 2] = new Vector2(selectedTileCol * uvTileWidth + uvTileWidth, selectedTileRowConverted * uvTileHeight + uvTileHeight);
                    newUV[tileIndex * 4 + 3] = new Vector2(selectedTileCol * uvTileWidth + uvTileWidth, selectedTileRowConverted * uvTileHeight);
                    meshFilter.sharedMesh.uv = newUV;
                }

                GUIUtility.hotControl = controlId; // Prevent selection from working in the scene view
                Event.current.Use();
                break;
        }
    }
}
