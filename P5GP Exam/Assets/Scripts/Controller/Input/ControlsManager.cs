using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Utilities.Singletons;

namespace Features.Revised_Controller
{
    public class ControlsManager : Singleton<ControlsManager>
    {
        [SerializeField] private Controls controls;

        [ShowInInspector, HideInEditorMode]
        private Dictionary<MonoBehaviour, List<SubscribeForm>>
            subscribeHistory = new Dictionary<MonoBehaviour, List<SubscribeForm>>();

        public bool ControllerMode => controls.ControllerMode;
        
        protected override void Awake()
        {
            base.Awake();
            if (controls == null) controls = Resources.Load<Controls>("Controls");
            controls.Initialise();
        }

        private void Update()
        {
            controls.CheckInputs();
        }

        public bool Subscribe(MonoBehaviour owner, string controlName, ControlEvent controlEvent, Action action)
        {
            if (controls.GetButton(controlName, out InputAction button))
            {
                if(!subscribeHistory.ContainsKey(owner)) subscribeHistory.Add(owner, new List<SubscribeForm>());
                switch (controlEvent)
                {
                    case ControlEvent.OnPressed :
                        button.OnInputPressed += action;
                        subscribeHistory[owner].Add(new SubscribeForm(button, action));
                        break;
                    case ControlEvent.OnReleased :
                        var heldAction = new Action<float>((f) => action.Invoke());
                        button.OnInputReleased += heldAction;
                        subscribeHistory[owner].Add(new SubscribeForm(button, heldAction));
                        break;
                    case ControlEvent.Unassigned :
                        break;
                }
                return true;
            }
            return false;
        }
        
        public bool SubscribeAxis(MonoBehaviour owner, string controlName, Action<Vector2> action)
        {
            if (controls.GetAxis(controlName, out InputAxis axis))
            {
                if(!subscribeHistory.ContainsKey(owner)) subscribeHistory.Add(owner, new List<SubscribeForm>());
                axis.OnValueChanged += action;
                subscribeHistory[owner].Add(new SubscribeForm(axis, action));
                return true;
            }

            return false;
        }

        public bool SubscribeHeld(MonoBehaviour owner, string controlName, Action<float> action)
        {
            if (controls.GetButton(controlName, out InputAction button))
            {
                if(!subscribeHistory.ContainsKey(owner)) subscribeHistory.Add(owner, new List<SubscribeForm>());
                button.OnInputReleased += action;
                subscribeHistory[owner].Add(new SubscribeForm(button, action));
                return true;
            }
            return false;
        }

        public bool Unsubscribe(MonoBehaviour owner, string controlName)
        {
            if (subscribeHistory.ContainsKey(owner))
            {
                var history = subscribeHistory[owner];
                List<SubscribeForm> matches = new List<SubscribeForm>();
                foreach (var form in history)
                {
                    if((form.axis != null && form.axis.Name == controlName) || (form.button != null && form.button.Name == controlName)) matches.Add(form);
                }

                foreach (var match in matches)
                {
                    switch (match.type)
                    {
                        case InputType.Button:
                            switch (match.eventType)
                            {
                                case ControlEvent.Unassigned:
                                    break;
                                case ControlEvent.OnPressed:
                                    match.button.OnInputPressed -= match.pressedAction;
                                    break;
                                case ControlEvent.OnReleased:
                                    match.button.OnInputReleased -= match.releasedAction;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                        case InputType.Axis:
                            match.axis.OnValueChanged -= match.changedAction;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    history.Remove(match);
                }
            }
            return false;
        }

        public bool UnsubscribeAll(MonoBehaviour owner)
        {
            if (subscribeHistory.ContainsKey(owner))
            {
                var history = subscribeHistory[owner];
                List<SubscribeForm> matches = new List<SubscribeForm>();
                foreach (var form in history)
                {
                    switch (form.type)
                    {
                        case InputType.Button:
                            switch (form.eventType)
                            {
                                case ControlEvent.Unassigned:
                                    break;
                                case ControlEvent.OnPressed:
                                    form.button.OnInputPressed -= form.pressedAction;
                                    break;
                                case ControlEvent.OnReleased:
                                    form.button.OnInputReleased -= form.releasedAction;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            break;
                        case InputType.Axis:
                            form.axis.OnValueChanged -= form.changedAction;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                subscribeHistory.Remove(owner);
                return true;
            }
            return false;
        }

        public void SetControlLocked(string controlName, bool locked)
        {
            for (int i = 0; i < controls.buttonInputs.Count; i++)
            {
                controls.buttonInputs[i].Enabled = !locked;
            }
            for (int i = 0; i < controls.axisInputs.Count; i++)
            {
                controls.axisInputs[i].Enabled = !locked;
            }
            if (controls.GetButton(controlName, out InputAction button))
            {
                button.Enabled = true;
            }
            else if (controls.GetAxis(controlName, out InputAxis axis))
            {
                axis.Enabled = true;
            }
        }
        public void SetAllControlsEnabled(bool enabled)
        {
            for (int i = 0; i < controls.buttonInputs.Count; i++)
            {
                controls.buttonInputs[i].Enabled = enabled;
            }
            for (int i = 0; i < controls.axisInputs.Count; i++)
            {
                controls.axisInputs[i].Enabled = enabled;
            }
        }
        public void SetControlsEnabled(bool enabled, params string[] controlsNames)
        {
            foreach (var controlName in controlsNames)
            {
                if (controls.GetButton(controlName, out InputAction button))
                {
                    button.Enabled = enabled;
                }
                else if (controls.GetAxis(controlName, out InputAxis axis))
                {
                    axis.Enabled = enabled;
                }
            }
        }
        
        #region Enums
        public enum ControlEvent
        {
            Unassigned,
            OnPressed,
            OnReleased,
        }
        public enum InputType
        {
            Button,
            Axis,
        }

        private struct SubscribeForm
        {
            public SubscribeForm(InputAction buttonInput, Action onPressed)
            {
                type = InputType.Button;
                button = buttonInput;
                eventType = ControlEvent.OnPressed;
                axis = null;
                pressedAction = onPressed;
                releasedAction = null;
                changedAction = null;
            }
            
            public SubscribeForm(InputAction buttonInput, Action<float> onReleased)
            {
                type = InputType.Button;
                button = buttonInput;
                eventType = ControlEvent.OnReleased;
                axis = null;
                pressedAction = null;
                releasedAction = onReleased;
                changedAction = null;
            }
            
            public SubscribeForm(InputAxis axisInput, Action<Vector2> onChanged)
            {
                type = InputType.Axis;
                button = null;
                eventType = ControlEvent.Unassigned;
                axis = axisInput;
                pressedAction = null;
                releasedAction = null;
                changedAction = onChanged;
            }
            public InputType type;
            public InputAction button;
            public InputAxis axis;
            public ControlEvent eventType;

            public Action pressedAction;
            public Action<float> releasedAction;
            public Action<Vector2> changedAction;
        }
        #endregion
    }
}