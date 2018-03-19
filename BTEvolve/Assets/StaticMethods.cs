using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

// Static singleton staticmethods
public class StaticMethods {

    private static StaticMethods instance;
    private GameObject agent0, agent1;

    private StaticMethods()
    {
        agent0 = GameObject.Find("agent0");
        agent1 = GameObject.Find("agent1");
    }
    
    public static StaticMethods GetInstance()
    {
        if (instance == null)
            instance = new StaticMethods();

        return instance;
    }

    public ShooterAgent GetAgent0 ()
    {
        return agent0.GetComponent<ShooterAgent>();
    }
    public ShooterAgent GetAgent1 ()
    {
        return agent1.GetComponent<ShooterAgent>();
    }

    // Create a deep copy of a given object.
    public static T DeepCopy<T>(T other)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, other);
            ms.Position = 0;
            return (T)formatter.Deserialize(ms);
        }
    }
}
