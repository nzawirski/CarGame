using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class wheelControler : MonoBehaviour
{
    private WheelCollider wheelCollider;
    public TrailRenderer trailRenderer;

    // Start is called before the first frame update
    void Start()
    {
        wheelCollider = GetComponent<WheelCollider>();

    }

    // Update is called once per frame
    void Update()
    {
        ParticleSystem pS = GetComponent<ParticleSystem>();
        WheelHit hit;

        if (wheelCollider.GetGroundHit(out hit))
        {
            Debug.Log(hit.sidewaysSlip);
            if (hit.sidewaysSlip > 0.5 || hit.forwardSlip > 0.5 || hit.sidewaysSlip < -0.5 || hit.forwardSlip < -0.5)
            {
                var em = pS.emission;
                em.enabled = true; //bez prewarmu nie działa
                pS.Play();
            } else
            {
               pS.Stop();
            }

        }
    }
}
