using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIControl : MonoBehaviour
{
    public string deviceName = "VibratorDevice";
    public KeyCode showIHMKey = KeyCode.F1;
    public GameObject COMPanel;
    public Button btnStart;
    public TMP_InputField inputField;
    // Start is called before the first frame update
    void Start()
    {
        inputField.text = SerialCOMManager.Instance.getDeviceParameter(deviceName).Port;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(showIHMKey))
        {
            COMPanel.SetActive(!COMPanel.activeSelf);
        }
    }
    
    public void startCom()
    {
        string txt = inputField.text;
        if (txt != "")
        {
            /*SerialCom.Instance.GetPortsName()*/ //tester si dans la list
            SerialCOMManager.Instance.getDeviceParameter(deviceName).Port = txt;
            SerialCOMManager.Instance.StartCom();
        }
        Debug.Log("Started");
        btnStart.gameObject.SetActive(false);
    }

}
