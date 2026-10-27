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
    [Min(0f)] public float minimumSpawnDistance = 2f;
    [Min(0f)] public float animationSpeed = 1f;
    [Min(0.1f)] public float waterProbeDistance = 4f;

    private readonly List<GameObject> activeAfterimages = new List<GameObject>();
    private readonly RaycastHit[] waterHits = new RaycastHit[32];
    private Vector3 lastSpawnPosition;
    private bool hasSpawned;
    private bool isOverWater;
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
        isOverWater = IsOverWater();
        if (sourceAnimator != null && !isOverWater)
            sourceAnimator.speed = 0f;
        StartCoroutine(SpawnLoop());
    }

    void Update()
    {
        bool overWaterNow = IsOverWater();
        if (isOverWater && !overWaterNow)
            ClearAfterimages();
        isOverWater = overWaterNow;

        float speed = isOverWater ? Mathf.Max(0f, animationSpeed) : 0f;
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
            if (!isOverWater)
            {
                yield return new WaitForSeconds(Mathf.Max(0.01f, spawnInterval));
                continue;
            }

            activeAfterimages.RemoveAll(clone => clone == null);
            bool farFromLastSpawn = hasSpawned &&
                Vector3.Distance(transform.position, lastSpawnPosition) > minimumSpawnDistance;
            int allowedCount = farFromLastSpawn ? Mathf.Clamp(maxSimultaneous, 1, 4) : 1;

            while (activeAfterimages.Count > allowedCount)
            {
                Destroy(activeAfterimages[0]);
                activeAfterimages.RemoveAt(0);
            }

            if (activeAfterimages.Count < allowedCount)
                SpawnAfterimage();

            yield return new WaitForSeconds(Mathf.Max(0.01f, spawnInterval));
        }
    }

    private bool IsOverWater()
    {
        Transform playerHead = transform.parent != null ? transform.parent : transform;
        Ray ray = new Ray(playerHead.position + Vector3.up * 0.5f, Vector3.down);
        float probeDistance = Mathf.Max(0.1f, waterProbeDistance);
        int count = Physics.RaycastNonAlloc(ray, waterHits, probeDistance,
            ~0, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            if (IsWater(waterHits[i].collider))
                return true;
        }

        if (count == waterHits.Length)
        {
            foreach (RaycastHit hit in Physics.RaycastAll(ray, probeDistance,
                ~0, QueryTriggerInteraction.Collide))
            {
                if (IsWater(hit.collider))
                    return true;
            }
        }

        return false;
    }

    private static bool IsWater(Collider collider)
    {
        for (Transform current = collider.transform; current != null; current = current.parent)
        {
            if (current.CompareTag("water"))
                return true;
        }
        return false;
    }

    private void ClearAfterimages()
    {
        foreach (GameObject clone in activeAfterimages)
        {
            if (clone != null)
                Destroy(clone);
        }
        activeAfterimages.Clear();
        hasSpawned = false;
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
            ClearAfterimages();
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
        lastSpawnPosition = transform.position;
        hasSpawned = true;
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

        if (sr == null)
            yield break;

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
