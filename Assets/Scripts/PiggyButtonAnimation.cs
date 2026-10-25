using UnityEngine;

public class PiggyButtonAnimation : MonoBehaviour
{
    public Animator animator;
    public bool isRight = false;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isRight==false)
        {
            animator.Play("trigger", 0, 0f);
            isRight = true;
        }
    }
}
