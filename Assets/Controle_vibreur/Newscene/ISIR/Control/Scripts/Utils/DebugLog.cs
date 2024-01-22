using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugLog : SingletonBehaviour<DebugLog>
{
    public enum e_LogLvl
    {
        NONE = 0, VERBOSE =17,  DEBUG = 7, WARNING = 3, ERROR = 1, 
    }

    //TODO check in config if group are enable
    /*    [SerializeField]
        private string _configurationFile = "globalSetting.json";
    */
    Dictionary<string, e_LogLvl> enabledGroup = new Dictionary<string, e_LogLvl>();
    protected override bool Awake()
    {
        if (base.Awake())
        {
            //initialize here
            return true;
        }
        else
            return false;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }
    
    public void Log(string group, string msg, e_LogLvl lvl = e_LogLvl.DEBUG )
    {

        if ((!enabledGroup.ContainsKey(group) || ((int)enabledGroup[group] & (int)lvl) == (int)lvl))
        {
            switch(lvl) { 
                case e_LogLvl.WARNING:
                    Debug.LogWarning("[" + lvl.ToString() + "] ("+ group + "): " + msg);
                    break; 
                case e_LogLvl.ERROR:
                    Debug.LogError("[" + lvl.ToString() + "] ("+ group + "): " + msg);
                    break;
                case e_LogLvl.DEBUG:
                case e_LogLvl.VERBOSE:
                    Debug.Log("[" + lvl.ToString() + "] ("+ group + "): " + msg);
                    break;
            }
        }
    }

    public void setDebug(string group, e_LogLvl enable)
    {
       if( enabledGroup.ContainsKey(group) )
       {
            enabledGroup[group] = enable;
       }
       else
       {
            enabledGroup.Add(group,enable);
       }
    }

}
