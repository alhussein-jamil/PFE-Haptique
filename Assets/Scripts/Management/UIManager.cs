using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject ExperienceUI;
    public GameObject FeedBackUI;
    public GameObject ManagementUI;
    public GameObject[] IndicatorsUI;
    public GameObject gameManger;
    // Start is called before the first frame update
    void Start()
    {

        gameManger = GameObject.Find("GameManager");
        SetSide(gameManger.GetComponent<GManager>().gameParameters["Side"]);

        ExperienceUI.SetActive(false);
        FeedBackUI.SetActive(false);
        ManagementUI.SetActive(false);
        foreach (GameObject indicator in IndicatorsUI)
        {
            indicator.GetComponent<MeshRenderer>().enabled = false;
        }
        ShowExperienceUI();
        ShowIndicatorsUI();
    }
    public void ShowExperienceUI()
    {
        ExperienceUI.SetActive(true);
    }
    public async void HideExperienceUI()
    {
        // wait for 0.5s
        await Task.Delay(500);
        HideIndicatorsUI();
        ExperienceUI.SetActive(false);
        ShowManagementUI();
    }
    public void ShowFeedBackUI()
    {
        FeedBackUI.SetActive(true);

    }
    public async Task HideFeedBackUIAsync()
    {
        // wait for 0.5s
        await Task.Delay(500);
        FeedBackUI.SetActive(false);
        ShowExperienceUI();

    }
    public void ShowManagementUI()
    {
        ManagementUI.SetActive(true);
    }
    public async Task HideManagementUI()
    {
                // wait for 0.5s
        await Task.Delay(500);
        ManagementUI.SetActive(false);
        ShowFeedBackUI();
    }
    public void ShowIndicatorsUI()
    {
        foreach (GameObject indicator in IndicatorsUI)
        {
            indicator.GetComponent<MeshRenderer>().enabled = true;
        }
    }
    public void HideIndicatorsUI()
    {
        foreach (GameObject indicator in IndicatorsUI)
        {
            indicator.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    void SetSide(string side)
    {
        float factor = side == "left" ? +1 : -1;
        ExperienceUI.transform.position = new Vector3(Mathf.Abs(ExperienceUI.transform.position.x) * factor , ExperienceUI.transform.position.y, ExperienceUI.transform.position.z);
        FeedBackUI.transform.position = new Vector3(Mathf.Abs(FeedBackUI.transform.position.x)* factor , FeedBackUI.transform.position.y, FeedBackUI.transform.position.z);
        ManagementUI.transform.position = new Vector3(Mathf.Abs(ManagementUI.transform.position.x) * factor , ManagementUI.transform.position.y, ManagementUI.transform.position.z);
        foreach (GameObject indicator in IndicatorsUI)
        {
            indicator.transform.position = new Vector3(Mathf.Abs(indicator.transform.position.x)* factor , indicator.transform.position.y, indicator.transform.position.z);
        }


    }


    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E))
            HideExperienceUI();
        if(Input.GetKeyDown(KeyCode.F))
            HideFeedBackUIAsync();
        if(Input.GetKeyDown(KeyCode.M))
            HideManagementUI();

    }
}
