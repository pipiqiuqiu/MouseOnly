using UnityEngine;

public class PiggyAnimationController : MonoBehaviour
{
    public PiggyButtonAnimation button;
    public Animator animator;
    private bool hasTriggered = false;
    void Update()
    {
        if (button.isRight && !hasTriggered)
        {
            hasTriggered = true;
            animator.SetTrigger("PiggyStart");
        }
    }
    
}
