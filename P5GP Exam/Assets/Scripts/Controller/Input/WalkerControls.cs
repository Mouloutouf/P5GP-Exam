using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Features.Revised_Controller;

namespace Controller.Input.Character
{
    public class WalkerControls : MonoBehaviour
    {
	    public bool Freezed { get; private set; }
	    public bool IsJumpPressed
	    {
		    get => !Freezed && jumpPressed;
		    set => jumpPressed = value;
	    }

	    private bool jumpPressed;

	    public bool IsSprintPressed
	    {
		    get => !Freezed && sprintPressed;
		    set => sprintPressed = value;
	    }
		private bool sprintPressed;

		public float HorizontalInput
		{
			get => Freezed ? 0 : horizontalInput;
			set => horizontalInput = value;
		}
		private float horizontalInput;

		public float VerticalInput
		{
			get => Freezed ? 0 : verticalInput;
			set => verticalInput = value;
		}
		private float verticalInput;

		private void Start()
		{
			ControlsManager.Instance.Subscribe(this, "Jump", ControlsManager.ControlEvent.OnPressed, JumpControlPressed);
			ControlsManager.Instance.Subscribe(this, "Jump", ControlsManager.ControlEvent.OnReleased, JumpControlReleased);

			ControlsManager.Instance.Subscribe(this, "Sprint", ControlsManager.ControlEvent.OnPressed, SprintControlPressed);
			ControlsManager.Instance.Subscribe(this, "Sprint", ControlsManager.ControlEvent.OnReleased, SprintControlReleased);

			ControlsManager.Instance.SubscribeAxis(this, "Movement", MovementControlAxis);
		}

		public void Freeze(bool state)
		{
			Freezed = state;
		}

		public void MovementControlAxis(Vector2 _value)
		{
			HorizontalInput = _value.x;
			VerticalInput = _value.y;
		}
		
		public void JumpControlPressed() => IsJumpPressed = true;
		public void JumpControlReleased() => IsJumpPressed = false;

		public void SprintControlPressed() => IsSprintPressed = true;
		public void SprintControlReleased() => IsSprintPressed = false;
		private void OnDestroy()
		{
			ControlsManager.Instance.UnsubscribeAll(this);
		}
	}
}
