using System;
using System.Collections;
using Controller.Core_scripts;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Controller.Controllers
{
    public class StickToGround : MonoBehaviour
    {
        [SerializeField, FoldoutGroup("Controller Settings")] 
        private float smoothSpeed = 10;
    
        [FoldoutGroup("Endurance"), SerializeField] private float maxFreeAngle;
        [FoldoutGroup("Endurance"), SerializeField] private float anglePrecision;
        [FoldoutGroup("Endurance"), SerializeField] private float increaseRate;
        [FoldoutGroup("Endurance"), SerializeField] private int increaseAmount;
        [FoldoutGroup("Endurance"), SerializeField] private float decreaseRate;
        [FoldoutGroup("Endurance"), SerializeField] private int decreaseAmount;
    
        [SerializeField, FoldoutGroup("Events")] 
        private UnityEvent<int> OnChangeEndurance;
    
        [ShowInInspector]
        private float angle;

        private Mover mover;
        private Rigidbody rb;
        private bool grounded;
        private bool prevGrounded;
        private bool enabled = true;
        private bool unstick;
        private void Start()
        {
            TryGetComponent(out mover);
            TryGetComponent(out rb);
        }

        private void FixedUpdate()
        {
            prevGrounded = grounded;
            grounded = mover.IsGrounded();
            angle = Quaternion.Angle(Quaternion.identity, Quaternion.Euler(rb.rotation.eulerAngles.x, 0, rb.rotation.eulerAngles.z));
            if (grounded && !prevGrounded && unstick) unstick = false;
            if (!enabled) return;
            if (grounded && !unstick)
            {
                var normal = mover.GetGroundNormal();
                var forward = Vector3.Cross(transform.right, normal);
                var upRot = Quaternion.LookRotation(forward, normal);
                rb.MoveRotation(Quaternion.Lerp(rb.rotation, upRot, smoothSpeed * Time.deltaTime));
                CheckEffort();
            }
            else
            {
                rb.MoveRotation(Quaternion.Lerp(rb.rotation, Quaternion.Euler(0, rb.rotation.eulerAngles.y, 0), smoothSpeed * Time.deltaTime));
            }
        
        }

        private void CheckEffort()
        {
            if (decreaseRoutine != null)
            {
                if (angle > (maxFreeAngle - anglePrecision)) ;
                else
                {
                    StopCoroutine(decreaseRoutine);
                    decreaseRoutine = null;
                }
            }
            else
            {
                if (angle > maxFreeAngle)
                {
                    if (increaseRoutine != null)
                    {
                        StopCoroutine(increaseRoutine);
                        increaseRoutine = null;
                    }
                    decreaseRoutine = StartCoroutine(Timer(decreaseRate, () =>
                    {
                        OnChangeEndurance.Invoke(decreaseAmount);
                        decreaseRoutine = null;
                    }));
                }
                else
                {
                    if (increaseRoutine != null) ;
                    else increaseRoutine = StartCoroutine(Timer(increaseRate, () =>
                    {
                        OnChangeEndurance.Invoke(increaseAmount);
                        increaseRoutine = null;
                    }));
                }
            }
        }

        public void Unstick()
        {
            unstick = true;
        }

        private Coroutine decreaseRoutine;
        private Coroutine increaseRoutine;

        private IEnumerator Timer(float duration, Action onComplete)
        {
            yield return new WaitForSeconds(duration);
            onComplete.Invoke();
        }
    }
}
