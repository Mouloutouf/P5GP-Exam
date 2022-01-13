using System;
using System.Collections;
using System.Collections.Generic;
using Controller.Core_scripts;
using Controller.Controllers;
using Features.Revised_Controller;
using Sirenix.OdinInspector;
using UnityEngine;

public class DashBehaviour : MonoBehaviour
{
    public DashControls dashControls;
    private bool DashPressed => dashControls.IsDashPressed;
    
    [Title("References")]
    [Title("Scripts", horizontalLine: false)]
    public AdvancedWalkerController controller;
    public ClimbingController climbing;
    
    [Title("Components", horizontalLine: false)]
    public Rigidbody characterRigidbody;
    public Collider hornCollider;

    public Transform cameraTransform;
    public Transform modelTransform;
    
    [Title("Parameters")]
    public float dashForce;
    private float force => dashForce * characterRigidbody.mass;
    public float factor = 1f;

    public float dashDuration;
    public float timeBeforeNextDash = 0.1f;

    private Vector3 forward;
    
    public bool CanDash { get; set; } = true;
    private bool dashing;
    
    private void Start()
    {
        SetActiveHorn(false);
    }

    void Update()
    {
        // Debug
        displayForward = Vector3.ProjectOnPlane(cameraTransform.forward, modelTransform.up);
        
        if (CanDash && DashPressed && controller.IsGrounded() && dashing == false)
        {
            dashing = true;
            CanDash = false;

            SetActiveHorn(true);

            if (Vector3.Distance(Vector3.zero, characterRigidbody.velocity) >= 0.1f)
                forward = transform.forward;
            else
                forward = Vector3.ProjectOnPlane(cameraTransform.forward, modelTransform.up);
            forward = forward.normalized;

            var quaternion = Quaternion.LookRotation(forward, modelTransform.up);
            var rotation = quaternion.eulerAngles;
            rotation = new Vector3(0, rotation.y, 0);
            modelTransform.rotation = Quaternion.Euler(rotation);

            if (climbing != null) climbing.CanClimb = false;
            
            ControlsManager.Instance.SetControlLocked("Dash", true);
            
            StartCoroutine(Dash());
        }

        if (DashPressed == false)
        {
            CanDash = true;
        }
    }

    IEnumerator Dash()
    {
        for (float ft = 0f; ft <= dashDuration; ft += Time.deltaTime)
        {
            characterRigidbody.AddForce(forward * force * factor);

            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(timeBeforeNextDash);

        ControlsManager.Instance.SetControlLocked("Dash", false);

        if (climbing != null) climbing.CanClimb = true;
        
        dashing = false;
        SetActiveHorn(false);
    }

    private void SetActiveHorn(bool _value) { if (hornCollider != null) hornCollider.gameObject.SetActive(_value); }

    private Vector3 displayForward;
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(modelTransform.position, displayForward.normalized);
    }
}
