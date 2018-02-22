using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GeneticAlgorithm : MonoBehaviour {

    // Privates
    MatchSimulator m_simulator;

    private void Awake()
    {
        // Initialize values
        m_simulator = GameObject.FindGameObjectWithTag("matchSimulator").GetComponent<MatchSimulator>();
        m_simulator.MatchOver += MatchSessionOver; ;
    }

    private void MatchSessionOver(object sender, EventArgs args)
    {
        Debug.Log("Match over! " + Time.time);
    }
}
