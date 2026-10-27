using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AfterimageSpawner : MonoBehaviour
{
    public GameObject prefab;

    public float spawnInterval = 0.5f;
    public float waitTime = 1f;
    public float fadeDuration = 1f;
    [Range(1, 4)] public int maxSimultaneous = 4;
    [Min(0f)] public float animationSpeed = 1f;

    private readonly List<GameObject> activeAfterimages = new List<GameObject>();
    private SpriteRenderer sourceRenderer;
    private Animator sourceAnimator;

    void Awake()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();
        sourceAnimator = GetComponent<Animator>();
        if (sourceRenderer != null)
            sourceRenderer.enabled = false;
        if (sourceAnimator != null)
            sourceAnimator.speed = Mathf.Max(0f, animationSpeed);
    }

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    void Update()
    {
        float speed = Mathf.Max(0f, animationSpeed);
        if (sourceAnimator != null)
            sourceAnimator.speed = speed;

        foreach (GameObject clone in activeAfterimages)
        {
            if (clone == null)
                continue;
            Animator cloneAnimator = clone.GetComponent<Animator>();
            if (cloneAnimator != null)
                cloneAnimator.speed = speed;
        }
    }

    IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            activeAfterimages.RemoveAll(clone => clone == null);
            if (activeAfterimages.Count < Mathf.Clamp(maxSimultaneous, 1, 4))
                SpawnAfterimage();

            yield return new WaitForSeconds(Mathf.Max(0.01f, spawnInterval));
        }
    }

    void SpawnAfterimage()
    {
        if (prefab == null)
        {
            Debug.LogError("AfterimageSpawner needs a prefab or source object.", this);
            enabled = false;
            return;
        }

        GameObject clone = Instantiate(
            prefab,
            transform.position,
            transform.rotation
        );

        clone.transform.localScale = transform.lossyScale;

        // A scene object can be used as the template. Its copy must not spawn again.
        AfterimageSpawner cloneSpawner = clone.GetComponent<AfterimageSpawner>();
        if (cloneSpawner != null)
            cloneSpawner.enabled = false;

        Animator cloneAnimator = clone.GetComponent<Animator>();
        if (cloneAnimator != null)
        {
            cloneAnimator.enabled = true;
            cloneAnimator.speed = Mathf.Max(0f, animationSpeed);
        }

        SpriteRenderer cloneRenderer = clone.GetComponent<SpriteRenderer>();
        if (cloneRenderer != null)
        {
            cloneRenderer.enabled = true;
            if (sourceRenderer != null)
            {
                cloneRenderer.sprite = sourceRenderer.sprite;
                cloneRenderer.color = sourceRenderer.color;
            }
        }

        activeAfterimages.Add(clone);
        StartCoroutine(FadeAndDestroy(clone));
    }

    IEnumerator FadeAndDestroy(GameObject clone)
    {
        SpriteRenderer sr = clone.GetComponent<SpriteRenderer>();

        if (sr == null)
        {
            Destroy(clone, waitTime + fadeDuration);
            yield break;
        }

        yield return new WaitForSeconds(waitTime);

        Color originalColor = sr.color;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            if (sr == null)
                yield break;

            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / fadeDuration);

            Color color = originalColor;
            color.a = Mathf.Lerp(originalColor.a, 0f, t);

            sr.color = color;

            yield return null;
        }

        Destroy(clone);
    }
}
