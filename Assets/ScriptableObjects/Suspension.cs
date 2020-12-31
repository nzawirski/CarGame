using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Suspension", menuName = "Car/Suspension")]
public class Suspension : ScriptableObject
{
    public enum driveType { RWD, AWD }
    public driveType drive;

    public float brakeForce;
    public float maxSteerAngle;
    public float springs;
    public float damper;
    
}
