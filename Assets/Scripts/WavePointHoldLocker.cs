using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A locker that uses an existing wave source SpriteRenderer. The trigger keeps
/// its original world size while the source point grows and shrinks.
/// </summary>
public sealed class WavePointHoldLocker : MonoBehaviour
{
    [SerializeField] private GameObject featureParent;
    [SerializeField, Min(0.01f)] private float growDuration = 1f;
    [SerializeField, Min(0.01f)] private float shrinkDuration = 0.5f;
    [SerializeField, Min(1f)] private float enlargedScale = 1.3f;

    private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();
    private BoxCollider trigger;
    private Vector3 originalScale;
    private Vector3 originalTriggerSize;
    private Vector3 originalTriggerCenter;
    private float elapsed;
    private bool completed;

    private void Awake()
    {
        trigger = GetComponent<BoxCollider>();
        originalScale = transform.localScale;
        if (trigger != null)
        {
            originalTriggerSize = trigger.size;
            originalTriggerCenter = trigger.center;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (completed || other.GetComponentInParent<InchwormLocomotion>() == null)
            return;

        playerColliders.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!playerColliders.Remove(other) || playerColliders.Count != 0 || completed)
            return;

        elapsed = 0f;
        ApplyScale(1f);
    }

    private void Update()
    {
        if (completed || playerColliders.Count == 0)
            return;

        elapsed += Time.deltaTime;
        if (elapsed < growDuration)
        {
            ApplyScale(Mathf.Lerp(1f, enlargedScale, elapsed / growDuration));
            return;
        }

        float shrinkProgress = Mathf.Clamp01((elapsed - growDuration) / shrinkDuration);
        ApplyScale(Mathf.Lerp(enlargedScale, 0.01f, shrinkProgress));
        if (shrinkProgress < 1f)
            return;

        if (featureParent == null)
        {
            Debug.LogError("WavePointHoldLocker needs a hidden feature parent to reveal.", this);
            elapsed = 0f;
            ApplyScale(1f);
            playerColliders.Clear();
            return;
        }

        completed = true;
        featureParent.SetActive(true);
        Destroy(gameObject);
    }

    private void ApplyScale(float multiplier)
    {
        transform.localScale = originalScale * multiplier;
        if (trigger == null)
            return;

        // The point's sprite animates, while the player's required overlap area
        // stays the same size throughout both animation phases.
        trigger.size = originalTriggerSize / multiplier;
        trigger.center = originalTriggerCenter / multiplier;
    }
}
