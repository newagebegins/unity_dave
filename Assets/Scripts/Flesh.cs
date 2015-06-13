using UnityEngine;

public class Flesh : MonoBehaviour
{
    [HideInInspector]
    public Vector2 velocity = new Vector2(0, 0);
    public float groundFrictionX = 30;
    public float airFrictionX = 10;
    public int horizontalRaysCount = 8;
    public int verticalRaysCount = 4;
    private BoxCollider2D boxCollider;
    public float skinWidth = 0.02f;
    public float gravity = -25f;
    private bool isGrounded = false;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        // Horizontal friction.
        float frictionX = isGrounded ? groundFrictionX : airFrictionX;
        float velocityXAfterFriction = velocity.x - Mathf.Sign(velocity.x) * frictionX * Time.deltaTime;
        velocity.x = Mathf.Sign(velocityXAfterFriction) != Mathf.Sign(velocity.x) ? 0 : velocityXAfterFriction;

        // Gravity.
        velocity.y += gravity * Time.deltaTime;

        Vector2 deltaMovement = velocity * Time.deltaTime;

        isGrounded = false;

        Vector2 raycastOriginsBottomRight = new Vector2(boxCollider.bounds.max.x - skinWidth, boxCollider.bounds.min.y + skinWidth);
        Vector2 raycastOriginsBottomLeft = new Vector2(boxCollider.bounds.min.x + skinWidth, boxCollider.bounds.min.y + skinWidth);
        Vector2 raycastOriginsTopLeft = new Vector2(boxCollider.bounds.min.x + skinWidth, boxCollider.bounds.max.y - skinWidth);

        float colliderUsableWidth = boxCollider.size.x * Mathf.Abs(transform.localScale.x) - (2f * skinWidth);
        float colliderUsableHeight = boxCollider.size.y * Mathf.Abs(transform.localScale.y) - (2f * skinWidth);
        float horizontalDistanceBetweenRays = colliderUsableWidth / ((float)verticalRaysCount - 1f);
        float verticalDistanceBetweenRays = colliderUsableHeight / ((float)horizontalRaysCount - 1f);
        const float skinWidthFloatFudgeFactor = 0.001f; // Helps avoiding float precision bugs.

        if (deltaMovement.x != 0)
        {
            // Move horizontally.

            bool isMovingRight = deltaMovement.x > 0;
            Vector2 baseRayOrigin = isMovingRight ? raycastOriginsBottomRight : raycastOriginsBottomLeft;
            float rayDistance = Mathf.Abs(deltaMovement.x) + skinWidth;
            Vector2 rayDirection = isMovingRight ? Vector2.right : -Vector2.right;

            for (int i = 0; i < horizontalRaysCount; ++i)
            {
                Vector2 rayOrigin = new Vector2(baseRayOrigin.x, baseRayOrigin.y + i * verticalDistanceBetweenRays);
                Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.red);
                RaycastHit2D raycastHit = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, 1 << LayerMask.NameToLayer("Default"));
                if (raycastHit)
                {
                    deltaMovement.x = raycastHit.point.x - rayOrigin.x;
                    rayDistance = Mathf.Abs(deltaMovement.x);
                    if (isMovingRight)
                    {
                        deltaMovement.x -= skinWidth;
                    }
                    else
                    {
                        deltaMovement.x += skinWidth;
                    }

                    if (rayDistance < skinWidth + skinWidthFloatFudgeFactor)
                        break;
                }
            }
        }

        if (deltaMovement.y != 0)
        {
            // Move vertically.

            bool isMovingUp = deltaMovement.y > 0;
            float rayDistance = Mathf.Abs(deltaMovement.y) + skinWidth;
            Vector2 rayDirection = isMovingUp ? Vector2.up : -Vector2.up;
            Vector2 baseRayOrigin = isMovingUp ? raycastOriginsTopLeft : raycastOriginsBottomLeft;
            baseRayOrigin.x += deltaMovement.x;

            int mask = 1 << LayerMask.NameToLayer("Default");
            if (!isMovingUp)
            {
                mask |= 1 << LayerMask.NameToLayer("OneWayPlatform");
            }

            for (int i = 0; i < verticalRaysCount; ++i)
            {
                Vector2 rayOrigin = new Vector2(baseRayOrigin.x + i * horizontalDistanceBetweenRays, baseRayOrigin.y);
                Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.red);
                RaycastHit2D raycastHit = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, mask);
                if (raycastHit)
                {
                    deltaMovement.y = raycastHit.point.y - rayOrigin.y;
                    rayDistance = Mathf.Abs(deltaMovement.y);
                    if (isMovingUp)
                    {
                        deltaMovement.y -= skinWidth;
                    }
                    else
                    {
                        deltaMovement.y += skinWidth;
                        isGrounded = true;
                    }

                    if (rayDistance < skinWidth + skinWidthFloatFudgeFactor)
                        break;
                }
            }
        }

        transform.Translate(deltaMovement);

        if (Time.deltaTime > 0)
        {
            velocity = deltaMovement / Time.deltaTime;
        }
    }
}
