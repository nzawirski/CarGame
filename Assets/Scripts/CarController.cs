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
    private float verticalInput;
    private float currentSteerAngle;
    private float currentBrakeForce;
    private bool isBraking;

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

    private Rigidbody rb;

    public float[] gearRatios;
    public int currentGear = 0;
    float engineRPM = 0;
    public float minRpm;
    public float maxRpm;

    public AnimationCurve torqueCurve;
    public float gearLength;

    void Start()
    {
        defaultRotation = transform.rotation;
        defaultPosition = transform.position;

        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass.localPosition;
    }

    private void Update()
    {
        GetInput();
        UpdateWheels();
        GearShift();

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
            engineRPM = (rearLeftWheelCollider.rpm + rearRightWheelCollider.rpm) / 2 * gearRatios[currentGear];
        }
        else
        {
            engineRPM = (rearLeftWheelCollider.rpm + rearRightWheelCollider.rpm + frontLeftWheelCollider.rpm + frontRightWheelCollider.rpm) / 4 * gearRatios[currentGear];
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
        verticalInput = Input.GetAxis(VERTICAL);
        isBraking = Input.GetKey(KeyCode.Space);
        
    }

    private void GearShift()
    {
        if (Input.GetKeyDown(KeyCode.Q) && currentGear > 0)
        {
            currentGear -= 1;
        }

        if (Input.GetKeyDown(KeyCode.E) && currentGear < 5)
        {
            currentGear += 1;
        }
    }



    private void HandleMotor()
    {
        double wheelSpeed = Math.Round(((rearLeftWheelCollider.rpm * 2 * 3.14 * rearLeftWheelCollider.radius) / 60) * 3.6);

        Debug.Log(torqueCurve.Evaluate(engineRPM*gearLength));
        //Debug.Log(torqueCurve.Evaluate(engineRPM) * motorForce * gearRatios[currentGear] * verticalInput);


        if (drive == driveType.RWD){
            rearLeftWheelCollider.motorTorque = torqueCurve.Evaluate(engineRPM*gearLength)  * gearRatios[currentGear] * verticalInput;
            rearRightWheelCollider.motorTorque = torqueCurve.Evaluate(engineRPM*gearLength)  * gearRatios[currentGear] * verticalInput;
        }
        else
        {
            rearLeftWheelCollider.motorTorque = motorForce * gearRatios[currentGear] * verticalInput;
            rearRightWheelCollider.motorTorque = motorForce * gearRatios[currentGear] * verticalInput;
            frontLeftWheelCollider.motorTorque = motorForce * gearRatios[currentGear] * verticalInput;
            frontRightWheelCollider.motorTorque = motorForce * gearRatios[currentGear] * verticalInput;
        }

        currentBrakeForce = 0f;

        currentBrakeForce = isBraking ? brakeForce : currentBrakeForce;
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