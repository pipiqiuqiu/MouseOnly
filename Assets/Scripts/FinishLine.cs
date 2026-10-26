using UnityEngine;
using System.Collections;
public class FinishLine : MonoBehaviour
{
    public bool piggyWin = false;
    public bool playerWin = false;

    public float checkInterval = 10f;
    private bool winnerAnnounced = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("RunningPiggy"))
            piggyWin = true;
        else if (other.CompareTag("Player"))
            playerWin = true;
        else
            return;

        CheckWin(); 
    }

    IEnumerator CheckWinRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            CheckWin(); 
        }
    }

    void CheckWin()
    {
        if (winnerAnnounced) return;

        if (piggyWin)
        {
            Debug.Log("Piggy Wins!");
            winnerAnnounced = true;
        }
        else if (playerWin)
        {
            Debug.Log("Player Wins!");
            winnerAnnounced = true;
        }
    }
}
