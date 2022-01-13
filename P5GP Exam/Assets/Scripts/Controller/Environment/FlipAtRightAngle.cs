using Controller.Controllers;
using DG.Tweening;
using UnityEngine;

namespace Controller.Environment
{
	//This script flips any rigidbody (which also has a 'Controller' attached) that touches its trigger around a 90 degree angle;
	public class FlipAtRightAngle : MonoBehaviour {

		//Audiosource component which is played when switch is triggered;
		AudioSource audioSource;

		Transform tr;

		private bool busy;

		void Start()
		{
			//Get component references;
			tr = transform;
			audioSource = GetComponent<AudioSource>();
		}

		void OnTriggerEnter(Collider col)
		{
			if(busy) return;
			if(col.GetComponent<Controllers.Controller>() == null)
				return;
			busy = true;
			col.GetComponent<StickToGround>().enabled = false;
			SwitchDirection(tr.forward, col.GetComponent<Controllers.Controller>());
		}

		void SwitchDirection(Vector3 _newUpDirection, Controllers.Controller _controller)
		{
			float _angleThreshold = 0.001f;

			//Calculate angle;
			float _angleBetweenUpDirections = Vector3.Angle(_newUpDirection, _controller.transform.up);

			//If angle between new direction and current rigidbody rotation is too small, return;
			if(_angleBetweenUpDirections < _angleThreshold)
				return;

			//Play audio cue;
			//audioSource.Play();

			Transform _transform = _controller.transform;

			//Rotate gameobject;
			Quaternion _rotationDifference = Quaternion.FromToRotation(_transform.up, _newUpDirection);
			_transform.DORotateQuaternion(_rotationDifference * _transform.rotation, 0.65f).OnComplete(() =>
			{
				_controller.GetComponent<StickToGround>().enabled = true;
				busy = false;
			});
		}
	}
}