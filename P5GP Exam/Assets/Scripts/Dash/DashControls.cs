using System;
using System.Collections;
using System.Collections.Generic;
using Features.Revised_Controller;
using UnityEngine;

public class DashControls : MonoBehaviour
{
    public bool IsDashPressed { get; set; }
    public bool IsDashReleased { get; set; }
    
    void Start()
    {
        ControlsManager.Instance.Subscribe(this, "Dash", ControlsManager.ControlEvent.OnPressed, DashControlPressed);
        ControlsManager.Instance.Subscribe(this, "Dash", ControlsManager.ControlEvent.OnReleased, DashControlReleased);
    }

    void DashControlPressed()
    {
        IsDashPressed = true;
        IsDashReleased = false;
    }
    void DashControlReleased()
    {
        IsDashPressed = false;
        IsDashReleased = true;
    }
    
    private void OnDestroy()
    {
        ControlsManager.Instance.UnsubscribeAll(this);
    }
}
