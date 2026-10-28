using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
public class HoverSceneIcon : MonoBehaviour
{
     [SerializeField] private string targetSceneName = "mouseOnly";
    [SerializeField] private float hoverTime = 1f;
    [SerializeField] private float growTime = 0.25f;
    [SerializeField] private float shrinkTime = 0.3f;
    [SerializeField] private float growScale = 1.2f;
    [SerializeField] private float finalScale = 0.8f;

    private Vector3 originalScale;
    private Coroutine routine;
    private bool activated;

    private void Awake()
    {
        originalScale = transform.localScale;
    }


    private void OnMouseEnter() => BeginHover();
    private void OnMouseExit() => EndHover();

    private void BeginHover()
    {
        if (routine == null && !activated)
            routine = StartCoroutine(HoverThenLoad());
    }

    private void EndHover()
    {
        if (activated || routine == null) return;

        StopCoroutine(routine);
        routine = null;
        transform.localScale = originalScale;
    }

    private IEnumerator HoverThenLoad()
    {
        yield return new WaitForSecondsRealtime(hoverTime);
        activated = true;

        yield return ScaleTo(growScale, growTime);
        yield return ScaleTo(finalScale, shrinkTime);

        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }

    private IEnumerator ScaleTo(float factor, float duration)
    {
        Vector3 start = transform.localScale;
        Vector3 target = originalScale * factor;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t); 
            transform.localScale = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.localScale = target;
    }
}
