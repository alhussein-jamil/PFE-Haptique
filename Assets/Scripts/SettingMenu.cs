using System;
using System.Collections.Generic;
using System.IO;
using Franka;
using UnityEngine;

public class SettingMenu : MonoBehaviour
{
    private RedisConnection redisConnection;
    public GameObject gameManager;
    private string side; // Déclaration de la variable side

    void Start()
    {
        gameManager = GameObject.Find("GameManager");
        redisConnection = gameManager.GetComponent<RedisConnection>();
    }

    public void PublishSceneRightSide()
    {   
        side = "right"; 
        redisConnection.publisher.Publish(redisConnection.redisChannels["Side"], side);
        Debug.Log("Published Sceneside: " + side);
        UnityEngine.SceneManagement.SceneManager.LoadScene("RobotScene");
    }

    public void PublishSceneLeftSide()
    {   
        side = "left";
        redisConnection.publisher.Publish(redisConnection.redisChannels["Side"], side);
        Debug.Log("Published Sceneside: " + side);
        UnityEngine.SceneManagement.SceneManager.LoadScene("RobotScene");
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E))
            PublishSceneRightSide();
        if(Input.GetKeyDown(KeyCode.Z))
            PublishSceneLeftSide();
    }
}
