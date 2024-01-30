using Franka;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackManager : MonoBehaviour
{   
    public GameObject gameManager;
    public Slider pleasureSlider;
    public Slider intensitySlider;
    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.Find("GameManager");
    }

    public void publishSensationData()
    {
        if(!gameManager.GetComponent<RedisConnection>().redis.IsConnected)
            return;
        Debug.Log("Publishing sensation data");
        string message = "pleasantness : " + pleasureSlider.value + ";intensity : " + intensitySlider.value;
        gameManager.GetComponent<RedisConnection>().publisher.Publish(gameManager.GetComponent<RedisConnection>().redisChannels["feedback"], message);
    }

}
