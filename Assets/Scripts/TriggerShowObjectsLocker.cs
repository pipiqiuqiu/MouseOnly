using UnityEngine;
using System.Collections;

public class TriggerShowObjectsLocker : MonoBehaviour
{
    
    // sprite object are all applied
    public GameObject[] objectsToShow;
    public float showDuration = 3f;

    private bool isShowing = false;

    void Start()
    {
        foreach (GameObject obj in objectsToShow)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
       
        if (other.CompareTag("Player") && !isShowing)
        {
            StartCoroutine(ShowRoutine());
        }

    }

    IEnumerator ShowRoutine()
    {
        isShowing = true;


        foreach (GameObject obj in objectsToShow)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        //wait for 3 seconds
        yield return new WaitForSeconds(showDuration);


        foreach (GameObject obj in objectsToShow)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        // can reshow
        isShowing = false;
    }
}
