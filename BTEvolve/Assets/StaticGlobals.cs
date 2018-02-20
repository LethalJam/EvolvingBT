using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticGlobals : MonoBehaviour {

    public MeshRenderer plane;

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
}
