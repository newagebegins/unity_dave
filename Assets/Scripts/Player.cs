using UnityEngine;

public class Player : MonoBehaviour
{
    private Vector2 velocity = new Vector2(0, 0);
    public float runAcceleration = 50;
    public float maxVelocityX = 8;
    public float groundFriction = 30;

    private void Update()
    {
        // Ground friction
        float velocityXAfterFriction = velocity.x - Mathf.Sign(velocity.x) * groundFriction * Time.deltaTime;
        velocity.x = Mathf.Sign(velocityXAfterFriction) != Mathf.Sign(velocity.x) ? 0 : velocityXAfterFriction;

        // Horizontal acceleration
        float hAxis = Input.GetAxisRaw("Horizontal");
        velocity.x += hAxis * runAcceleration * Time.deltaTime;
        velocity.x = Mathf.Clamp(velocity.x, -maxVelocityX, maxVelocityX);

        // Flip the sprite if it faces the wrong direction
        if ((hAxis > 0 && transform.localScale.x < 0) || (hAxis < 0 && transform.localScale.x > 0))
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        
        Vector2 deltaMovement = velocity * Time.deltaTime;
        transform.Translate(deltaMovement);
    }
}
