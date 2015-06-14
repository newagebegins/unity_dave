using UnityEngine;

public class Zombie : MonoBehaviour
{
    private Body body;
    public float speedX = 8;

    private void Awake()
    {
        body = GetComponent<Body>();
    }

    private void Update()
    {
        body.velocity.x = body.directionX * speedX;
        body.FlipIfNecessary();
        body.Move();

        if (body.collisionLeft || body.collisionRight)
        {
            body.directionX = -body.directionX;
        }
    }
}
