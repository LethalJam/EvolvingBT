using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngleTest : MonoBehaviour {

    public Vector2 a, b;
	
	// Update is called once per frame
	void Update ()
    {
        float angle = Vector2.Angle(a, b);
        float crossZ = Vector3.Cross(a, b).z;

        if (crossZ > 0)
            angle = 360 - angle;

        Debug.Log("Angle: " + angle);
	}
}
