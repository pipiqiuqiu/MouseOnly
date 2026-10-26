using UnityEngine;

public class FastForwardTrigger : MonoBehaviour
{
    public Animator targetAnimator;

    public string[] triggerNames = { "quick", "quick2", "quick3" };

    private int currentIndex = 0;
    public bool isEnd = false;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (targetAnimator == null || triggerNames.Length == 0)
                return;

            targetAnimator.SetTrigger(triggerNames[currentIndex]);

            currentIndex++;

            if (currentIndex >= triggerNames.Length)
            {
                isEnd = true;
                currentIndex = 0;
            }
        }
    }
}