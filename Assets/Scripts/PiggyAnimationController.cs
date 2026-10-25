using UnityEngine;

public class PiggyAnimationController : MonoBehaviour
{
    public PiggyButtonAnimation button;
    public Animator animator;
    private bool hasTriggered = false;
    
    private bool hasTriggeredWalking = false;
    
    private bool hasStoppedWalking = false;
    private bool hasStoppedWalkingBack = false;

    public bool isEnd;
    void Update()
    {
        if (button.isRight && !hasTriggered)
        {
            hasTriggered = true;
            animator.SetTrigger("PiggyStart");
        }
        
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Piggy_walk") && !hasTriggeredWalking)
        {
            hasTriggeredWalking = true;
            animator.SetTrigger("isWalk");
        }

        if (!state.IsName("Piggy_walk"))
        {
            hasTriggeredWalking = false;
        }
        
        AnimatorStateInfo positionState = animator.GetCurrentAnimatorStateInfo(1);
        if (positionState.IsName("Piggy_Jumpposition") && !hasStoppedWalking)
        {
            hasStoppedWalking = true;
            animator.SetTrigger("StopWalking");
        }

        
        if (!positionState.IsName("Piggy_walkposition"))
        {
            hasStoppedWalking = false;
        }
        
        if (positionState.IsName("Piggy_walkbackposition") && !hasStoppedWalkingBack)
        {
            hasStoppedWalkingBack = true;
            animator.SetTrigger("isWalk");
        }
        if (!positionState.IsName("Piggy_walkbackposition")&& hasStoppedWalkingBack)
        {
            animator.SetTrigger("PiggySit");
            hasStoppedWalkingBack = false;
        }
        
        if (state.IsName("Piggy_down"))
        {
            isEnd = true;
        }
        
    }
    
}
