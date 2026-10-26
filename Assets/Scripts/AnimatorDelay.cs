using UnityEngine;
using System.Collections;
public class AnimatorDelay : MonoBehaviour
{
    public Animator targetAnimator;

    public float minDelay = 0f;
    public float maxDelay = 8f;

    void Start()
    {
        if (targetAnimator == null)
            return;
        
        targetAnimator.speed = 0f;
        float randomDelay = Random.Range(minDelay, maxDelay);
        StartCoroutine(PlayAfterDelay(randomDelay));
    }

    IEnumerator PlayAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        targetAnimator.speed = 1f;
    }
}
