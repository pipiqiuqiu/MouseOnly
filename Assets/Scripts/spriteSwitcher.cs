using UnityEngine;

public class spriteSwitcher : MonoBehaviour
{
    public SpriteRenderer targetRenderer;
    
    public Sprite[] sprites;

    private int currentIndex = 0;

    void Start()
    {
        if (targetRenderer != null && sprites.Length > 0)
        {
            targetRenderer.sprite = sprites[0];
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SwitchSprite();
        }
    }

    void SwitchSprite()
    {
        if (targetRenderer == null || sprites.Length == 0)
            return;

        currentIndex = (currentIndex + 1) % sprites.Length;

        targetRenderer.sprite = sprites[currentIndex];
    }
}
