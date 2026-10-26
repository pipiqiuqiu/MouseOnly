using UnityEngine;

public class SpriteToggle : MonoBehaviour
{
    public SpriteRenderer targetRenderer;

    public Sprite sprite1;
    public Sprite sprite2;

    private bool isSprite1 = true;

    public Animator piggy;
    private bool isPlaying = true;
    void Start()
    {
        piggy.speed = 1f;
        targetRenderer.sprite = sprite2;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ToggleAnimation();
        }
    }

    public void ToggleAnimation()
    {
        isPlaying = !isPlaying;
        if (isPlaying)
        {
            piggy.speed = 1f;
            targetRenderer.sprite = sprite2;
        }
        else
        {
            piggy.speed = 0f;
            targetRenderer.sprite = sprite1;
        }
    }

}