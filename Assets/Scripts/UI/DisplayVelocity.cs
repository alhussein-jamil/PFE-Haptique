using UnityEngine;
using UnityEngine.UI; 
using TMPro;
public class DisplayVelocity : MonoBehaviour
{
    public GManager gameManager; 
    public TextMeshProUGUI velocityText; 


    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GManager>();
    }
    
    void Update()
    {
        
        if(gameManager != null && velocityText != null)
        {
            string velocityStr = gameManager.gameParameters["velocite.tactile"];

            
            if (float.TryParse(velocityStr, out float velocity))
            {
              
                velocityText.text = "Caresse Speed : " + velocity.ToString();
            }
            else
            {
                
                velocityText.text = "Conversion Error";
            }
        }
    }
}