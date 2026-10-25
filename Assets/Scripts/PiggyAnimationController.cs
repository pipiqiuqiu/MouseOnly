using UnityEngine;

public class PiggyAnimationController : MonoBehaviour
{
    public PiggyButtonAnimation button;
    public Animator animator;
    private bool hasTriggered = false;
    
    private bool hasTriggeredWalking = false;
    
    private bool hasStoppedWalking = false;
    private bool hasStoppedWalkingBack = false;
    private bool hasEnteredDown = false;

    public bool isEnd;
    public bool IsReady => !hasTriggered;
    

    void Update()
    {
        if (button.isRight && !hasTriggered)
        {
            hasTriggered = true;
            isEnd = false;
            hasEnteredDown = false;
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

        
        if (positionState.IsName("Piggy_walkposition"))
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
            hasEnteredDown = true;
        }

        if (hasEnteredDown && state.IsName("New State") &&
            positionState.IsName("New State") &&
            !animator.IsInTransition(0) && !animator.IsInTransition(1))
        {
            isEnd = true;
            hasTriggered = false;
            hasEnteredDown = false;
            button.CompleteCycle();
        }
        
    }
    
}
