using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    public Transform Target;
    [Range(0.5f, 2f)]
    public float cameraHeight = 1f;
    [Range(2f, 30f)]
    public float offset = 7f;
    [Range(0.001f, 1f)]
    public float SmoothFactor = 1f;
    public float rotationSpeed = 50f;
    private Vector3 _cameraOffset;


    // Update is called once per frame
    void Update()
    {

        //zooming
        float d = Input.GetAxis("Mouse ScrollWheel");
        if (d > 0f)
        {
            // scroll up
            offset -= 1;
        }
        else if (d < 0f)
        {
            // scroll down
            offset += 1;
        }
        offset = Mathf.Clamp(offset, 2, 30);
    }

    void FixedUpdate()
    {
        _cameraOffset = -Target.forward * offset;
        _cameraOffset.y = cameraHeight;
        

        Vector3 newPos = Target.position + _cameraOffset;
        transform.position = Vector3.Slerp(transform.position, newPos, SmoothFactor);

        Quaternion rotationToTarget = Quaternion.LookRotation(Target.transform.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotationToTarget, rotationSpeed * Time.deltaTime);
    }
}
