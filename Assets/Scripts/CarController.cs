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

    //Private vars
    private float currentSteerAngle;
    private float currentBrakeForce;
    private bool isHandBrakeOn;
    private int currentGear = 1;
    float engineRPM = 0;
    private bool clutchEngaged = false;

    private Rigidbody rb;

    public enum gearboxTypes {
        auto,
        manual
    };

    public gearboxTypes gearboxType;

    float engineLowerGear = 0;

    //stats
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

    //Parts
    public Engine engine;
    public Transmission transmission;
    public Suspension suspension;

    void Start()
    {
        defaultRotation = transform.rotation;
        defaultPosition = transform.position;

        //pp
        ppProfile = m_Volume.sharedProfile;
        if(!ppProfile.TryGet<ChromaticAberration>(out ChromaticAberration ca))
        {
            ca = ppProfile.Add<ChromaticAberration>(false);
        }
        ca.intensity.Override(0f);
        //Set redline on Tacho
        tacho.value = engine.redline / engine.maxRpm;

        currentGear = 1;

        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass.localPosition;

        ApplySuspension();

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
        rpmImage.color = engineRPM > engine.redline ? new Color(1f, 0f, 0f) : new Color(1f, 1f, 1f);
        rpmDisplay.value = engineRPM / engine.maxRpm;

        //SFX
        engineSoundInstance.setParameterByID(fmodRPM.id, Mathf.Abs(engineRPM) / engine.maxRpm);
        engineSoundInstance.setParameterByID(fmodLoad.id, Mathf.Lerp(0.4f, 0.8f, throttleInput));

        //pp
        ppProfile.TryGet<ChromaticAberration>(out ChromaticAberration ca);
        float caIntensity = Mathf.Lerp(0, 1, (float)speed / 150);

        ca.intensity.Override(caIntensity);

        if (gearboxType == gearboxTypes.auto)
        {
            engineLowerGear = (rearLeftWheelCollider.rpm + rearRightWheelCollider.rpm) / 2f * transmission.gearRatios[currentGear-1];
            
            WheelHit hit;

            if (frontLeftWheelCollider.GetGroundHit(out hit))
            { 
                if (engineRPM>engine.redline && hit.forwardSlip < 0.2 && currentGear<transmission.gearRatios.Length-1) {

                    gearUp();
                }
            }

            if (engineLowerGear<engine.redline*0.8 && currentGear > 1 && Input.GetAxis("Vertical")<=0)
            {
                gearDown();
            }
        }
      
    }

    private void FixedUpdate()
    {
        if (suspension.drive == Suspension.driveType.RWD)
        {
            engineRPM = (rearLeftWheelCollider.rpm + rearRightWheelCollider.rpm) / 2f * transmission.gearRatios[currentGear];
        }
        else
        {
            engineRPM = (rearLeftWheelCollider.rpm + rearRightWheelCollider.rpm + frontLeftWheelCollider.rpm + frontRightWheelCollider.rpm) / 4f * transmission.gearRatios[currentGear];
        }

        HandleMotor();
        HandleSteering();

    }

    private void GetInput()
    {
        if (Input.GetKey(KeyCode.R)) //reset
        {
            transform.rotation = defaultRotation;
            transform.position = defaultPosition;
        }

        horizontalInput = Input.GetAxis(HORIZONTAL); // turning

        float verticalInput = Input.GetAxis(VERTICAL);
        if(verticalInput >= 0) //throttle
        {
            brakeInput = 0;
            if (!clutchEngaged)
            {
                throttleInput = verticalInput;
            }

        }
        else //brake
        {
            throttleInput = 0;
            brakeInput = -verticalInput;
        }

        if (Input.GetKey(KeyCode.Space) || Input.GetKey("joystick button 0")) //handbrake
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

       // if (gearboxType == gearboxTypes.manual) { 
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown("joystick button 2")) //gear down
            {
                if(currentGear > 0)
                { 
                    gearDown();
                }
            }
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown("joystick button 1")) //gear up
            {
                if(currentGear < transmission.gearRatios.Length - 1)
                {
                    gearUp();    
                }

            }
      //  }

    }

    private void engageClutch()
    {
        clutchEngaged = true;
    }

    private void disengageClutch()
    {
        clutchEngaged = false;
    }

    private void gearUp()
    {
        engageClutch();
        throttleInput = 0;
        currentGear += 1;
        Invoke(nameof(disengageClutch), transmission.shiftTime);
    }
    private void gearDown()
    {
        engageClutch();
        throttleInput = 0;
        currentGear -= 1;
        Invoke(nameof(disengageClutch), transmission.shiftTime);
    }


    private void HandleMotor()
    {
        //double wheelSpeed = Math.Round(((rearLeftWheelCollider.rpm * 2 * 3.14 * rearLeftWheelCollider.radius) / 60) * 3.6);
        float mt;
        if (currentGear > 0)
        {
            float torque = engine.torqueCurve.Evaluate(engineRPM);
            mt = torque * transmission.gearRatios[currentGear] * throttleInput * engine.motorForce;
        }
        else
        {
            float torque = engine.torqueCurve.Evaluate(-engineRPM);
            mt = -torque * transmission.gearRatios[currentGear] * throttleInput * engine.motorForce;
        }

        if (suspension.drive == Suspension.driveType.RWD)
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

        if (throttleInput == 0 && brakeInput == 0)
        {
            currentBrakeForce = Mathf.Lerp(0,1000,engineRPM/engine.redline);
        } else
        {
            currentBrakeForce = Mathf.Lerp(0, suspension.brakeForce, brakeInput);
        }

        ApplyBraking();
    }

    private void ApplyBraking()
    {
        frontRightWheelCollider.brakeTorque = currentBrakeForce;
        frontLeftWheelCollider.brakeTorque = currentBrakeForce;
       
        if (isHandBrakeOn)
        {
            rearLeftWheelCollider.brakeTorque = suspension.brakeForce;
            rearRightWheelCollider.brakeTorque = suspension.brakeForce;
        } else
        {
            rearLeftWheelCollider.brakeTorque = currentBrakeForce;
            rearRightWheelCollider.brakeTorque = currentBrakeForce;
        }
    }

    private void HandleSteering()
    {
        currentSteerAngle = suspension.maxSteerAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;


    }

    private void ApplyHandbrake()
    {
        rearLeftWheelCollider.GetComponent<wheelControler>().ApplyHandbrake();
        rearRightWheelCollider.GetComponent<wheelControler>().ApplyHandbrake();
        frontLeftWheelCollider.GetComponent<wheelControler>().ApplyHandbrake();
        frontRightWheelCollider.GetComponent<wheelControler>().ApplyHandbrake();
    }

    private void ReleaseHandbrake()
    {
        rearLeftWheelCollider.GetComponent<wheelControler>().ReleaseHandbrake();
        rearRightWheelCollider.GetComponent<wheelControler>().ReleaseHandbrake();
        frontLeftWheelCollider.GetComponent<wheelControler>().ReleaseHandbrake();
        frontRightWheelCollider.GetComponent<wheelControler>().ReleaseHandbrake();
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

    private void ApplySuspension()
    {
        rearLeftWheelCollider.suspensionDistance = suspension.suspensionDistance;
        rearRightWheelCollider.suspensionDistance = suspension.suspensionDistance;
        frontLeftWheelCollider.suspensionDistance = suspension.suspensionDistance;
        frontRightWheelCollider.suspensionDistance = suspension.suspensionDistance;


        JointSpring nSuspension = rearLeftWheelCollider.suspensionSpring;
        nSuspension.spring = suspension.springs;
        nSuspension.damper = suspension.damper;

        rearLeftWheelCollider.suspensionSpring = nSuspension;
        rearRightWheelCollider.suspensionSpring = nSuspension;
        frontLeftWheelCollider.suspensionSpring = nSuspension;
        frontRightWheelCollider.suspensionSpring = nSuspension;
    }
}