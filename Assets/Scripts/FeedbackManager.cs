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

        // add callback to sliders 
        pleasureSlider.onValueChanged.AddListener(delegate { pleasureValueChanged(); });
        intensitySlider.onValueChanged.AddListener(delegate { intensityValueChanged(); });
    }

    void pleasureValueChanged()
    {
        if(!gameManager.GetComponent<RedisConnection>().redis.IsConnected)
            return;
        Debug.Log("Pleasure value changed");
        string message = "pleasure;" + pleasureSlider.value;
        gameManager.GetComponent<RedisConnection>().publisher.Publish(gameManager.GetComponent<RedisConnection>().redisChannels["feedback"], message);
    }
    void intensityValueChanged()
    {
        if(!gameManager.GetComponent<RedisConnection>().redis.IsConnected)
            return;
        Debug.Log("Intensity value changed");
        string message = "intensity;" + intensitySlider.value;
        gameManager.GetComponent<RedisConnection>().publisher.Publish(gameManager.GetComponent<RedisConnection>().redisChannels["feedback"], message);
    }
    // Update is called once per frame
    void Update()
    {

        
    }
}
