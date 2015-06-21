﻿using UnityEngine;

public class Zombie : MonoBehaviour
{
    private Body body;
    public float speedX = 8;
    private BoxCollider2D boxCollider;
    public float playerScanWidth = 6;
    
    private GameObject player;
    private BoxCollider2D playerCollider;

    // Enemy is inactive until the player comes close enough.
    private bool isActive = false;
    public float activeScanWidth = 11;
    public float activeScanHeight = 6;

    private bool isWalkingDownStairs = false;
    private float stairsBottomY = 0;
    private float stairsStepTimer = 0;
    public float stairsStepDuration = 0.5f;
    private const float stairsRayDistance = 0.5f;
    private const float stairsYEpsilon = 0.1f;

    private float dontSearchForPlayerTimer = 0;
    private float dontSearchForPlayerDuration = 0;
    public float dontSearchForPlayerDurationMin = 0.5f;
    public float dontSearchForPlayerDurationMax = 1.5f;
    private bool ShouldSearchForPlayer
    {
        get { return dontSearchForPlayerTimer >= dontSearchForPlayerDuration; }
    }

    private void Awake()
    {
        body = GetComponent<Body>();
        boxCollider = GetComponent<BoxCollider2D>();
        player = GameObject.Find("Player");
        playerCollider = player.GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        if (isActive)
        {
            if (isWalkingDownStairs)
            {
                stairsStepTimer += Time.deltaTime;
                if (stairsStepTimer > stairsStepDuration)
                {
                    stairsStepTimer = 0;
                    
                    // Step down.
                    transform.position = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

                    float colliderBottomY = boxCollider.bounds.min.y - stairsYEpsilon;
                    bool reachedPlayerYLevel = colliderBottomY <= playerCollider.bounds.min.y;
                    bool reachedStairsBottom = colliderBottomY <= stairsBottomY;
                    
                    if (reachedStairsBottom || reachedPlayerYLevel)
                    {
                        isWalkingDownStairs = false;
                    }
                }
            }
            else
            {
                body.velocity.x = body.DirectionX * speedX;
                body.Move();

                if (body.collisionLeft || body.collisionRight)
                {
                    // Horizontal collision.

                    body.DirectionX = -body.DirectionX;

                    // Don't search for player for some random time.
                    dontSearchForPlayerDuration = Random.Range(dontSearchForPlayerDurationMin, dontSearchForPlayerDurationMax);
                    dontSearchForPlayerTimer = 0;
                }

                dontSearchForPlayerTimer += Time.deltaTime;

                if (ShouldSearchForPlayer)
                {
                    Vector2 playerScanMin = new Vector2(boxCollider.bounds.min.x - playerScanWidth, boxCollider.bounds.min.y);
                    Vector2 playerScanMax = new Vector2(boxCollider.bounds.max.x + playerScanWidth, boxCollider.bounds.max.y);
                    DebugUtility.DrawRect(playerScanMin, playerScanMax, Color.yellow);
                    Collider2D playerCollider = Physics2D.OverlapArea(playerScanMin, playerScanMax, 1 << LayerMask.NameToLayer("Player"));
                    if (playerCollider)
                    {
                        // Found the player.

                        // Move towards the player.
                        body.DirectionX = playerCollider.transform.position.x < transform.position.x ? -1 : 1;
                    }
                }

                // Check if we are near the stairs.
                {
                    Vector2 stairsRayOrigin = new Vector2(transform.position.x, boxCollider.bounds.min.y);
                    Vector2 stairsRayDirection = Vector2.down;
                    Debug.DrawRay(stairsRayOrigin, stairsRayDirection * stairsRayDistance, Color.cyan);
                    RaycastHit2D stairsHit = Physics2D.Raycast(stairsRayOrigin, stairsRayDirection, stairsRayDistance, 1 << LayerMask.NameToLayer("Stairs"));

                    if (stairsHit)
                    {
                        // Stairs is below us.

                        float originY = boxCollider.bounds.min.y;
                        Vector2 leftRayOrigin = new Vector2(boxCollider.bounds.min.x, originY);
                        Vector2 rightRayOrigin = new Vector2(boxCollider.bounds.max.x, originY);
                        Debug.DrawRay(leftRayOrigin, stairsRayDirection * stairsRayDistance, Color.cyan);
                        Debug.DrawRay(rightRayOrigin, stairsRayDirection * stairsRayDistance, Color.cyan);
                        RaycastHit2D leftHit = Physics2D.Raycast(leftRayOrigin, stairsRayDirection, stairsRayDistance, 1 << LayerMask.NameToLayer("OneWayPlatform"));
                        RaycastHit2D rightHit = Physics2D.Raycast(rightRayOrigin, stairsRayDirection, stairsRayDistance, 1 << LayerMask.NameToLayer("OneWayPlatform"));
                        bool isOnOneWayPlatform = leftHit && rightHit;

                        Bounds stairsBounds = stairsHit.collider.bounds;
                        stairsBounds.Expand(2f);
                        DebugUtility.DrawRect(stairsBounds.min, stairsBounds.max, Color.magenta);
                        bool playerIsNearTheStairs = Physics2D.OverlapArea(stairsBounds.min, stairsBounds.max, 1 << LayerMask.NameToLayer("Player"));
                        bool playerIsBelow = (boxCollider.bounds.min.y - stairsYEpsilon) > playerCollider.bounds.min.y;

                        if (isOnOneWayPlatform && playerIsNearTheStairs && playerIsBelow)
                        {
                            isWalkingDownStairs = true;
                            stairsBottomY = stairsHit.collider.bounds.min.y;
                        }
                    }
                }
            }
        }
        else
        {
            // Check if the player is close.
            
            Vector2 playerScanMin = new Vector2(transform.position.x - activeScanWidth, transform.position.y - activeScanHeight);
            Vector2 playerScanMax = new Vector2(transform.position.x + activeScanWidth, transform.position.y + activeScanHeight);
            DebugUtility.DrawRect(playerScanMin, playerScanMax, Color.green);
            Collider2D playerCollider = Physics2D.OverlapArea(playerScanMin, playerScanMax, 1 << LayerMask.NameToLayer("Player"));
            if (playerCollider)
            {
                isActive = true;
            }
        }
    }
}
