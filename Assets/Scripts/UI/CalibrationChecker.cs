using UnityEngine;
using UnityEngine.UI; 
using TMPro;
using Franka;
public class CalibrationChecker : MonoBehaviour
{   
    public GManager gameManager; 
    // public Text statusText; 
    public TextMeshProUGUI statusText;
    public GameObject franka;

    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GManager>();
        franka = GameObject.Find("EmikaBrush");
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
            if(franka.GetComponent<Command>().firstMove)
            {
                statusText.color = Color.yellow;
                statusText.text = "Calibrating...";
            }
            else
            {
                statusText.color = Color.red;
                statusText.text = "Not Calibrated";
            }
        }
    }
}
