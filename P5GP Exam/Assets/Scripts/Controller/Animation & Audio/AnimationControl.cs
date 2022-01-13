using Controller.Helper;
using UnityEngine;

namespace Controller
{
	//This script controls the character's animation by passing velocity values and other information ('isGrounded') to an animator component;
	public class AnimationControl : MonoBehaviour {

		Controllers.Controller controller;
		Animator animator;
		Transform animatorTransform;
		Transform tr;

		//Whether the character is using the strafing blend tree;
		public bool useStrafeAnimations = false;

		//Velocity threshold for landing animation;
		//Animation will only be triggered if downward velocity exceeds this threshold;
		public float landVelocityThreshold = 5f;

		private float smoothingFactor = 40f;
		Vector3 oldMovementVelocity = Vector3.zero;

		//Setup;
		void Awake()
		{
			controller = GetComponent<Controllers.Controller>();
			animator = GetComponentInChildren<Animator>();
			if (animator != null)
				animatorTransform = animator.transform;

			tr = transform;
		}

		//OnEnable;
		void OnEnable()
		{
			//Connect events to controller events;
			controller.OnLand += OnLand;
			controller.OnJump += OnJump;
			controller.OnThrow += OnThrow;
			controller.OnDash += OnDash;
		}

		//OnDisable;
		void OnDisable()
		{
			//Disconnect events to prevent calls to disabled gameobjects;
			controller.OnLand -= OnLand;
			controller.OnJump -= OnJump;
			controller.OnThrow -= OnThrow;
			controller.OnDash -= OnDash;
		}

		//Update;
		void Update()
		{
			if (animator == null) return;
			
			//Get controller velocity;
			Vector3 _velocity = controller.GetVelocity();

			//Split up velocity;
			Vector3 _horizontalVelocity = VectorMath.RemoveDotVector(_velocity, tr.up);
			Vector3 _verticalVelocity = _velocity - _horizontalVelocity;

			//Smooth horizontal velocity for fluid animation;
			_horizontalVelocity = Vector3.Lerp(oldMovementVelocity, _horizontalVelocity, smoothingFactor * Time.deltaTime);
			oldMovementVelocity = _horizontalVelocity;

			animator.SetFloat("VerticalSpeed", _verticalVelocity.magnitude * VectorMath.GetDotProduct(_verticalVelocity.normalized, tr.up));
			animator.SetFloat("HorizontalSpeed", _horizontalVelocity.magnitude);

			//If animator is strafing, split up horizontal velocity;
			if (useStrafeAnimations)
			{
				Vector3 _localVelocity = animatorTransform.InverseTransformVector(_horizontalVelocity);
				animator.SetFloat("ForwardSpeed", _localVelocity.z);
				animator.SetFloat("StrafeSpeed", _localVelocity.x);
			}

			//Pass values to animator;
			animator.SetBool("IsGrounded", controller.IsGrounded());
			animator.SetBool("IsStrafing", useStrafeAnimations);
		}

		void OnLand(Vector3 _v)
		{
			if (animator == null) return;
			
			//Only trigger animation if downward velocity exceeds threshold;
			if (VectorMath.GetDotProduct(_v, tr.up) > -landVelocityThreshold)
				return;

			animator.SetTrigger("OnLand");
		}

		void OnJump(Vector3 _v)
		{
			if (animator == null) return;
			animator.SetTrigger("OnJump");
		}

		void OnThrow(Vector3 _v)
		{
			if (animator == null) return;
			animator.SetTrigger("OnThrow");
		}

		void OnDash(Vector3 _v)
		{
			if (animator == null) return;
			animator.SetTrigger("OnDash");
		}
	}
}
