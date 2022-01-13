using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Controller;
using Controller.Controllers;
using Controller.Core_scripts;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Utilities.Geometry;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

[ExecuteAlways]
public class ClimbingController : MonoBehaviour
{
    #region References
    [FoldoutGroup("References"), SerializeField]
    private Transform modelRoot;
    private Collider col;
    private Rigidbody rb;
    private Mover mover;
    private AdvancedWalkerController controller;

    #endregion

    #region Parameters
    [FoldoutGroup("Controller Settings"), SerializeField]
    private LayerMask wallMasks;
    [FoldoutGroup("Controller Settings"), SerializeField]
    private float climbingRangeX = 0.5f;
    [FoldoutGroup("Controller Settings"), SerializeField]
    private float climbingRangeY = 0.5f;
    [FoldoutGroup("Controller Settings"), SerializeField]
    private float smoothing = 5f;
    
    [FoldoutGroup("Controller Settings"), SerializeField]
    private float identitySmoothing = 10f;

    /*
    [FoldoutGroup("Controller Settings"), SerializeField]
    private float groundRange = 0.35f;
    [FoldoutGroup("Endurance Settings"), SerializeField]
    private bool enduranceActive;
    [FoldoutGroup("Endurance Settings"), SerializeField]
    private float enduranceLossDuration = 3f;
    [FoldoutGroup("Endurance Settings"), SerializeField]
    private int enduranceLossTicks = 6;
    [FoldoutGroup("Endurance Settings"), SerializeField]
    private float enduranceHealDuration = 3f;
    [FoldoutGroup("Endurance Settings"), SerializeField]
    private int enduranceHealTicks = 6;
    [FoldoutGroup("Endurance Settings"), SerializeField]
    private float exhaustingAngle = 60;
    [FoldoutGroup("Endurance Settings"), SerializeField]
    private float jumpTimeBeforeGravity = 0.2f;*/
    #endregion
    #region Events
    /*
    [FoldoutGroup("Events"), SerializeField]
    private UnityEvent<float> onEnduranceChanged;*/
    #endregion
    
    #region Const
    //private const float offset = 0.15f;
    #endregion
    
    #region Runtime Values
    /*
    //private float rayLength => size.x + range + offset;
    [ShowInInspector, HideInEditorMode, BoxGroup("Debug")]
    private float endurance;
    [ShowInInspector, HideInEditorMode, BoxGroup("Debug")]
    private float enduranceTimer;
    [ShowInInspector, HideInEditorMode, BoxGroup("Debug")]
    private bool enduranceHealing;
    [ShowInInspector, HideInEditorMode, BoxGroup("Debug")]
    private float groundAngle;
    [ShowInInspector, HideInEditorMode, BoxGroup("Debug")]
    private bool unstick;
    private Vector3 extents;
    private Vector3 size;*/
    private Ellipse ellipse;
    
    private CollisionInfo infos;
    private bool climbKeyDown;
    
    private float jumpTimer;
    private Vector3 lastGroundPoint;
    private bool wasGrounded;
    public bool fall;

    public bool CanClimb { get; set; } = true;
    #endregion
    
    private void Awake()
    {
        TryGetComponent(out col);
        TryGetComponent(out rb);
        TryGetComponent(out mover);
        TryGetComponent(out controller);
        
        //extents = col.bounds.extents;
        //size = col.bounds.size;

        CanClimb = true;
    }

    private void FixedUpdate()
    {
        if (CanClimb == false) return;
        var grounded = controller.IsGrounded();


        if (fall)
        {
            RotateTowards(Vector3.up, identitySmoothing);
            fall = false;
            return;
            
            //var rot = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up));
        }
        
        ellipse = new Ellipse(climbingRangeX, climbingRangeY, Vector3.zero, UnityEngine.Animations.Axis.X);
        
        var velocity = controller.GetMovementVelocity().normalized;
        
        //rb.isKinematic = false;
        bool noHit = true;
        
        if (velocity != Vector3.zero)
        {
            var childRot = modelRoot.transform.rotation;
            var points = ellipse.GetPoints(Vector3.forward, 360, 20).Convert(p => transform.position+((childRot) * (Vector3)p)).ToList();
            for (int i = points.Count-1; i > 0; i--)
            {
                var point = points[i];
                var nextPoint = points[i-1];
                if(Physics.Linecast(point, nextPoint, out RaycastHit hitInfo, wallMasks, QueryTriggerInteraction.Ignore))
                {
                    //rb.isKinematic = true;
                    RotateTowards(hitInfo.normal, smoothing);
                    noHit = false;
                    break;
                }
                Debug.DrawLine(point, nextPoint, hitInfo.collider != null ? Color.red : Color.blue, Time.deltaTime, false);
            }
            
            if(noHit) RotateTowards(Vector3.up, identitySmoothing);
        }
        else
        {
            if (!grounded)
            {
                RotateTowards(Vector3.up, identitySmoothing);
            }
            else RotateTowards(mover.GetGroundNormal(), smoothing);
        }

    }
    
    private void RotateTowards(Vector3 up, float smoothing)
    {
        var upRot = Quaternion.FromToRotation(transform.up, up) * rb.rotation;
        rb.MoveRotation(Quaternion.Lerp(rb.rotation, upRot, smoothing * Time.deltaTime));
    }

