using UnityEngine;

public class PiggyButtonAnimation : MonoBehaviour
{
    
    public Animator animator;
    public bool isRight = false;
    
    public PiggyAnimationController controller;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isRight==false)
        {
            animator.Play("trigger", 0, 0f);
            isRight = true;
        }
        
        if (other.CompareTag("Piggy" ) && isRight==true)
        {
            animator.Play("triggerToLeft", 0, 0f);

            if (controller.isEnd)
            {
                isRight = false;
                controller.isEnd = false;
            }

        }
    }
    
    
}
