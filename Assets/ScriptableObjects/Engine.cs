using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Engine", menuName = "Car/Engine")]
public class Engine : ScriptableObject
{
    public float motorForce;
    public AnimationCurve torqueCurve;
    public float redline;
    public float maxRpm;
   
}
