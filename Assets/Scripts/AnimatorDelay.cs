using UnityEngine;
using System.Collections;
public class AnimatorDelay : MonoBehaviour
{
    public Animator targetAnimator;

    public float minDelay = 0f;
    public float maxDelay = 8f;

    [Header("Running Piggy Audio")]
    [Tooltip("X = minimum pitch, Y = maximum pitch")]
    public Vector2 pitchRange = new Vector2(0.85f, 1.15f);
    [Tooltip("X = minimum volume, Y = maximum volume")]
    public Vector2 volumeRange = new Vector2(0.7f, 1f);

    private AudioSource runningPiggyAudio;

    void Start()
    {
        if (targetAnimator == null)
            return;

        if (CompareTag("RunningPiggy"))
        {
            runningPiggyAudio = GetComponent<AudioSource>();
            if (runningPiggyAudio != null)
            {
                runningPiggyAudio.Stop();
                runningPiggyAudio.pitch = RandomInRange(pitchRange, 0.1f, 3f);
                runningPiggyAudio.volume = RandomInRange(volumeRange, 0f, 1f);
            }
        }
        
        targetAnimator.speed = 0f;
        float randomDelay = Random.Range(minDelay, maxDelay);
        StartCoroutine(PlayAfterDelay(randomDelay));
    }

    IEnumerator PlayAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        targetAnimator.speed = 1f;
        if (runningPiggyAudio != null && runningPiggyAudio.clip != null)
            runningPiggyAudio.Play();
    }

    private static float RandomInRange(Vector2 range, float minimum, float maximum)
    {
        float low = Mathf.Clamp(Mathf.Min(range.x, range.y), minimum, maximum);
        float high = Mathf.Clamp(Mathf.Max(range.x, range.y), minimum, maximum);
        return Random.Range(low, high);
    }
}
