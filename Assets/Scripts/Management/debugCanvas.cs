using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

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
            logText.text += "<color=red>" + logString + "</color>\r\n";
            // add the exception details to the logText but only the beginning of the stack trace
            logText.text += "<color=yellow>" + stackTrace.Split('\n').First() + "</color>\r\n";
            //Or Append the log to the old one
            //logText.text += logString + "\r\n";
        }
    }
}
