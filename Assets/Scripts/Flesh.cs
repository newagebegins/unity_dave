using UnityEngine;

public class Flesh : MonoBehaviour
{
    private Body body;
    private Animator animator;
    private bool isOnGround = false;
    private float lifeTimer = 0;
    private float lifeDuration = 0;

    private void Awake()
    {
        body = GetComponent<Body>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (isOnGround)
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer > lifeDuration)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            body.Move();

            if (body.collisionLeft || body.collisionRight)
            {
                // Bounce off walls.
                body.velocity.x = -body.velocity.x;
            }

            if (body.velocity.x > 0)
            {
                body.DirectionX = 1;
            }
            else if (body.velocity.x < 0)
            {
                body.DirectionX = -1;
            }

            if (body.IsGrounded)
            {
                animator.Play(Animator.StringToHash("Ground"));
                isOnGround = true;
                lifeDuration = Random.Range(0.5f, 2f);
            }
            else
            {
                if (body.velocity.y > 0)
                {
                    animator.Play(Animator.StringToHash("Ascend"));
                }
                else
                {
                    animator.Play(Animator.StringToHash("Descend"));
                }
            }
        }
    }
}
