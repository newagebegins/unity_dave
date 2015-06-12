using UnityEngine;

public class Game : MonoBehaviour
{
    public GameObject projectileExplosionPrefab;

    public void CreateProjectileExplosion(Vector3 position)
    {
        GameObject explosion = Instantiate(projectileExplosionPrefab, position, Quaternion.identity) as GameObject;
        Destroy(explosion, 0.25f);
    }
}
