using UnityEngine;

public class wantedPiggy : MonoBehaviour
{
    public FastForwardTrigger CarAnimation;
    public Sprite sprite1;
    public Sprite sprite2;
    
    [SerializeField] private GameObject featureParent;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.GetComponent<SpriteRenderer>().sprite = sprite1;
    }

    // Update is called once per frame
    void Update()
    {
        if (CarAnimation.isEnd)
        {
            gameObject.GetComponent<SpriteRenderer>().sprite = sprite2;
            
            if (featureParent != null)
                featureParent.SetActive(true);
            
        }
    }
}
