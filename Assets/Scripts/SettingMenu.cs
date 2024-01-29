using System;
using System.Collections.Generic;
using System.IO;
using Franka;
using UnityEngine;
using TMPro; // Include the TextMeshPro namespace

public class SettingMenu : MonoBehaviour
{
    private RedisConnection redisConnection;
    public GameObject gameManager;
    public TextMeshPro SideText; // Change type to TextMeshPro for 3D text
    public TextMeshPro DeviceText;
    private string side = "right"; // Valeur par défaut
    private string device = "robot"; // Valeur par défaut

    void Start()
    {
        gameManager = GameObject.Find("GameManager");
        redisConnection = gameManager.GetComponent<RedisConnection>(); 
    }

    public void ToggleSideAndPublish()
    {
        // Basculer 'side' entre 'left' et 'right'
        side = side == "left" ? "right" : "left";

        // Construire et publier le message
        string message = "Side" + ";" + side;
        redisConnection.publisher.Publish(redisConnection.redisChannels["game_parameters"], message);
        SideText.text = side; // Update the text using TextMeshPro
        Debug.Log("Published Sceneside: " + side);
    }
    public void ToggleDeviceAndPublish()
    {
        // Basculer 'side' entre 'left' et 'right'
        device = device == "haptic" ? "robot" : "haptic";

        // Construire et publier le message
        string message = "SceneType" + ";" + device;
        redisConnection.publisher.Publish(redisConnection.redisChannels["game_parameters"], message);
        DeviceText.text = device; // Update the text using TextMeshPro
        Debug.Log("Published device: " + device);
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            ToggleSideAndPublish();     
        } 
        if(Input.GetKeyDown(KeyCode.X))
        {
            ToggleDeviceAndPublish();        }  
    }
}
