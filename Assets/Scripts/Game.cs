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
    public void CreateFleshChunks(Vector3 position)
    {
        int chunkCount = Random.Range(3, 6);
        for (int i = 0; i < chunkCount; ++i)
        {
            GameObject fleshObj = Instantiate(fleshPrefab, position, Quaternion.identity) as GameObject;
            Body body = fleshObj.GetComponent<Body>();
            float randomAngle = Random.Range(40f, 140f) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
            Vector2 velocity = dir * Random.Range(5f, 15f);
            body.velocity = velocity;
            float lifeTime = Random.Range(1.0f, 2.4f);
            Destroy(fleshObj, lifeTime);
        }
    }
}