    /*

    private void Start()
    {
        endurance = 1;

        UpdateEndurance(1);
    }

    private void Update()
    {
        climbKeyDown |= Input.GetKeyDown(KeyCode.LeftShift);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        DetectWalls();
    }

    private void DetectWalls()
    {
        infos = new CollisionInfo();
        infos.up = transform.up;
        infos.grounded = mover.IsGrounded();
        infos.groundNormal = mover.GetGroundNormal();
        infos.groundPoint = mover.GetGroundPoint();
        infos.direction = controller.GetMovementVelocity().normalized;
        infos.origin = transform.position - infos.direction * offset + transform.up;

        //Unstick from surface if endurance is at 0
        if (unstick)
        {
            if (infos.grounded && Vector3.Angle(Vector3.up, infos.groundNormal) < exhaustingAngle)
            {
                unstick = false;
            }
            else
            {
                var upRot = Quaternion.FromToRotation(transform.up, Vector3.up) * rb.rotation;
                rb.MoveRotation(Quaternion.Lerp(rb.rotation, upRot, identitySmoothing * Time.deltaTime));
                return;
            }
        }

        if (infos.grounded)
        {
            jumpTimer = 0;
            groundAngle = Vector3.Angle(infos.groundNormal, Vector3.up);

            if (groundAngle < exhaustingAngle)
            {
                if (!enduranceHealing)
                {
                    enduranceTimer = enduranceHealDuration / enduranceHealTicks;
                    enduranceHealing = true;
                }
            }
            else
            {
                if (endurance == 0) unstick = true;
                if (enduranceHealing)
                {
                    enduranceTimer = enduranceLossDuration / enduranceLossTicks; 
                    enduranceHealing = false;
                }
            }
        }
        
        if (enduranceHealing && endurance < 1 || !enduranceHealing && endurance > 0)
        {
            if(!enduranceHealing){ if(infos.direction != Vector3.zero) enduranceTimer -= Time.deltaTime;}
            else if(infos.grounded) enduranceTimer -= Time.deltaTime;
            if (enduranceTimer <= 0)
            {
                enduranceTimer = enduranceHealing ? enduranceHealDuration / enduranceHealTicks : enduranceLossDuration / enduranceLossTicks;
                UpdateEndurance(enduranceHealing ? 1f / enduranceHealTicks : -1f / enduranceLossTicks);
            }
        }

        var wrapping = false;
        var groundOrigin = transform.position - transform.up; 
        //Detect a Wall in range
        if (Physics.Raycast(infos.origin, infos.direction, out infos.hitInfo, rayLength, wallMasks) && infos.angle > 30 && (endurance != 0 || infos.angle < exhaustingAngle))
        {
            infos.hit = true;
            RotateTowards(infos.normal, identitySmoothing);
            Debug.DrawLine(infos.origin, infos.hitInfo.point, Color.blue, 0.1f);
        }
        else if (infos.grounded)
        {
            RotateTowards(infos.groundNormal, smoothing);
            lastGroundPoint = infos.groundPoint;
        }
        else if (wasGrounded && Physics.Raycast(groundOrigin, -infos.direction,
            out RaycastHit wrapHitInfo, rayLength, wallMasks))
        {
            RotateTowards(wrapHitInfo.normal, smoothing);
            Debug.DrawLine(groundOrigin, wrapHitInfo.point, Color.red, 0.1f);
            wrapping = true;
        }
        else if (Physics.Raycast(transform.position, -transform.up, out RaycastHit groundHitInfo, groundRange, wallMasks))
        {
            RotateTowards(groundHitInfo.normal, smoothing);
        }
        else
        {
            if (jumpTimer != 1) IncreasePercentage(ref jumpTimer, jumpTimeBeforeGravity);
            else RotateTowards(Vector3.up, identitySmoothing);
        }

        climbKeyDown = false;
        if (infos.grounded) wasGrounded = true;
        else if (wasGrounded && !wrapping) wasGrounded = false;
    }

    public void UpdateEndurance(float amount)
    {
        if (!enduranceActive) return;

        var prevEndurance = endurance;
        endurance = Mathf.Clamp(endurance + amount, 0, 1);
        if (prevEndurance != endurance)
        {
            onEnduranceChanged.Invoke(endurance);
        }
    }
    
    private void RotateTowards(Vector3 up, float smoothing)
    {
        var upRot = Quaternion.FromToRotation(transform.up, up) * rb.rotation;
        rb.MoveRotation(Quaternion.Lerp(rb.rotation, upRot, smoothing * Time.deltaTime));
    }
    private void IncreasePercentage(ref float value, float timeToMax)
    {
        value = Mathf.Clamp(value + Time.deltaTime / timeToMax, 0, 1);
    }
    */
    private struct CollisionInfo
    {
        public Vector3 up;
        public Vector3 origin;
        public Vector3 direction;
        public float distance => hitInfo.distance;
        public Vector3 point => hitInfo.point;
        public Vector3 normal => hitInfo.normal;
        public bool grounded;
        public Vector3 groundNormal;
        public Vector3 groundPoint;
        public float angle
        {
            get
            {
                if(_angle == null) _angle = Vector3.Angle(up, normal);
                return _angle.Value;
            }
        }
        private float? _angle;
        
        public Collider contact => hitInfo.collider;
        public bool hit;
        public RaycastHit hitInfo;
    }
}
