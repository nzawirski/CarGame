using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wheel", menuName = "Car/Wheels")]
public class Wheel : ScriptableObject
{
    //Road Grip
    public float roadFwdGrip;
    public float roadSwsGrip;

    //Offroad Grip
    public float offFwdGrip;
    public float offSwsGrip;
}
