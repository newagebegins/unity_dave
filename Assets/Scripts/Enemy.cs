using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Enemy is inactive until the camera sees him for the first time.
    public bool isActive = false;

    public int health = 2;
    public int scoreValue = 100;
    private Game game;

    private const float flashDuration = 0.1f;
    private float flashTimer = 0.0f;
    private bool isFlashing = false;

    private SpriteRenderer spriteRenderer;
    private Shader guiTextShader;
    private Shader defaultShader;
    private BoxCollider2D boxCollider;

    private void Start()
    {
        game = Camera.main.GetComponent<Game>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        guiTextShader = Shader.Find("GUI/Text Shader");
        defaultShader = Shader.Find("Sprites/Default");
        boxCollider = GetComponent<BoxCollider2D>();
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
            // Check if camera can see us.
            Bounds bounds = boxCollider.bounds;
            Vector2 leftBottom = Camera.main.WorldToViewportPoint(new Vector2(bounds.min.x, bounds.min.y));
            Vector2 rightBottom = Camera.main.WorldToViewportPoint(new Vector2(bounds.max.x, bounds.min.y));
            Vector2 rightTop = Camera.main.WorldToViewportPoint(new Vector2(bounds.max.x, bounds.max.y));
            Vector2 leftTop = Camera.main.WorldToViewportPoint(new Vector2(bounds.min.x, bounds.max.y));

            if (IsPointInViewport(leftBottom) ||
                IsPointInViewport(rightBottom) ||
                IsPointInViewport(rightTop) ||
                IsPointInViewport(leftTop))
            {
                isActive = true;
            }
        }
    }

    private bool IsPointInViewport(Vector2 point)
    {
        bool result = point.x >= 0 && point.x <= 1 &&
                      point.y >= 0 && point.y <= 1;
        return result;
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
