using UnityEngine;

public class PiggyButtonAnimation : MonoBehaviour
{
    
    public Animator animator;
    public Animator RingAnimator;
    public Animator ComputerAnimator;
    public bool isRight = false;
    
    public PiggyAnimationController controller;
    private bool hasPlayedReturnAnimation = false;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isRight &&
            (controller == null || controller.IsReady))
        {
            animator.Play("trigger", 0, 0f);
            RingAnimator.SetBool("RingTrigger", true);
            ComputerAnimator.SetBool("isTrigger", true);
            isRight = true;
            hasPlayedReturnAnimation = false;
        }
        
        if (other.CompareTag("Piggy") && isRight && !hasPlayedReturnAnimation)
        {
            animator.Play("triggerToLeft", 0, 0f);
            RingAnimator.SetBool("RingTrigger", false);
            ComputerAnimator.SetBool("isTrigger", false);
            hasPlayedReturnAnimation = true;
        }
    }

    public void CompleteCycle()
    {
        if (!hasPlayedReturnAnimation)
        {
            animator.Play("triggerToLeft", 0, 0f);
        }

        isRight = false;
        hasPlayedReturnAnimation = false;
    }
}
