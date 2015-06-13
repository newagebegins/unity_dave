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
        for (int i = 0; i < 4; ++i)
        {
            GameObject flesh = Instantiate(fleshPrefab, position, Quaternion.identity) as GameObject;
            //float randomAngle = Random.Range(20, 160) * Mathf.Deg2Rad;
            //Vector2 dir = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
            //Vector2 forceVector = dir * Random.Range(300, 600);
            //flesh.GetComponent<Rigidbody2D>().AddForce(forceVector);
            float lifeTime = Random.Range(5f, 12f) / 10f;
            Destroy(flesh, lifeTime);
        }
    }
}
