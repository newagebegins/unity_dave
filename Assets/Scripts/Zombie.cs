using UnityEngine;

public class Zombie : MonoBehaviour
{
    private Body body;
    public float speedX = 8;
    private BoxCollider2D boxCollider;
    public float playerScanWidth = 6;

    // Enemy is inactive until the player comes close enough.
    private bool isActive = false;
    public float activeScanWidth = 11;
    public float activeScanHeight = 6;

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
    }

    private void Update()
    {
        if (isActive)
        {
            body.velocity.x = body.directionX * speedX;
            body.FlipIfNecessary();
            body.Move();

            if (body.collisionLeft || body.collisionRight)
            {
                // Horizontal collision.

                body.directionX = -body.directionX;

                // Don't search for player for some random time.
                dontSearchForPlayerDuration = Random.Range(dontSearchForPlayerDurationMin, dontSearchForPlayerDurationMax);
                dontSearchForPlayerTimer = 0;
            }

            dontSearchForPlayerTimer += Time.deltaTime;

            if (ShouldSearchForPlayer)
            {
                Vector2 playerScanMin = new Vector2(boxCollider.bounds.min.x - playerScanWidth, boxCollider.bounds.min.y);
                Vector2 playerScanMax = new Vector2(boxCollider.bounds.max.x + playerScanWidth, boxCollider.bounds.max.y);
                Color debugLineColor = Color.yellow;
                Debug.DrawLine(new Vector2(playerScanMin.x, playerScanMin.y), new Vector2(playerScanMax.x, playerScanMin.y), debugLineColor);
                Debug.DrawLine(new Vector2(playerScanMax.x, playerScanMin.y), new Vector2(playerScanMax.x, playerScanMax.y), debugLineColor);
                Debug.DrawLine(new Vector2(playerScanMax.x, playerScanMax.y), new Vector2(playerScanMin.x, playerScanMax.y), debugLineColor);
                Debug.DrawLine(new Vector2(playerScanMin.x, playerScanMax.y), new Vector2(playerScanMin.x, playerScanMin.y), debugLineColor);
                Collider2D playerCollider = Physics2D.OverlapArea(playerScanMin, playerScanMax, 1 << LayerMask.NameToLayer("Player"));
                if (playerCollider)
                {
                    // Found the player.

                    // Move towards the player.
                    body.directionX = playerCollider.transform.position.x < transform.position.x ? -1 : 1;
                }
            }
        }
        else
        {
            // Check if the player is close.
            
            Vector2 playerScanMin = new Vector2(transform.position.x - activeScanWidth, transform.position.y - activeScanHeight);
            Vector2 playerScanMax = new Vector2(transform.position.x + activeScanWidth, transform.position.y + activeScanHeight);
            Color debugLineColor = Color.green;
            Debug.DrawLine(new Vector2(playerScanMin.x, playerScanMin.y), new Vector2(playerScanMax.x, playerScanMin.y), debugLineColor);
            Debug.DrawLine(new Vector2(playerScanMax.x, playerScanMin.y), new Vector2(playerScanMax.x, playerScanMax.y), debugLineColor);
            Debug.DrawLine(new Vector2(playerScanMax.x, playerScanMax.y), new Vector2(playerScanMin.x, playerScanMax.y), debugLineColor);
            Debug.DrawLine(new Vector2(playerScanMin.x, playerScanMax.y), new Vector2(playerScanMin.x, playerScanMin.y), debugLineColor);
            Collider2D playerCollider = Physics2D.OverlapArea(playerScanMin, playerScanMax, 1 << LayerMask.NameToLayer("Player"));
            if (playerCollider)
            {
                isActive = true;
            }
        }
    }
}
