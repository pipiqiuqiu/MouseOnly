using UnityEngine;

public class LockerReveal : MonoBehaviour
{
    [SerializeField] private GameObject featureParent;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<InchwormLocomotion>() == null)
            return;

        if (featureParent != null)
            featureParent.SetActive(true);
    }
}
