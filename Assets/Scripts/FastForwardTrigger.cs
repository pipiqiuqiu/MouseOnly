using UnityEngine;

public class FastForwardTrigger : MonoBehaviour
{
    public Animator targetAnimator;

    public string[] triggerNames = { "quick", "quick2", "quick3" };

    private int currentIndex = 0;
    public bool isEnd = false;
    
    public AudioSource targetAudio;
    public AudioSource DamageAudio;
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") ||
            targetAnimator == null ||
            triggerNames == null ||
            triggerNames.Length == 0)
            return;

        targetAnimator.SetTrigger(triggerNames[currentIndex]);

        if (targetAudio != null)
        {
            switch (currentIndex)
            {
                case 0: // quick
                    targetAudio.pitch = 1.5f;
                    if (!targetAudio.isPlaying) targetAudio.Play();
                    break;

                case 1: // quick2
                    targetAudio.pitch = 2f;
                    if (!targetAudio.isPlaying) targetAudio.Play();
                    break;

                case 2: // quick3
                    targetAudio.Stop();
                    DamageAudio.Play();
                    break;
            }
        }

        currentIndex++;

        if (currentIndex >= triggerNames.Length)
        {
            isEnd = true;
        }
    }
    
}