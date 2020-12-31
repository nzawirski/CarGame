using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Transmission", menuName = "Car/Transmission")]
public class Transmission : ScriptableObject
{
    public float[] gearRatios;
    public float shiftTime;
}
