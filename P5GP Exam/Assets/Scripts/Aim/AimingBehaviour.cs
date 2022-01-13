using System;
using Controller.Camera;
using Controller.Controllers;
using System.Collections;
using System.Collections.Generic;
using Controller;
using Features.Revised_Controller;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class AimingBehaviour : SerializedMonoBehaviour
{
    #region References
    [FoldoutGroup("References"), SerializeField]
    private Transform cameraTarget;
    [FoldoutGroup("References"), SerializeField]
    private CameraController cameraController;
    [FoldoutGroup("References"), SerializeField]
    private AdvancedWalkerController movementController;
    [FoldoutGroup("References"), SerializeField]
    private GrappleBehaviour grapple;
    [FoldoutGroup("References"), SerializeField]
    private Image reticleImage;
    [FoldoutGroup("References"), SerializeField]
    private Transform modelRoot;
    [FoldoutGroup("References"), SerializeField]
    private TurnTowardControllerVelocity turnController;
    #endregion

    #region Parameters
    [Title("Aim Settings")]
    public Transform aimedCameraPosition;
    public float aimedCameraSpeed = 40;  public float AimedCameraSpeed { get; set; }
    public float aimedMovementSpeed = 2;
    [Range(0f, 90f)]
    public float aimedUpperVerticalLimit = 60f;
    public float lerpDuration = 0.3f;
    public AnimationCurve lerpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public List<(MonoBehaviour target, bool state)> toggleOnAim = new List<(MonoBehaviour target, bool state)>();

    #endregion

    #region Properties
    public bool CanAim { get; set; } = true;
    public Transform NormalCameraPosition => cameraController.cameraBasePosition;
    public float NormalCameraSpeed => cameraController.cameraSpeed;
    public float NormalMovementSpeed => movementController.walkSpeed;
    public float NormalUpperVerticalLimit => cameraController.upperVerticalLimit;
    #endregion

    #region Runtime Values
    [NonSerialized]
    public bool aiming;
    #endregion
    
    private void Start()
    {
        AimedCameraSpeed = aimedCameraSpeed;
        reticleImage.gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        turnController.enabled = movementController.IsGrounded() && !aiming;
    }

    public void AimState(bool aiming)
    {
        if (!CanAim) return;
        this.aiming = aiming;
        movementController.CanSprint = !aiming;
        if (grapple != null) grapple.IsGrappleAimed = aiming;

        if (aimLerp != null) StopCoroutine(aimLerp);
        aimLerp = StartCoroutine(AimLerp());

        foreach (var toggleable in toggleOnAim)
        {
            toggleable.target.enabled = aiming ? toggleable.state : !toggleable.state;
        }
    }

    private Coroutine aimLerp;
    IEnumerator AimLerp()
    {
        float progress = 0f;
        (Vector3 origin, Vector3 destination) pos = (cameraTarget.localPosition,
            aiming ? aimedCameraPosition.localPosition : NormalCameraPosition.localPosition);
        (float origin, float destination) cameraSpeed = (cameraController.CurrentCameraSpeed,
            aiming ? AimedCameraSpeed : NormalCameraSpeed);
        (float origin, float destination) movementSpeed = (movementController.CurrentMovementSpeed,
            aiming ? aimedMovementSpeed : NormalMovementSpeed);
        (float origin, float destination) verticalLimit = (cameraController.CurrentUpperVerticalLimit,
            aiming ? aimedUpperVerticalLimit : NormalUpperVerticalLimit);

        while (progress < 1)
        {
            Lerp(lerpCurve.Evaluate(progress));
            progress += Time.deltaTime / lerpDuration;
            yield return null;
        }
        Lerp(1);
        
        reticleImage.gameObject.SetActive(aiming);
        ControlsManager.Instance.SetControlsEnabled(!aiming, "Jump", "Dash");

        void Lerp(float time)
        {
            cameraTarget.localPosition = Vector3.Lerp(pos.origin, pos.destination, time);
            cameraController.CurrentCameraSpeed = Mathf.Lerp(cameraSpeed.origin, cameraSpeed.destination, time);
            movementController.CurrentMovementSpeed = Mathf.Lerp(movementSpeed.origin, movementSpeed.destination, time);
            cameraController.CurrentUpperVerticalLimit = Mathf.Lerp(verticalLimit.origin, verticalLimit.destination, time);
        }
    }
}
