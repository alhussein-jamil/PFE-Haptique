using UnityEngine;
using Franka;
using TMPro;
public class RedisState : MonoBehaviour
{
    public GameObject GameManager; // Assignez votre GameObject ici
    public TextMeshProUGUI connectionText; // Assignez votre composant TextMeshProUGUI ici
    private RedisConnection redisConnection;
    void Start()
    {


        GameManager = GameObject.Find("GameManager");
        redisConnection = GameManager.GetComponent<RedisConnection>();
        UpdateConnectionStatus();
    }

    void Update()
    {
        UpdateConnectionStatus();
    }

    void UpdateConnectionStatus()
    {
        if (redisConnection == null)
        {
            Debug.LogError("RedisConnection component is not found");
            return;
        }

        if (connectionText == null)
        {
            Debug.LogError("TextMeshProUGUI component is not assigned in RedisState script");
            return;
        }

        if (!redisConnection.doneInit)
        {
            connectionText.text = "Connecting...";
            connectionText.color = Color.yellow;
            return;
        }
        
        bool isConnected = redisConnection.redis.IsConnected;

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
