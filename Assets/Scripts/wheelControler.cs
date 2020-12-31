using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class wheelControler : MonoBehaviour
{
    private WheelCollider wheelCollider;
    private TrailRenderer trailRenderer;
    private ParticleSystem pS;
    private ParticleSystem.EmissionModule particleEmission;

    private FMODUnity.StudioEventEmitter soundEmmiter;

    public Wheel wheel;

    private WheelFrictionCurve roadForwardFrictionCurve;
    private WheelFrictionCurve roadSidewaysFrictionCurve;

    private WheelFrictionCurve offroadForwardFrictionCurve;
    private WheelFrictionCurve offroadSidewaysFrictionCurve;

    private bool isHandbrakeOn;

    void Start()
    {
        wheelCollider = GetComponent<WheelCollider>();
        //vfx
        trailRenderer = GetComponentInChildren<TrailRenderer>();
        pS = GetComponent<ParticleSystem>();
        particleEmission = pS.emission;
        particleEmission.enabled = false;
        pS.Play();

        //sfx
        soundEmmiter = GetComponent<FMODUnity.StudioEventEmitter>();


        roadForwardFrictionCurve = wheelCollider.forwardFriction;
        roadSidewaysFrictionCurve = wheelCollider.sidewaysFriction;

        offroadForwardFrictionCurve = wheelCollider.forwardFriction;
        offroadSidewaysFrictionCurve = wheelCollider.sidewaysFriction;

        roadForwardFrictionCurve.stiffness = wheel.roadFwdGrip;
        roadSidewaysFrictionCurve.stiffness = wheel.roadSwsGrip;

        offroadForwardFrictionCurve.stiffness = wheel.offFwdGrip;
        offroadSidewaysFrictionCurve.stiffness = wheel.offSwsGrip;

        isHandbrakeOn = false;
    }

    void Update()
    {

        WheelHit hit;

        if (wheelCollider.GetGroundHit(out hit))
        {

            
            if(hit.collider.tag == "Road")
            {
                if (isHandbrakeOn)
                {
                    WheelFrictionCurve roadForward = wheelCollider.forwardFriction;
                    WheelFrictionCurve roadSideways = wheelCollider.sidewaysFriction;
                    roadForward.stiffness = roadForwardFrictionCurve.stiffness / 2;
                    roadSideways.stiffness = roadSidewaysFrictionCurve.stiffness / 2;
                    wheelCollider.forwardFriction = roadForward;
                    wheelCollider.sidewaysFriction = roadSideways;
                } else
                {
                    wheelCollider.forwardFriction = roadForwardFrictionCurve;
                    wheelCollider.sidewaysFriction = roadSidewaysFrictionCurve;
                }

            } else if(hit.collider.tag == "Offroad")
            {
                if (isHandbrakeOn)
                {
                    WheelFrictionCurve offroadForward = wheelCollider.forwardFriction;
                    WheelFrictionCurve offroadSideways = wheelCollider.sidewaysFriction;
                    offroadForward.stiffness = offroadForwardFrictionCurve.stiffness / 2;
                    offroadSideways.stiffness = offroadSidewaysFrictionCurve.stiffness / 2;
                    wheelCollider.forwardFriction = offroadForward;
                    wheelCollider.sidewaysFriction = offroadSideways;
                }
                else
                {
                    wheelCollider.forwardFriction = offroadForwardFrictionCurve;
                    wheelCollider.sidewaysFriction = offroadSidewaysFrictionCurve;
                }
            }

            if (hit.sidewaysSlip > 0.5 || hit.forwardSlip > 0.5 || hit.sidewaysSlip < -0.5 || hit.forwardSlip < -0.5)
            {
                if((hit.sidewaysSlip > 0.7 || hit.forwardSlip > 0.7 || hit.sidewaysSlip < -0.7 || hit.forwardSlip < -0.7))
                {
                    particleEmission.enabled = true;
                }
                else
                {
                    particleEmission.enabled = false;
                }
                trailRenderer.emitting = true;
                if (!soundEmmiter.IsPlaying())
                {
                    soundEmmiter.Play();
                }

                
            } else
            {
                particleEmission.enabled = false;
                trailRenderer.emitting = false;
                if (soundEmmiter.IsPlaying())
                {
                    soundEmmiter.Stop();
                }
            }
        }
        else
        {
            particleEmission.enabled = false;
            trailRenderer.emitting = false;
            if (soundEmmiter.IsPlaying())
            {
                soundEmmiter.Stop();
            }
        }

    }

    public void ApplyHandbrake()
    {
        isHandbrakeOn = true;
    }

    public void ReleaseHandbrake()
    {
        isHandbrakeOn = false;
    }
}
