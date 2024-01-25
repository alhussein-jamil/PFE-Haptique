using System;
using UnityEngine;
using Franka;
using UnityEngine.UI;

public class RedisState : MonoBehaviour
{
    public GameObject EmikaBrush; // Assignez votre GameObject ici
    public Text connectionText; // Assignez votre composant TextMeshProUGUI ici

    void Start()
    {
        UpdateConnectionStatus();
    }

    void Update()
    {
        UpdateConnectionStatus();
    }

    void UpdateConnectionStatus()
    {
        if (EmikaBrush == null)
        {
            Debug.LogError("EmikaBrush GameObject is not assigned in RedisState script");
            return;
        }

        RedisConnection redisConnection = EmikaBrush.GetComponent<RedisConnection>();
        if (redisConnection == null)
        {
            Debug.LogError("RedisConnection component is not found on EmikaBrush GameObject");
            return;
        }

        if (connectionText == null)
        {
            Debug.LogError("TextMeshProUGUI component is not assigned in RedisState script");
            return;
        }

        bool isConnected = redisConnection.doneInit;

        if (isConnected)
        {
            connectionText.text = "Connected";
            connectionText.color = Color.green;
        }
        else
        {
            connectionText.text = "Disconnected";
            connectionText.color = Color.red;
        }
    }
}
