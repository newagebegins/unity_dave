using UnityEngine;

public class Knife : MonoBehaviour
{
    private Body body;
    private Game game;
    private BoxCollider2D boxCollider;
    public float speedX = 8;

    private void Awake()
    {
        body = GetComponent<Body>();
        game = Camera.main.GetComponent<Game>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        body.velocity.x = body.DirectionX * speedX;
        body.Move();

        if (body.collisionLeft || body.collisionRight)
        {
            DestroySelf();
        }
        else
        {
            Collider2D playerCollider = Physics2D.OverlapArea(boxCollider.bounds.min, boxCollider.bounds.max, 1 << LayerMask.NameToLayer("Player"));
            if (playerCollider)
            {
                // Knife has hit the player.
                DestroySelf();
                playerCollider.GetComponent<Player>().Kill();
            }
        }
    }

    private void DestroySelf()
    {
        game.CreateProjectileExplosion(transform.position);
        Destroy(gameObject);
    }
}
