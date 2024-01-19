using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HapticDeviceGrip : HapticDevice
{
    public enum e_gripCtrl
    {
        SPEED_CTRL = 0,
        FORCE_CTRL = 0
    }

    [SerializeField]
    private e_gripCtrl currentGripCtrl = e_gripCtrl.SPEED_CTRL;

    public e_gripCtrl GripCtrl { get => currentGripCtrl; set => currentGripCtrl = value; }

    public void setSource(HapticSource si, e_gripCtrl gripCtrl)
    {
        GripCtrl = gripCtrl;
        setSource(si);
    }


}
