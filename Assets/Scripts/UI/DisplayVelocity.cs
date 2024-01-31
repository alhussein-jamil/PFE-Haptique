using UnityEngine;
using UnityEngine.UI; 
using TMPro;
using System.Globalization;
using Franka;
public class DisplayVelocity : MonoBehaviour
{
    public GManager gameManager; 
    public TextMeshProUGUI velocityText; 
    public CaresseManager caresseManager;

    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GManager>();

    }
    
    void Update()
    {
        
        if(gameManager != null && velocityText != null)
        {
                
                velocityText.text = "Experience \n ("+ caresseManager.speedidx.ToString() + ")";
        }
    }
}