using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticGlobals : MonoBehaviour {

    public MeshRenderer plane;
    public GameObject agent0, agent1;

    public Vector3 GetPlaneMax()
    {
        return plane.bounds.max;
    }
    public Vector3 GetPlaneMin()
    {
        return plane.bounds.min;
    }
    public MeshRenderer GetPlane()
    {
        return plane;
    }
    public ShooterAgent GetAgent0()
    {
        return agent0.GetComponent<ShooterAgent>();
    }
    public ShooterAgent GetAgent1()
    {
        return agent1.GetComponent<ShooterAgent>();
    }
}
