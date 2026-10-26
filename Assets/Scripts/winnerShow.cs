using System;
using UnityEngine;
using Object = System.Object;
using System.Collections;

public class winnerShow : MonoBehaviour
{
   public Sprite sprite;
   public Sprite sprite2;

   public FinishLine finishLine;

   public float showTime = 1f;
   
   private SpriteRenderer spriteRenderer;

   private bool lastPiggyWin = false;
   private bool lastPlayerWin = false;

   
   void Start()
   {
      spriteRenderer = GetComponent<SpriteRenderer>();

      spriteRenderer.enabled = false;
   }

   void Update()
   {
      if (finishLine == null)
         return;

      if (finishLine.piggyWin && !lastPiggyWin)
      {
         StartCoroutine(ShowSprite(sprite));
      }

      if (finishLine.playerWin && !lastPlayerWin)
      {
         StartCoroutine(ShowSprite(sprite2));
      }

      lastPiggyWin = finishLine.piggyWin;
      lastPlayerWin = finishLine.playerWin;
   }

   IEnumerator ShowSprite(Sprite targetSprite)
   {
      spriteRenderer.sprite = targetSprite;
      spriteRenderer.enabled = true;

      yield return new WaitForSeconds(showTime);

      spriteRenderer.enabled = false;
   }
}
