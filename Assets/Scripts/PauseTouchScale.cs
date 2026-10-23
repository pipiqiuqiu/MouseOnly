using System.Collections.Generic;
using UnityEngine;

public sealed class PauseTouchScale : MonoBehaviour
{
    [SerializeField, Min(1f)] private float enteredScale = 1.18f;
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.18f;
    [SerializeField] private WaveMetaballs wave;

    //change to another sprite
    public Sprite alternateSprite;
    private SpriteRenderer spriteRenderer;
    private Sprite originalSprite;

    //record pause and stop animation
    private bool isPaused;
    
    private readonly HashSet<Collider> touchingPlayerColliders = new HashSet<Collider>();
    private Vector3 restingScale;
    private float currentFactor = 1f;
    private float startFactor = 1f;
    private float targetFactor = 1f;
    private float transitionTime;
    private bool initialized;

    private void Awake()
    {
        //record original Sprite
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalSprite = spriteRenderer.sprite;
        
        restingScale = transform.localScale;
        initialized = true;

        Collider trigger = GetComponent<Collider>();
        if (trigger == null)
            trigger = gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<InchwormLocomotion>() == null)
            return;

        if (touchingPlayerColliders.Add(other) && touchingPlayerColliders.Count == 1)
        {
            isPaused = !isPaused;
            if (spriteRenderer != null && alternateSprite != null)
                spriteRenderer.sprite = isPaused ? alternateSprite : originalSprite;

            BeginScale(enteredScale);
            if (wave != null)
            {
                if (isPaused)
                    wave.SwitchToSliderControl();
                else
                    wave.ResumeAutomaticControl();
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        OnTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (touchingPlayerColliders.Remove(other) && touchingPlayerColliders.Count == 0)
            BeginScale(1f);
    }

    private void BeginScale(float factor)
    {
        startFactor = currentFactor;
        targetFactor = factor;
        transitionTime = 0f;
    }

    private void Update()
    {
        if (!initialized || Mathf.Approximately(currentFactor, targetFactor))
            return;

        transitionTime += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(transitionTime / Mathf.Max(0.01f, transitionDuration));
        t = t * t * (3f - 2f * t);
        currentFactor = Mathf.Lerp(startFactor, targetFactor, t);
        transform.localScale = restingScale * currentFactor;
    }

    private void OnDisable()
    {
        touchingPlayerColliders.Clear();
        if (initialized)
            transform.localScale = restingScale;
        currentFactor = startFactor = targetFactor = 1f;
    }
}
