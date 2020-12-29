using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
public class CarController : MonoBehaviour
{
    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";
    //Input
    private float horizontalInput;
    private float throttleInput;
    private float brakeInput;

    //Wheels
    [SerializeField] private WheelCollider frontLeftWheelCollider;
    [SerializeField] private WheelCollider frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider;
    [SerializeField] private WheelCollider rearRightWheelCollider;

    [SerializeField] private Transform frontLeftWheelTransform;
    [SerializeField] private Transform frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform;
    [SerializeField] private Transform rearRightWheelTransform;

    //Default stats
    private Quaternion defaultRotation;
    private Vector3 defaultPosition;
    private WheelFrictionCurve defaultForwardFrictionCurve;
    private WheelFrictionCurve defaultSidewaysFrictionCurve;

    //Private vars
    private float currentSteerAngle;
    private float currentBrakeForce;
    private bool isHandBrakeOn;
    private int currentGear = 1;
    float engineRPM = 0;

    private Rigidbody rb;

    //stats
    [SerializeField] private float motorForce;
    [SerializeField] private float brakeForce;
    [SerializeField] private float maxSteerAngle;
    public float[] gearRatios;
    public enum driveType { RWD, AWD }
    public driveType drive = driveType.RWD;
    public AnimationCurve torqueCurve;
    public float redline;
    public float maxRpm;

    public Transform centerOfMass;

    //Post processing bullshit
    public Volume m_Volume;
    private VolumeProfile ppProfile;
    private ChromaticAberration ca;

    //UI
    public Text speedo;
    public Text rpmText;
    public Text gearText;
    //Gauges
    public Slider tacho;
    public Slider rpmDisplay;
    public Image rpmImage;

    //Sound
    private FMODUnity.StudioEventEmitter engineSoundEmmiter;
    private FMOD.Studio.EventInstance engineSoundInstance;
    private FMOD.Studio.EventDescription engineSoundDescription;
    private FMOD.Studio.PARAMETER_DESCRIPTION fmodRPM;
    private FMOD.Studio.PARAMETER_DESCRIPTION fmodLoad;


    void Start()
    {
        defaultRotation = transform.rotation;
        defaultPosition = transform.position;

        defaultForwardFrictionCurve = rearLeftWheelCollider.forwardFriction;
        defaultSidewaysFrictionCurve = rearLeftWheelCollider.sidewaysFriction;

        //pp
        ppProfile = m_Volume.sharedProfile;
        if(!ppProfile.TryGet<ChromaticAberration>(out ChromaticAberration ca))
        {
            ca = ppProfile.Add<ChromaticAberration>(false);
        }
        ca.intensity.Override(0f);
        //Set redline on Tacho
        tacho.value = redline / maxRpm;

        currentGear = 1;

        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass.localPosition;

        //Sound
        //event:/Engine/Engine1

        engineSoundEmmiter = GetComponent<FMODUnity.StudioEventEmitter>();
        engineSoundEmmiter.Play();
        engineSoundInstance = engineSoundEmmiter.EventInstance;
        engineSoundDescription = engineSoundEmmiter.EventDescription;

        engineSoundDescription.getParameterDescriptionByName("RPM", out fmodRPM);
        engineSoundDescription.getParameterDescriptionByName("Load", out fmodLoad);
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

        //update rpms on tacho
        rpmImage.color = engineRPM > redline ? new Color(1f, 0f, 0f) : new Color(1f, 1f, 1f);
        rpmDisplay.value = engineRPM / maxRpm;

        //SFX
        engineSoundInstance.setParameterByID(fmodRPM.id, engineRPM / maxRpm);
        engineSoundInstance.setParameterByID(fmodLoad.id, Mathf.Lerp(0.2f, 0.8f, throttleInput));

       

        //pp
        ppProfile.TryGet<ChromaticAberration>(out ChromaticAberration ca);
        float caIntensity = Mathf.Lerp(0, 1, (float)speed / 150);

        ca.intensity.Override(caIntensity);

      
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

        if (Input.GetKey(KeyCode.Space) || Input.GetKey("joystick button 0"))
        {
            isHandBrakeOn = true;
        }
        else
        {
            isHandBrakeOn = false;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown("joystick button 0"))
        {
            ApplyHandbrake();
        }
        if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp("joystick button 0"))
        {
            ReleaseHandbrake();
        }

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown("joystick button 2"))
        {
            if(currentGear > 0)
            {
                gearDown();
            }
        }
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown("joystick button 1"))
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
            float torque = torqueCurve.Evaluate(engineRPM);
            mt = torque * gearRatios[currentGear] * throttleInput * motorForce;
        }
        else
        {
            float torque = torqueCurve.Evaluate(-engineRPM);
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