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
}
