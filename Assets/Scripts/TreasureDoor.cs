using UnityEngine;
using System.Collections.Generic;

public class TreasureDoor : MonoBehaviour
{
    public Sprite closedSprite;
    private Sprite openSprite;
    private List<GameObject> collectibles = new List<GameObject>();
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        openSprite = spriteRenderer.sprite;
        spriteRenderer.sprite = closedSprite;

        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        Collider2D[] colliders = Physics2D.OverlapAreaAll(boxCollider.bounds.min, boxCollider.bounds.max, 1 << LayerMask.NameToLayer("Collectible"));
        foreach (Collider2D collider in colliders)
        {
            collectibles.Add(collider.gameObject);
            collider.gameObject.SetActive(false);
        }
    }

    public bool IsClosed
    {
        get { return spriteRenderer.sprite == closedSprite; }
    }

    public void Open()
    {
        if (IsClosed)
        {
            spriteRenderer.sprite = openSprite;
            foreach (GameObject collectible in collectibles)
            {
                collectible.SetActive(true);
            }
        }
    }
}
