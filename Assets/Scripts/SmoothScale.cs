using UnityEngine;

public class SmoothScale : MonoBehaviour
{
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    public float speed = 2f;

    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;

        float scale = Mathf.Lerp(minScale, maxScale, t);

        transform.localScale = originalScale * scale;
    }
}