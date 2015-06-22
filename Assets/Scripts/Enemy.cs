using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Enemy is inactive until the player comes close enough.
    public bool isActive = false;
    public float activeScanWidth = 11;
    public float activeScanHeight = 6;

    public int health = 2;
    public int scoreValue = 100;
    private Game game;

    private const float flashDuration = 0.1f;
    private float flashTimer = 0.0f;
    private bool isFlashing = false;

    private SpriteRenderer spriteRenderer;
    private Shader guiTextShader;
    private Shader defaultShader;

    private void Start()
    {
        game = Camera.main.GetComponent<Game>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        guiTextShader = Shader.Find("GUI/Text Shader");
        defaultShader = Shader.Find("Sprites/Default");
    }

    private void Update()
    {
        if (isActive)
        {
            if (isFlashing)
            {
                flashTimer += Time.deltaTime;
                if (flashTimer > flashDuration)
                {
                    StopFlashing();
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

    public void Hit()
    {
        if (isActive)
        {
            health--;
            if (health <= 0)
            {
                game.CreateFleshChunks(transform.position);
                game.CreatePoints(transform.position, scoreValue);
                Destroy(gameObject);
            }
            else
            {
                StartFlashing();
            }
        }
    }

    private void StartFlashing()
    {
        isFlashing = true;
        flashTimer = 0.0f;
        spriteRenderer.material.shader = guiTextShader;
        spriteRenderer.sortingOrder = 1;
    }

    private void StopFlashing()
    {
        isFlashing = false;
        spriteRenderer.material.shader = defaultShader;
        spriteRenderer.sortingOrder = 0;
    }
}
