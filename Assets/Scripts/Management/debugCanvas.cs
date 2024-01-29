using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class debugCanvas : MonoBehaviour
{
    public TextMeshProUGUI logText;

    void OnEnable()
    {
        Application.logMessageReceived += LogCallback;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= LogCallback;
    }

    void LogCallback(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Exception)
        {
            logText.text = logString;
            //Or Append the log to the old one
            //logText.text += logString + "\r\n";
        }
    }
}
