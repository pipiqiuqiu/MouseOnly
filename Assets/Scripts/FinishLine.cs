using UnityEngine;
using System.Collections;

public class FinishLine : MonoBehaviour
{
    public bool piggyWin = false;
    public bool playerWin = false;

    public float cooldownTime = 5f;

    private bool isCooldown = false;

    void OnTriggerEnter(Collider other)
    {
        if (isCooldown)
            return;

        if (other.CompareTag("RunningPiggy"))
        {
            piggyWin = true;
            Debug.Log("Piggy Wins!");

            StartCoroutine(WinCooldown());
        }
        else if (other.CompareTag("Player"))
        {
            playerWin = true;
            Debug.Log("Player Wins!");

            StartCoroutine(WinCooldown());
        }
    }

    IEnumerator WinCooldown()
    {
        isCooldown = true;

        yield return new WaitForSeconds(cooldownTime);
        
        piggyWin = false;
        playerWin = false;

        isCooldown = false;
    }
}