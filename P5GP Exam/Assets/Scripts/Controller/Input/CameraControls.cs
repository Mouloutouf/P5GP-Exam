using System;
using System.Collections;
using System.Collections.Generic;
using Features.Revised_Controller;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace Controller.Input.Camera
{
    public class CameraControls : MonoBehaviour
    {
        public bool switchSettings;
        
        [Title("Mouse Settings")]
        [HideIf("switchSettings")] public bool invertMouseHorizontalInput = false;
        [HideIf("switchSettings")] public bool invertMouseVerticalInput = true;

        [HideIf("switchSettings")] public float mouseSensitivity = 0.01f;

        [Title("Joystick Settings")]
        [ShowIf("switchSettings")] public bool invertJoystickHorizontalInput = false;
        [ShowIf("switchSettings")] public bool invertJoystickVerticalInput = false;
        
        [ShowIf("switchSettings")] public float joystickDeadZoneThreshold = 0.1f;
        
        private float HorizontalInput { get; set; }
        private float VerticalInput { get; set; }
        
        private bool controllerMode => ControlsManager.Instance.ControllerMode;
        
        private void Start()
        {
            ControlsManager.Instance.SubscribeAxis(this, "Camera", CameraControlAxis);
        }

        void CameraControlAxis(Vector2 _value)
        {
            HorizontalInput = _value.x;
            VerticalInput = _value.y;
        }

        public Vector2 GetCameraAxis()
        {
            if (controllerMode)
                return GetCameraJoystickAxis();
            // else
            return GetCameraMouseAxis();
            
            Vector2 axis;
            float horizontalAxis = HorizontalInput;
            float verticalAxis = VerticalInput;

            if (invertMouseHorizontalInput)
                horizontalAxis *= -1;
            if (invertMouseVerticalInput)
                verticalAxis *= -1;

            axis = new Vector2(horizontalAxis, verticalAxis);
            
            axis *= mouseSensitivity;
            return axis;
        }

        public Vector2 GetCameraMouseAxis()
        {
            Vector2 axis;
            float horizontalAxis = HorizontalInput;
            float verticalAxis = VerticalInput;
            
            // Invert Input
            if (invertMouseHorizontalInput)
                horizontalAxis *= -1;
            if (invertMouseVerticalInput)
                verticalAxis *= -1;

            axis = new Vector2(horizontalAxis, verticalAxis);
            
            // Since raw mouse input is already time-based, we need to correct for this before passing the input to the camera controller
            if (Time.timeScale > 0f)
            {
                axis /= Time.deltaTime;
                axis *= Time.timeScale;
            }
            else
                axis = Vector2.zero;
            
            // Apply mouse sensitivity
            axis *= mouseSensitivity;
            
            return axis;
        }
        public Vector2 GetCameraJoystickAxis()
        {
            Vector2 axis;
            float horizontalAxis = HorizontalInput;
            float verticalAxis = VerticalInput;
            
            // Invert Input
            if (invertJoystickHorizontalInput)
                horizontalAxis *= -1;
            if (invertJoystickVerticalInput)
                verticalAxis *= -1;
            
            axis = new Vector2(horizontalAxis, verticalAxis);
            
            //Set any input values below threshold to '0';
            if (Mathf.Abs(axis.x) < joystickDeadZoneThreshold && Mathf.Abs(axis.y) < joystickDeadZoneThreshold)
                axis = Vector2.zero;
            
            return axis;
        }
        
        private void OnDestroy()
        {
            ControlsManager.Instance.UnsubscribeAll(this);
        }

        #region Old Cam Controls
        // public float GetMouseCameraInput(InputAxis _axis)
        // {
        //     string axisInput = _axis == InputAxis.Vertical ? mouseVerticalAxis : mouseHorizontalAxis;
        //     bool invertInput = _axis == InputAxis.Vertical ? invertVerticalInput : invertHorizontalInput;
        //
        //     //Get raw mouse input;
        //     float _input = -UnityEngine.Input.GetAxisRaw(axisInput);
        //
        //     //Since raw mouse input is already time-based, we need to correct for this before passing the input to the camera controller;
        //     if (Time.timeScale > 0f)
        //     {
        //         _input /= Time.deltaTime;
        //         _input *= Time.timeScale;
        //     }
        //     else
        //         _input = 0f;
        //
        //     //Apply mouse sensitivity;
        //     _input *= mouseInputMultiplier;
        //
        //     //Invert input;
        //     if (invertInput)
        //         _input *= -1f;
        //
        //     return _input;
        // }
        //
        // public float GetJoystickCameraInput(InputAxis _axis)
        // {
        //     string axisInput = _axis == InputAxis.Vertical ? joystickVerticalAxis : joystickHorizontalAxis;
        //     bool invertInput = _axis == InputAxis.Vertical ? invertVerticalInput : invertHorizontalInput;
        //
        //     //Get input;
        //     float _input = UnityEngine.Input.GetAxisRaw(axisInput);
        //
        //     //Set any input values below threshold to '0';
        //     if (Mathf.Abs(_input) < deadZoneThreshold)
        //         _input = 0f;
        //
        //     //Handle inverted inputs;
        //     if (invertInput)
        //         _input *= (-1f);
        //     
        //     return _input;
        // }
        #endregion
    }
}