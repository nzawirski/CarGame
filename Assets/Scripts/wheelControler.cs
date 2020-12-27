using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class wheelControler : MonoBehaviour
{
    private WheelCollider wheelCollider;
    private TrailRenderer trailRenderer;
    private ParticleSystem pS;
    private ParticleSystem.EmissionModule particleEmission;

    void Start()
    {
        wheelCollider = GetComponent<WheelCollider>();
        trailRenderer = GetComponentInChildren<TrailRenderer>();
        pS = GetComponent<ParticleSystem>();
        particleEmission = pS.emission;
        particleEmission.enabled = false;
        pS.Play();
    }

    void Update()
    {

        WheelHit hit;

        if (wheelCollider.GetGroundHit(out hit))
        {
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
                
            } else
            {
                particleEmission.enabled = false;
                trailRenderer.emitting = false;
            }
        }
        else
        {
            particleEmission.enabled = false;
            trailRenderer.emitting = false;
        }

    }
}
