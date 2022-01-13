using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Features.Revised_Controller
{
    [CreateAssetMenu(fileName = "Controls", menuName = "Settings/Controls", order = 0)]
    public class Controls : ScriptableObject
    {
        public bool ControllerMode;
        public List<InputAction> buttonInputs;
        public List<InputAxis> axisInputs;

        [NonSerialized] public Dictionary<string, InputAction> buttons;
        [NonSerialized] public Dictionary<string, InputAxis> axises;
        
        public void Initialise()
        {
            buttons = new Dictionary<string, InputAction>();
            axises = new Dictionary<string, InputAxis>();
            foreach (var button in buttonInputs)
            {
                buttons.Add(button.Name, button);
                button.OnInputPressed = delegate {  };
                button.OnInputReleased = delegate {  };
                button.Enabled = true;
            }
            
            foreach (var axis in axisInputs)
            {
                axises.Add(axis.Name, axis);
                axis.OnValueChanged = delegate {  };
                axis.Enabled = true;
            }
        }

        public bool HasButton(string name)
        {
            return buttons.ContainsKey(name);
        }
        public bool HasAxis(string name)
        {
            return axises.ContainsKey(name);
        }

        public bool GetButton(string name, out InputAction button)
        {
            button = null;
            if (buttons.ContainsKey(name))
            {
                button = buttons[name];
                return true;
            }

            return false;
        }

        public bool GetAxis(string name, out InputAxis axis)
        {
            axis = null;
            if (axises.ContainsKey(name))
            {
                axis = axises[name];
                return true;
            }

            return false;
        }

        #region Events
        public void SubscribeToPressed(Action onPressed, string buttonName)
        {
            buttons[buttonName].OnInputPressed += onPressed;
        }
        public void UnsubscribeToPressed(Action onPressed, string buttonName)
        {
            buttons[buttonName].OnInputPressed -= onPressed;
        }
        public void SubscribeToReleased(Action<float> onReleased, string buttonName)
        {
            buttons[buttonName].OnInputReleased += onReleased;
        }
        public void UnsubscribeToReleased(Action<float> onReleased, string buttonName)
        {
            buttons[buttonName].OnInputReleased -= onReleased;
        }
        public void SubscribeToReleased(Action onReleased, string buttonName)
        {
            buttons[buttonName].OnInputReleased += (f) => onReleased.Invoke();
        }
        public void UnsubscribeToReleased(Action onReleased, string buttonName)
        {
            buttons[buttonName].OnInputReleased -= (f) => onReleased.Invoke();
        }

        public void SubscribeToAxis(Action<Vector2> onValueChanged, string axisName)
        {
            axises[axisName].OnValueChanged += onValueChanged;
        }
        public void UnsubscribeToAxis(Action<Vector2> onValueChanged, string axisName)
        {
            axises[axisName].OnValueChanged -= onValueChanged;
        }
        #endregion
        
        public void CheckInputs()
        {
            //ControllerMode = Input.GetJoystickNames().Length > 0;
            for (int i = 0; i < buttonInputs.Count; i++)
            {
                var input = buttonInputs[i];
                if (input.Enabled == false) continue;
                var value = ControllerMode ? Input.GetKey(input.JoystickControl) : Input.GetKey(input.KeyControl);
                var valueChanged = value != input.State;
                input.State = value;
                if (valueChanged)
                {
                    if (value)
                    {
                        input.PressedTimecode = Time.time;
                        input.OnInputPressed.Invoke();
                    }
                    else
                    {
                        input.HeldDuration = Time.time - input.PressedTimecode;
                        input.OnInputReleased.Invoke(input.HeldDuration);
                        input.ResetTimecode();
                    }
                }
            }

            for (int i = 0; i < axisInputs.Count; i++)
            {
                var input = axisInputs[i];
                Vector2 value = Vector2.zero;
                
                if (input.Enabled == false) continue;
                
                if (ControllerMode)
                {
                    value.x = GetInputAxis(input.JoystickAxisHorizontal, input.useRawAxis);
                    if (!input.useSingleAxis) value.y = GetInputAxis(input.JoystickAxisVertical, input.useRawAxis);
                }
                else if (input.useAxisForKeyboard)
                {
                    value.x = GetInputAxis(input.KeyboardAxisHorizontal, input.useRawAxis);
                    if (!input.useSingleAxis) value.y = GetInputAxis(input.KeyboardAxisVertical, input.useRawAxis);
                }
                else
                {
                    value.x = GetKeyAxis(input.KeyCodeLeft, input.KeyCodeRight);
                    if (!input.useSingleAxis) value.y = GetKeyAxis(input.KeyCodeDown,input.KeyCodeUp);
                }

                var valueChanged = value != input.Value;
                input.Value = value;
                if (valueChanged) input.OnValueChanged.Invoke(value);
            }
        }

        public float GetInputAxis(string _axisName, bool _useRawAxis)
        {
            return _useRawAxis ? Input.GetAxisRaw(_axisName) : Input.GetAxis(_axisName);
        }

        public float GetKeyAxis(KeyCode _negativeKey, KeyCode _positiveKey)
        {
            return -Convert.ToInt32(Input.GetKey(_negativeKey)) + Convert.ToInt32(Input.GetKey(_positiveKey));
        }
    }

    [Serializable]
    public class InputAction
    {
        public string Name;
        [NonSerialized, HideInEditorMode, ShowInInspector]
        public bool State;

        public bool Enabled { get; set; } = true;
        
        public KeyCode JoystickControl;
        [FormerlySerializedAs("KeyCode")] public KeyCode KeyControl;

        [NonSerialized, HideInInspector]
        public float PressedTimecode;
        [NonSerialized, HideInInspector]
        public float HeldDuration;

        [NonSerialized]
        public Action OnInputPressed = delegate {  };
        [NonSerialized]
        public Action<float> OnInputReleased = delegate {  };

        public void ResetTimecode()
        {
            PressedTimecode = 0;
            HeldDuration = 0;
        }
    }

    [Serializable]
    public class InputAxis
    {
        [Title("Axis")]
        
        public string Name;
        
        public bool useSingleAxis;
        public bool useRawAxis;

        public bool Enabled { get; set; } = true;
        
        [NonSerialized, HideInEditorMode, ShowInInspector]
        public Vector2 Value;
        
        [Title("Joystick", horizontalLine: false)]
        
        public string JoystickAxisHorizontal;
        [HideIf("useSingleAxis")]
        public string JoystickAxisVertical;
        
        [Title("Keyboard", horizontalLine: false)]
        
        public bool useAxisForKeyboard;
        
        [HideIf("useAxisForKeyboard")]
        public KeyCode KeyCodeLeft;
        [HideIf("useAxisForKeyboard")]
        public KeyCode KeyCodeRight;  
        [HideIf("@useSingleAxis || useAxisForKeyboard")]
        public KeyCode KeyCodeUp;
        [HideIf("@useSingleAxis || useAxisForKeyboard")]
        public KeyCode KeyCodeDown;
        
        [ShowIf("useAxisForKeyboard")]
        public string KeyboardAxisHorizontal;
        [ShowIf("@!useSingleAxis && useAxisForKeyboard")]
        public string KeyboardAxisVertical;
        
        [NonSerialized]
        public Action<Vector2> OnValueChanged = delegate {  };
    }
}