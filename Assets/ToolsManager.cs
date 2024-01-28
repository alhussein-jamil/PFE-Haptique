using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolsManager : MonoBehaviour
{
    public GameObject gameManager;
    public GameObject Robot;
    public GameObject HapticDevices;
    // Start is called before the first frame update
    void Start()
    {
        if (gameManager == null)
        gameManager = GameObject.Find("GameManager");


        
    }

    // Update is called once per frame
    void Update()
    {
        
        Robot.SetActive(gameManager.GetComponent<GManager>().gameParameters["SceneType"] == "robot");
        HapticDevices.SetActive(gameManager.GetComponent<GManager>().gameParameters["SceneType"] == "haptic");
        
    }
}
