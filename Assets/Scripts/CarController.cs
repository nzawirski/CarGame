using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarController : MonoBehaviour
{
    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";

    private float horizontalInput;
    private float throttleInput;
    private float brakeInput;

    private float currentSteerAngle;
    private float currentBrakeForce;
    private bool isHandBrakeOn;

    public Transform centerOfMass;

    [SerializeField] private float motorForce;
    [SerializeField] private float brakeForce;
    [SerializeField] private float maxSteerAngle;

    [SerializeField] private WheelCollider frontLeftWheelCollider;
    [SerializeField] private WheelCollider frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider;
    [SerializeField] private WheelCollider rearRightWheelCollider;

    [SerializeField] private Transform frontLeftWheelTransform;
    [SerializeField] private Transform frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform;
    [SerializeField] private Transform rearRightWheelTransform;

    public enum driveType { RWD, AWD }
    public driveType drive = driveType.RWD;

    public Text speedo;
    public Text rpmText;
    public Text gearText;

    private Quaternion defaultRotation;
    private Vector3 defaultPosition;
    private WheelFrictionCurve defaultForwardFrictionCurve;
    private WheelFrictionCurve defaultSidewaysFrictionCurve;

    private Rigidbody rb;

    public float[] gearRatios;
    private int currentGear = 1;
    float engineRPM = 0;
    public float redline;
    public float maxRpm;

    public AnimationCurve torqueCurve;
    public float gearLength;

    void Start()
    {
        defaultRotation = transform.rotation;
        defaultPosition = transform.position;

        defaultForwardFrictionCurve = rearLeftWheelCollider.forwardFriction;
        defaultSidewaysFrictionCurve = rearLeftWheelCollider.sidewaysFriction;

        currentGear = 1;

        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass.localPosition;
    }

    private void Update()
    {
        GetInput();
        UpdateWheels();

        double speed = Math.Round(rb.velocity.magnitude * 3.6);
        speedo.text = speed.ToString() + "km/h";

        //double wheelSpeed = Math.Round(((rearLeftWheelCollider.rpm * 2 * 3.14 * rearLeftWheelCollider.radius) / 60) * 3.6);
        //  speedo1.text = wheelSpeed.ToString() + "km/h";

        rpmText.text = Math.Round(engineRPM).ToString() + "rpm";

        gearText.text = currentGear.ToString();

    }

    private void FixedUpdate()
    {
        if (drive == driveType.RWD)
        {
            engineRPM = (rearLeftWheelCollider.rpm + rearRightWheelCollider.rpm) / 2f * gearRatios[currentGear];
        }
        else
        {
            engineRPM = (rearLeftWheelCollider.rpm + rearRightWheelCollider.rpm + frontLeftWheelCollider.rpm + frontRightWheelCollider.rpm) / 4f * gearRatios[currentGear];
        }

        HandleMotor();
        HandleSteering();
       
    }

    private void GetInput()
    {
        if (Input.GetKey(KeyCode.R))
        {
            transform.rotation = defaultRotation;
            transform.position = defaultPosition;
        }
        horizontalInput = Input.GetAxis(HORIZONTAL);

        float verticalInput = Input.GetAxis(VERTICAL);
        if(verticalInput >= 0)
        {
            brakeInput = 0;
            throttleInput = verticalInput;
        }
        else
        {
            throttleInput = 0;
            brakeInput = -verticalInput;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            isHandBrakeOn = true;
        }
        else
        {
            isHandBrakeOn = false;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ApplyHandbrake();
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            ReleaseHandbrake();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if(currentGear > 0)
            {
                gearDown();
            }
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            if(currentGear < gearRatios.Length - 1)
            {
                gearUp();
            }

        }

    }

    private void gearUp()
    {
        currentGear += 1;
    }
    private void gearDown()
    {
        currentGear -= 1;
    }


    private void HandleMotor()
    {
        //double wheelSpeed = Math.Round(((rearLeftWheelCollider.rpm * 2 * 3.14 * rearLeftWheelCollider.radius) / 60) * 3.6);
        float mt;
        if (currentGear > 0)
        {
            float torque = torqueCurve.Evaluate(engineRPM * gearLength);
            mt = torque * gearRatios[currentGear] * throttleInput * motorForce;
        }
        else
        {
            float torque = torqueCurve.Evaluate(-engineRPM * gearLength);
            mt = -torque * gearRatios[currentGear] * throttleInput * motorForce;
        }

        if (drive == driveType.RWD)
        {
            rearLeftWheelCollider.motorTorque = mt;
            rearRightWheelCollider.motorTorque = mt;
        }
        else
        {
            rearLeftWheelCollider.motorTorque = mt;
            rearRightWheelCollider.motorTorque = mt;
            frontLeftWheelCollider.motorTorque = mt;
            frontRightWheelCollider.motorTorque = mt;
        }
        currentBrakeForce = 0f;

        currentBrakeForce = Mathf.Lerp(0, brakeForce, brakeInput);
        Debug.Log(currentBrakeForce);
        ApplyBraking();
    }

    private void ApplyBraking()
    {
        frontRightWheelCollider.brakeTorque = currentBrakeForce;
        frontLeftWheelCollider.brakeTorque = currentBrakeForce;
        rearLeftWheelCollider.brakeTorque = currentBrakeForce;
        rearRightWheelCollider.brakeTorque = currentBrakeForce;
    }

    private void HandleSteering()
    {
        currentSteerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;

        if (isHandBrakeOn)
        {
            rearLeftWheelCollider.brakeTorque = brakeForce;
            rearRightWheelCollider.brakeTorque = brakeForce;
        }
    }

    private void ApplyHandbrake()
    {
        WheelFrictionCurve newForwardCurve = rearLeftWheelCollider.forwardFriction;
        newForwardCurve.stiffness = defaultForwardFrictionCurve.stiffness / 2;
        WheelFrictionCurve newSidewaysCurve = rearLeftWheelCollider.sidewaysFriction;
        newSidewaysCurve.stiffness = defaultSidewaysFrictionCurve.stiffness / 2;
        rearLeftWheelCollider.forwardFriction = newForwardCurve;
        rearLeftWheelCollider.sidewaysFriction = newSidewaysCurve;
        rearRightWheelCollider.forwardFriction = newForwardCurve;
        rearRightWheelCollider.sidewaysFriction = newSidewaysCurve;
    }

    private void ReleaseHandbrake()
    {
        WheelFrictionCurve newForwardCurve = rearLeftWheelCollider.forwardFriction;
        newForwardCurve.stiffness = defaultForwardFrictionCurve.stiffness;
        WheelFrictionCurve newSidewaysCurve = rearLeftWheelCollider.sidewaysFriction;
        newSidewaysCurve.stiffness = defaultSidewaysFrictionCurve.stiffness;
        rearLeftWheelCollider.forwardFriction = newForwardCurve;
        rearLeftWheelCollider.sidewaysFriction = newSidewaysCurve;
        rearRightWheelCollider.forwardFriction = newForwardCurve;
        rearRightWheelCollider.sidewaysFriction = newSidewaysCurve;
    }
    private void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot; 
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }
}