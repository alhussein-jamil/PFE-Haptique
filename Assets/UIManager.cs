using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject ExperienceUI;
    public GameObject FeedBackUI;
    public GameObject ManagementUI;
    public GameObject[] IndicatorsUI;
    // Start is called before the first frame update
    void Start()
    {
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
    public void HideExperienceUI()
    {
        HideIndicatorsUI();
        ExperienceUI.SetActive(false);
        ShowManagementUI();
    }
    public void ShowFeedBackUI()
    {
        FeedBackUI.SetActive(true);

    }
    public void HideFeedBackUI()
    {
        FeedBackUI.SetActive(false);
        ShowExperienceUI();

    }
    public void ShowManagementUI()
    {
        ManagementUI.SetActive(true);
    }
    public void HideManagementUI()
    {
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

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E))
            HideExperienceUI();
        if(Input.GetKeyDown(KeyCode.F))
            HideFeedBackUI();
        if(Input.GetKeyDown(KeyCode.M))
            HideManagementUI();
        
    }
}
