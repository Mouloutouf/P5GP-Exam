using System;
using Features.Revised_Controller;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

public class AimControls : MonoBehaviour
{
    public AimingBehaviour aimingBehaviour;
    
    private void Start()
    {
        ControlsManager.Instance.Subscribe(this, "Aim", ControlsManager.ControlEvent.OnPressed, AimControlPressed);
        ControlsManager.Instance.Subscribe(this, "Aim", ControlsManager.ControlEvent.OnReleased, AimControlReleased);
        
        ControlsManager.Instance.SubscribeAxis(this, "AimAxis", AimControlAxis);
    }

    public bool IsAimPressed { get; set; }

    void AimControlPressed()
    {
        IsAimPressed = true;
        aimingBehaviour.AimState(true);
    }

    void AimControlReleased()
    {
        IsAimPressed = false;
        aimingBehaviour.AimState(false);
        aimingBehaviour.CanAim = true;
    }

    void AimControlAxis(Vector2 _value)
    {
        var val = _value.x;
        IsAimPressed = val >= 0.1f;
        aimingBehaviour.AimState(IsAimPressed);
    }
    
    private void OnDestroy()
    {
        ControlsManager.Instance.UnsubscribeAll(this);
    }
}
