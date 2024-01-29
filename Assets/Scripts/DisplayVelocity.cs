using UnityEngine;
using UnityEngine.UI; 

public class DisplayVelocity : MonoBehaviour
{
    public GManager gameManager; 
    public Text velocityText; 


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
              
                velocityText.text = "Vélocité : " + velocity.ToString();
            }
            else
            {
                
                velocityText.text = "Erreur de conversion";
            }
        }
    }
}