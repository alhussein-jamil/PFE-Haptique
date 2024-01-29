using UnityEngine;
using UnityEngine.UI; 

public class CalibrationChecker : MonoBehaviour
{   
    public GManager gameManager; 
    public Text statusText; 

    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GManager>();
    }
    void Update()
    {
        
        if (gameManager.calibrationDataLength > 0)
        {
            
            statusText.color = Color.green;
            statusText.text = "Calibrated";
        }
        else
        {
            
            statusText.color = Color.yellow; 
            statusText.text = "calibrating...";
        }
    }
}
