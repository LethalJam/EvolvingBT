using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GeneticAlgorithm : MonoBehaviour {

    // Privates
    protected MatchSimulator m_simulator;
    protected List<N_Root> m_population = new List<N_Root>();
    protected List<N_Root> m_childPop = new List<N_Root>();

    private void Awake()
    {
        // Initialize values
        m_simulator = GameObject.FindGameObjectWithTag("matchSimulator").GetComponent<MatchSimulator>();

        if (m_simulator != null)
            m_simulator.MatchOver += MatchSessionOver;
        else
            Debug.LogError("No matchsimulator was found in GeneticAlgorithm");
    }

    private void MatchSessionOver(object sender, EventArgs args)
    {
        Debug.Log("Match over! " + Time.time);
    }

    private N_Root Randomgenome()
    {
        throw new NotImplementedException();
    }

    private void Update()
    {
        
    }
}
