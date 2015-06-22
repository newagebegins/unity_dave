using UnityEngine;

public class ExitDoor : MonoBehaviour
{
    public Sprite openSprite;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnOpenDoor()
    {
        spriteRenderer.sprite = openSprite;
    }

    private void OnEnterDoor()
    {
    }
}
