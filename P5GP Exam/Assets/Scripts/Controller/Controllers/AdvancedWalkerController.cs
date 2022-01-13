using System;
using System.Collections;
using Controller.Core_scripts;
using Controller.Helper;
using Controller.Input.Character;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Controller.Controllers
{
	//Advanced walker controller script;
	//This controller is used as a basis for other controller types ('SidescrollerController');
	//Custom movement input can be implemented by creating a new script that inherits 'AdvancedWalkerController' and overriding the 'CalculateMovementDirection' function;
	[RequireComponent(typeof(WalkerControls))]
	public class AdvancedWalkerController : Controller {

		#region References
		//References to attached components;
		protected Transform tr;
		protected Mover mover;
		protected WalkerControls characterInput; public WalkerControls GetControls() { return characterInput; }
		protected CeilingDetector ceilingDetector;

		#endregion

		#region Parameters
		#region Movement
		[FoldoutGroup("Movement"), HideInEditorMode, NonSerialized]
		public float movementSpeed = 4f;
		[FoldoutGroup("Movement")]
		public float walkSpeed = 4f;
		[FoldoutGroup("Movement")]
		public float slowSpeed = 2.5f;
		[FoldoutGroup("Movement")]
		public float slowLerpDuration = 0.35f;
		[FoldoutGroup("Movement")] 
		public AnimationCurve slowCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
		[FoldoutGroup("Movement")] //'GroundFriction' determines how fast the controller loses its momentum while grounded;
		public float groundFriction = 100f;
		[FoldoutGroup("Movement")] //Amount of downward gravity;
		public float gravity = 30f;
		[FoldoutGroup("Movement"), Tooltip("How fast the character will slide down steep slopes.")]
		public float slideGravity = 5f;
		[FoldoutGroup("Movement")] //Acceptable slope angle limit;
		public float slopeLimit = 80f;
		[FoldoutGroup("Movement"), Tooltip("Whether to calculate and apply momentum relative to the controller's transform.")]
		public bool useLocalMomentum = false;
		#endregion

		#region Sprint
		public bool sprintActive;
		[FoldoutGroup("Sprint"), ShowIf("sprintActive")] 
		public float sprintSpeed = 12f;
		[FoldoutGroup("Sprint"), ShowIf("sprintActive")] 
		public float sprintLerpDuration = 0.35f;
		[FoldoutGroup("Sprint"), ShowIf("sprintActive")] 
		public AnimationCurve sprintCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
		#endregion
		
		#region Jump
		public bool jumpActive;

		[FoldoutGroup("Jump"), ShowIf("jumpActive"), Range(0f, 360f)] //'Aircontrol' determines to what degree the player is able to move while in the air;
		public float minJumpAngle = 35f;
		[FoldoutGroup("Jump"), ShowIf("jumpActive"), Range(0f, 1f)] //'Aircontrol' determines to what degree the player is able to move while in the air;
		public float airControl = 0.4f;
		[FoldoutGroup("Jump"), ShowIf("jumpActive")] //Jump speed;
		public float jumpSpeed = 10f;
		[FoldoutGroup("Jump"), ShowIf("jumpActive")] //Jump duration variables;
		public float jumpDuration = 0.2f;
		[FoldoutGroup("Jump"), ShowIf("jumpActive")] //'AirFriction' determines how fast the controller loses its momentum while in the air;
		public float airFriction = 0.5f;
		[FoldoutGroup("Jump"), ShowIf("jumpActive")]
		public bool bypassJumpAngle;
		#endregion
		#endregion

		#region Runtime Variables
		#region Movement
		//Movement speed;
		public float CurrentMovementSpeed { get; set; }
		
		//Current momentum;
		protected Vector3 momentum = Vector3.zero;

		//Saved velocity from last frame;
		Vector3 savedVelocity = Vector3.zero;

		//Saved horizontal movement velocity from last frame;
		Vector3 savedMovementVelocity = Vector3.zero;

		public bool slowed;
		
		#endregion

		#region Sprint
		public bool CanSprint { get; set; } = true;	
		private bool sprinting;
		private Coroutine sprintCoroutine;

		#endregion
		
		#region Jump
		[FoldoutGroup("Debug"), ShowInInspector, ReadOnly, ShowIf("jumpActive")] //Jump speed;
		private bool canJump;
		bool jumpInputIsLocked = false;
		bool jumpKeyWasPressed = false;
		bool jumpKeyWasLetGo = false;
		bool jumpKeyIsPressed = false;
		
		float currentJumpStartTime = 0f;
		#endregion
		#endregion

		#region Enums
		//Enum describing basic controller states; 
		public enum ControllerState
		{
			Grounded,
			Sliding,
			Falling,
			Rising,
			Jumping
		}
		#endregion

		
		protected ControllerState currentControllerState = ControllerState.Falling;

		[Tooltip("Optional camera transform used for calculating movement direction. If assigned, character movement will take camera view into account.")]
		public Transform cameraTransform;
		
		//Get references to all necessary components;
		void Awake () {
			mover = GetComponent<Mover>();
			tr = transform;
			characterInput = GetComponent<WalkerControls>();
			ceilingDetector = GetComponent<CeilingDetector>();

			CurrentMovementSpeed = movementSpeed = walkSpeed;
			
			if(characterInput == null)
				Debug.LogWarning("No character input script has been attached to this gameobject", this.gameObject);

			Setup();
		}

		//This function is called right after Awake(); It can be overridden by inheriting scripts;
		protected virtual void Setup()
		{

		}

		void Update()
		{
			HandleJumpKeyInput();
		}

        //Handle jump booleans for later use in FixedUpdate;
        void HandleJumpKeyInput()
        {
			if (!jumpActive) return;

            bool _newJumpKeyPressedState = IsJumpKeyPressed();

            if (jumpKeyIsPressed == false && _newJumpKeyPressedState == true)
                jumpKeyWasPressed = true;

            if (jumpKeyIsPressed == true && _newJumpKeyPressedState == false)
            {
                jumpKeyWasLetGo = true;
                jumpInputIsLocked = false;
            }

            jumpKeyIsPressed = _newJumpKeyPressedState;
        }

        void FixedUpdate()
		{
			ControllerUpdate();
		}

		//Update controller;
		//This function must be called every fixed update, in order for the controller to work correctly;
		void ControllerUpdate()
		{
			//Check if mover is grounded;
			mover.CheckForGround();

			if (bypassJumpAngle) canJump = IsGrounded();
			else canJump = IsGrounded() && Mathf.Abs(Vector3.Angle(mover.GetGroundNormal(), Vector3.up)) >= minJumpAngle;

			//Determine controller state;
			currentControllerState = DetermineControllerState();

			//Apply friction and gravity to 'momentum';
			HandleMomentum();

			//Check if the player has initiated a jump;
			HandleJumping();

			HandleSprinting();

			//Calculate movement velocity;
			Vector3 _velocity = CalculateMovementVelocity();

			//If local momentum is used, transform momentum into world space first;
			Vector3 _worldMomentum = momentum;
			if(useLocalMomentum)
				_worldMomentum = tr.localToWorldMatrix * momentum;

			//Add current momentum to velocity;
			_velocity += _worldMomentum;
			
			//If player is grounded or sliding on a slope, extend mover's sensor range;
			//This enables the player to walk up/down stairs and slopes without losing ground contact;
			mover.SetExtendSensorRange(IsGrounded());

			//Set mover velocity;		
			mover.SetVelocity(_velocity);

			//Store velocity for next frame;
			savedVelocity = _velocity;
			savedMovementVelocity = _velocity - _worldMomentum;

			//Reset jump key booleans;
			jumpKeyWasLetGo = false;
			jumpKeyWasPressed = false;

			//Reset ceiling detector, if one was attached to this gameobject;
			if(ceilingDetector != null)
				ceilingDetector.ResetFlags();
		}

		//Calculate and return movement direction based on player input;
		//This function can be overridden by inheriting scripts to implement different player controls;
		protected virtual Vector3 CalculateMovementDirection()
		{
			//If no character input script is attached to this object, return;
			if(characterInput == null)
				return Vector3.zero;

			Vector3 _velocity = Vector3.zero;

			//If no camera transform has been assigned, use the character's transform axes to calculate the movement direction;
			if(cameraTransform == null)
			{
				_velocity += tr.right * characterInput.HorizontalInput;
				_velocity += tr.forward * characterInput.VerticalInput;
			}
			else
			{
				//If a camera transform has been assigned, use the assigned transform's axes for movement direction;
				//Project movement direction so movement stays parallel to the ground;
				_velocity += Vector3.ProjectOnPlane(cameraTransform.right, tr.up).normalized * characterInput.HorizontalInput;
				_velocity += Vector3.ProjectOnPlane(cameraTransform.forward, tr.up).normalized * characterInput.VerticalInput;
			}

			//If necessary, clamp movement vector to magnitude of 1f;
			if(_velocity.magnitude > 1f)
				_velocity.Normalize();

			return _velocity;
		}

		//Calculate and return movement velocity based on player input, controller state, ground normal [...];
		protected virtual Vector3 CalculateMovementVelocity()
		{
			//Calculate (normalized) movement direction;
			Vector3 _velocity = CalculateMovementDirection();

			//Save movement direction for later;
			Vector3 _velocityDirection = _velocity;

			//Multiply (normalized) velocity with movement speed;
			_velocity *= CurrentMovementSpeed;

			//If controller is not grounded, multiply movement velocity with 'airControl';
			if(!(currentControllerState == ControllerState.Grounded))
				_velocity = _velocityDirection * CurrentMovementSpeed * airControl;

			return _velocity;
		}

		//Returns 'true' if the player presses the jump key;
		protected virtual bool IsJumpKeyPressed()
		{
			//If no character input script is attached to this object, return;
			if(characterInput == null)
				return false;

			return characterInput.IsJumpPressed;
		}

		//Determine current controller state based on current momentum and whether the controller is grounded (or not);
		//Handle state transitions;
		ControllerState DetermineControllerState()
		{
			//Check if vertical momentum is pointing upwards;
			bool _isRising = IsRisingOrFalling() && (VectorMath.GetDotProduct(GetMomentum(), tr.up) > 0f);
			//Check if controller is sliding;
			bool _isSliding = mover.IsGrounded() && IsGroundTooSteep();
			
			//Grounded;
			if(currentControllerState == ControllerState.Grounded)
			{
				if(_isRising){
					OnGroundContactLost();
					return ControllerState.Rising;
				}
				if(!mover.IsGrounded()){
					OnGroundContactLost();
					return ControllerState.Falling;
				}
				if(_isSliding){
					return ControllerState.Sliding;
				}
				return ControllerState.Grounded;
			}

			//Falling;
			if(currentControllerState == ControllerState.Falling)
			{
				if(_isRising){
					return ControllerState.Rising;
				}
				if(mover.IsGrounded() && !_isSliding){
					OnGroundContactRegained(momentum);
					return ControllerState.Grounded;
				}
				if(_isSliding){
					OnGroundContactRegained(momentum);
					return ControllerState.Sliding;
				}
				return ControllerState.Falling;
			}
			
			//Sliding;
			if(currentControllerState == ControllerState.Sliding)
			{	
				if(_isRising){
					OnGroundContactLost();
					return ControllerState.Rising;
				}
				if(!mover.IsGrounded()){
					return ControllerState.Falling;
				}
				if(mover.IsGrounded() && !_isSliding){
					OnGroundContactRegained(momentum);
					return ControllerState.Grounded;
				}
				return ControllerState.Sliding;
			}

			//Rising;
			if(currentControllerState == ControllerState.Rising)
			{
				if(!_isRising){
					if(mover.IsGrounded() && !_isSliding){
						OnGroundContactRegained(momentum);
						return ControllerState.Grounded;
					}
					if(_isSliding){
						return ControllerState.Sliding;
					}
					if(!mover.IsGrounded()){
						return ControllerState.Falling;
					}
				}

				//If a ceiling detector has been attached to this gameobject, check for ceiling hits;
				if(ceilingDetector != null)
				{
					if(ceilingDetector.HitCeiling())
					{
						OnCeilingContact();
						return ControllerState.Falling;
					}
				}
				return ControllerState.Rising;
			}

			//Jumping;
			if(currentControllerState == ControllerState.Jumping)
			{
				//Check for jump timeout;
				if((Time.time - currentJumpStartTime) > jumpDuration)
					return ControllerState.Rising;

				//Check if jump key was let go;
				if(jumpKeyWasLetGo)
					return ControllerState.Rising;

				//If a ceiling detector has been attached to this gameobject, check for ceiling hits;
				if(ceilingDetector != null)
				{
					if(ceilingDetector.HitCeiling())
					{
						OnCeilingContact();
						return ControllerState.Falling;
					}
				}
				return ControllerState.Jumping;
			}
			
			return ControllerState.Falling;
		}

        //Check if player has initiated a jump;
        void HandleJumping()
        {
            if (currentControllerState == ControllerState.Grounded && canJump)
            {
                if ((jumpKeyIsPressed == true || jumpKeyWasPressed) && !jumpInputIsLocked)
                {
                    //Call events;
                    OnGroundContactLost();
                    OnJumpStart();

                    currentControllerState = ControllerState.Jumping;
                }
            }
        }

        void HandleSprinting()
        {
	        if (sprintActive == false) return;
	        
	        if (sprinting)
	        {
		        if (!CanSprint || slowed)
		        {
			        if(sprintCoroutine != null) StopCoroutine(sprintCoroutine);
			        CurrentMovementSpeed = movementSpeed;
			        sprinting = false;
		        }
		        else if (!characterInput.IsSprintPressed || currentControllerState != ControllerState.Grounded)
		        {
			        if(sprintCoroutine != null) StopCoroutine(sprintCoroutine);
			        sprinting = false;
			        sprintCoroutine = StartCoroutine(SprintRoutine());
		        }
	        }
	        else if (CanSprint && !slowed && currentControllerState == ControllerState.Grounded && characterInput.IsSprintPressed)
	        {
		        if(sprintCoroutine != null) StopCoroutine(sprintCoroutine);
		        sprinting = true;
		        sprintCoroutine = StartCoroutine(SprintRoutine());
	        }
        }

        //Apply friction to both vertical and horizontal momentum based on 'friction' and 'gravity';
        //Handle sliding down steep slopes;
        void HandleMomentum()
		{
			//If local momentum is used, transform momentum into world coordinates first;
			if(useLocalMomentum)
				momentum = tr.localToWorldMatrix * momentum;

			Vector3 _verticalMomentum = Vector3.zero;
			Vector3 _horizontalMomentum = Vector3.zero;

			//Split momentum into vertical and horizontal components;
			if(momentum != Vector3.zero)
			{
				_verticalMomentum = VectorMath.ExtractDotVector(momentum, tr.up);
				_horizontalMomentum = momentum - _verticalMomentum;
			}

			//Add gravity to vertical momentum;
			_verticalMomentum -= tr.up * gravity * Time.deltaTime;

			//Remove any downward force if the controller is grounded;
			if(currentControllerState == ControllerState.Grounded)
				_verticalMomentum = Vector3.zero;

			//Apply friction to horizontal momentum based on whether the controller is grounded;
			if(currentControllerState == ControllerState.Grounded)
				_horizontalMomentum = VectorMath.IncrementVectorTowardTargetVector(_horizontalMomentum, groundFriction, Time.deltaTime, Vector3.zero);
			else
				_horizontalMomentum = VectorMath.IncrementVectorTowardTargetVector(_horizontalMomentum, airFriction, Time.deltaTime, Vector3.zero); 

			//Add horizontal and vertical momentum back together;
			momentum = _horizontalMomentum + _verticalMomentum;

			//Project the current momentum onto the current ground normal if the controller is sliding down a slope;
			if(currentControllerState == ControllerState.Sliding)
			{
				momentum = Vector3.ProjectOnPlane(momentum, mover.GetGroundNormal());
			}

			//Apply slide gravity along ground normal, if controller is sliding;
			if(currentControllerState == ControllerState.Sliding)
			{
				Vector3 _slideDirection = Vector3.ProjectOnPlane(-tr.up, mover.GetGroundNormal()).normalized;
				momentum += _slideDirection * slideGravity * Time.deltaTime;
			}

			//If controller is jumping, override vertical velocity with jumpSpeed;
			if(currentControllerState == ControllerState.Jumping)
			{
				momentum = VectorMath.RemoveDotVector(momentum, tr.up);
				momentum += tr.up * jumpSpeed;
			}

			if(useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;
		}

		//Events;

		//This function is called when the player has initiated a jump;
		void OnJumpStart()
		{
			//If local momentum is used, transform momentum into world coordinates first;
			if(useLocalMomentum)
				momentum = tr.localToWorldMatrix * momentum;

			//Add jump force to momentum;
			momentum += tr.up * jumpSpeed;

			//Set jump start time;
			currentJumpStartTime = Time.time;

            //Lock jump input until jump key is released again;
            jumpInputIsLocked = true;

            //Call event;
            if (OnJump != null)
				OnJump(momentum);

			if(useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;
		}

		//This function is called when the controller has lost ground contact, i.e. is either falling or rising, or generally in the air;
		void OnGroundContactLost()
		{
			//Calculate current velocity;
			//If velocity would exceed the controller's movement speed, decrease movement velocity appropriately;
			//This prevents unwanted accumulation of velocity;
			float _horizontalMomentumSpeed = VectorMath.RemoveDotVector(GetMomentum(), tr.up).magnitude;
			Vector3 _currentVelocity = GetMomentum() + Vector3.ClampMagnitude(savedMovementVelocity, Mathf.Clamp(CurrentMovementSpeed - _horizontalMomentumSpeed, 0f, CurrentMovementSpeed));

			//Calculate length and direction from '_currentVelocity';
			float _length = _currentVelocity.magnitude;
			
			//Calculate velocity direction;
			Vector3 _velocityDirection = Vector3.zero;
			if(_length != 0f)
				_velocityDirection = _currentVelocity/_length;

			//Subtract from '_length', based on 'movementSpeed' and 'airControl', check for overshooting;
			if(_length >= CurrentMovementSpeed * airControl)
				_length -= CurrentMovementSpeed * airControl;
			else
				_length = 0f;

			//If local momentum is used, transform momentum into world coordinates first;
			if(useLocalMomentum)
				momentum = tr.localToWorldMatrix * momentum;

			momentum = _velocityDirection * _length;

			if(useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;
		}

		//This function is called when the controller has landed on a surface after being in the air;
		void OnGroundContactRegained(Vector3 _collisionVelocity)
		{
			//Call 'OnLand' event;
			if(OnLand != null)
				OnLand(_collisionVelocity);
		}

		//This function is called when the controller has collided with a ceiling while jumping or moving upwards;
		void OnCeilingContact()
		{
			//If local momentum is used, transform momentum into world coordinates first;
			if(useLocalMomentum)
				momentum = tr.localToWorldMatrix * momentum;

			//Remove all vertical parts of momentum;
			momentum = VectorMath.RemoveDotVector(momentum, tr.up);

			if(useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;
		}

		//Helper functions;

		//Returns 'true' if vertical momentum is above a small threshold;
		private bool IsRisingOrFalling()
		{
			//Calculate current vertical momentum;
			Vector3 _verticalMomentum = VectorMath.ExtractDotVector(GetMomentum(), tr.up);

			//Setup threshold to check against;
			//For most applications, a value of '0.001f' is recommended;
			float _limit = 0.001f;

			//Return true if vertical momentum is above '_limit';
			return(_verticalMomentum.magnitude > _limit);
		}

		//Returns true if angle between controller and ground normal is too big (> slope limit), i.e. ground is too steep;
		private bool IsGroundTooSteep()
		{
			if(!mover.IsGrounded())
				return true;

			return (Vector3.Angle(mover.GetGroundNormal(), tr.up) > slopeLimit);
		}

		//Getters;

		//Get last frame's velocity;
		public override Vector3 GetVelocity ()
		{
			return savedVelocity;
		}

		//Get last frame's movement velocity (momentum is ignored);
		public override Vector3 GetMovementVelocity()
		{
			return savedMovementVelocity;
		}

		//Get current momentum;
		public Vector3 GetMomentum()
		{
			Vector3 _worldMomentum = momentum;
			if(useLocalMomentum)
				_worldMomentum = tr.localToWorldMatrix * momentum;

			return _worldMomentum;
		}

		//Returns 'true' if controller is grounded (or sliding down a slope);
		public override bool IsGrounded()
		{
			return(currentControllerState == ControllerState.Grounded || currentControllerState == ControllerState.Sliding);
		}

		//Returns 'true' if controller is sliding;
		public bool IsSliding()
		{
			return(currentControllerState == ControllerState.Sliding);
		}

		//Add momentum to controller;
		public void AddMomentum (Vector3 _momentum)
		{
			if(useLocalMomentum)
				momentum = tr.localToWorldMatrix * momentum;

			momentum += _momentum;	

			if(useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;
		}

		//Set controller momentum directly;
		public void SetMomentum(Vector3 _newMomentum)
		{
			if(useLocalMomentum)
				momentum = tr.worldToLocalMatrix * _newMomentum;
			else
				momentum = _newMomentum;
		}
		
		public void Slow(bool state)
		{
			if(slowCoroutine != null) StopCoroutine(slowCoroutine);
			slowed = state;
			slowCoroutine = StartCoroutine(SlowRoutine());
		}
		
		public IEnumerator SprintRoutine()
		{
			float progress = 0f;
			(float origin, float destination) speed = (CurrentMovementSpeed, sprinting ? sprintSpeed : walkSpeed);

			while (progress < 1)
			{
				Lerp(sprintCurve.Evaluate(progress));
				progress += Time.deltaTime / sprintLerpDuration;
				yield return null;
			}
			Lerp(1);

			void Lerp(float time)
			{
				CurrentMovementSpeed = Mathf.Lerp(speed.origin, speed.destination, time);
			}
		}

		private Coroutine slowCoroutine;
		public IEnumerator SlowRoutine()
		{
			float progress = 0f;
			(float origin, float destination) speed = (CurrentMovementSpeed, slowed ? slowSpeed : walkSpeed);

			while (progress < 1)
			{
				Lerp(slowCurve.Evaluate(progress));
				progress += Time.deltaTime / slowLerpDuration;
				yield return null;
			}
			Lerp(1);

			void Lerp(float time)
			{
				CurrentMovementSpeed = Mathf.Lerp(speed.origin, speed.destination, time);
			}
		}
	}
}
