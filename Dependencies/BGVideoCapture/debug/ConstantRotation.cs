using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantRotation : MonoBehaviour
{
    public Vector3 angularVelocity;

    
    void Update()
    {
        transform.Rotate(angularVelocity * Time.deltaTime, Space.World);
    }
}
