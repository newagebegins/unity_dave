using UnityEngine;

public class Game : MonoBehaviour
{
    public GameObject projectileExplosionPrefab;
    public void CreateProjectileExplosion(Vector3 position)
    {
        GameObject explosion = Instantiate(projectileExplosionPrefab, position, Quaternion.identity) as GameObject;
        Destroy(explosion, 0.25f);
    }

    private int score = 0;
    public GameObject pointsPrefab;
    public Sprite pointsSprite100;
    public Sprite pointsSprite400;
    public void CreatePoints(Vector3 position, int scoreValue)
    {
        score += scoreValue;
        GameObject points = Instantiate(pointsPrefab, position, Quaternion.identity) as GameObject;
        SpriteRenderer spriteRenderer = points.transform.Find("Animation").GetComponent<SpriteRenderer>();
        switch (scoreValue)
        {
            case 100:
                spriteRenderer.sprite = pointsSprite100;
                break;
            case 400:
                spriteRenderer.sprite = pointsSprite400;
                break;
            default:
                Debug.LogError("Unsupported score value " + scoreValue);
                break;
        }
    }

    public GameObject fleshPrefab;
    public void CreateFleshChunks(Vector3 position, int count)
    {
        switch (count)
        {
            case 4:
            {
                CreateFleshChunk(position, 45);
                CreateFleshChunk(position, 135);
                CreateFleshChunk(position, 210);
                CreateFleshChunk(position, 330);
                break;
            }
            case 2:
            {
                CreateFleshChunk(position, 45);
                CreateFleshChunk(position, 135);
                break;
            }
            default:
            {
                Debug.LogError("Unsupported flesh chunk count: " + count);
                break;
            }
        }
    }

    private void CreateFleshChunk(Vector3 position, float angleDeg)
    {
        GameObject fleshObj = Instantiate(fleshPrefab, position, Quaternion.identity) as GameObject;
        Body body = fleshObj.GetComponent<Body>();
        float angleRad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
        float speed;
        if (angleDeg >= 0 && angleDeg <= 180)
        {
            speed = Random.Range(5f, 15f);
        }
        else
        {
            speed = Random.Range(2f, 8f);
        }
        Vector2 velocity = dir * speed;
        body.velocity = velocity;
    }
}
