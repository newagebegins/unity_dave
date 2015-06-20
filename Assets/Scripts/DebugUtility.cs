using UnityEngine;

public class DebugUtility
{
    public static void DrawRect(Vector2 min, Vector2 max, Color color)
    {
        Debug.DrawLine(new Vector2(min.x, min.y), new Vector2(max.x, min.y), color);
        Debug.DrawLine(new Vector2(max.x, min.y), new Vector2(max.x, max.y), color);
        Debug.DrawLine(new Vector2(max.x, max.y), new Vector2(min.x, max.y), color);
        Debug.DrawLine(new Vector2(min.x, max.y), new Vector2(min.x, min.y), color);
    }
}
