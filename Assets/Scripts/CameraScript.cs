using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public Transform player;
    public float xSpan = 2f;
    public float ySpan = 2f;
    public bool drawBounds = true;
    public float xMin = 0;
    public float yMin = 0;
    public float xMax = 100f;
    public float yMax = 100f;

    private void Update()
    {
        float right = transform.position.x + xSpan;
        float left = transform.position.x - xSpan;
        float bottom = transform.position.y - ySpan;
        float top = transform.position.y + ySpan;

        if (drawBounds)
        {
            Debug.DrawLine(new Vector3(right, bottom, 1), new Vector3(right, top, 1), Color.red);
            Debug.DrawLine(new Vector3(left, bottom, 1), new Vector3(left, top, 1), Color.red);
            Debug.DrawLine(new Vector3(left, bottom, 1), new Vector3(right, bottom, 1), Color.red);
            Debug.DrawLine(new Vector3(left, top, 1), new Vector3(right, top, 1), Color.red);
        }

        float newX = transform.position.x;
        float newY = transform.position.y;

        if (player.position.x < left)
        {
            newX = transform.position.x - (left - player.position.x);
        }
        else if (player.position.x > right)
        {
            newX = transform.position.x + (player.position.x - right);
        }

        if (player.position.y < bottom)
        {
            newY = transform.position.y - (bottom - player.position.y);
        }
        else if (player.position.y > top)
        {
            newY = transform.position.y + (player.position.y - top);
        }

        newX = Mathf.Clamp(newX, xMin, xMax);
        newY = Mathf.Clamp(newY, yMin, yMax);

        transform.position = new Vector3(newX, newY, transform.position.z);
    }
}
