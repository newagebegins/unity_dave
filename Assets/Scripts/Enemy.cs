using UnityEngine;

public class Enemy : MonoBehaviour
{
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
        if (isFlashing)
        {
            flashTimer += Time.deltaTime;
            if (flashTimer > flashDuration)
            {
                StopFlashing();
            }
        }
    }

    public void Hit()
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
