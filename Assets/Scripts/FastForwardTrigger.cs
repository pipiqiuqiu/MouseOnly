using UnityEngine;
using System.Collections;
public class FastForwardTrigger : MonoBehaviour
{
    public Animator targetAnimator;

    public string triggerName = "FastForward";
    
    private bool isTriggered = false;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")&& !isTriggered)
        {
            targetAnimator.SetTrigger(triggerName);
            
        }
    }
    
   
}
