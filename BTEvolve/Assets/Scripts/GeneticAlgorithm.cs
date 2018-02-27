using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GeneticAlgorithm : MonoBehaviour {

    // Class representing all relevant values for a genome.
    public class Genome
    {
        // RootNode defines the entire tree.
        N_Root rootNode;
        // Subroots are the nodes that act as roots for the various subtrees within
        // the tree of the genome.
        List<Node> subRoots;
        // Damage taken and given are the latest updated values given from evaluation.
        // These are then taken into consideration when selecting genomes.
        int damageTaken = 0, damageGiven = 0;

        // Initialize variables.
        public Genome()
        {
            subRoots = new List<Node>();
            rootNode = null;
        }
        // Set and get functions for various values.
        public N_Root RootNode { get { return rootNode; } set { rootNode = value; } }
        public List<Node> SubRoots { get { return subRoots; } set { subRoots = value; } }
        public int DamageTaken { get { return damageTaken;  } set { damageTaken = value; } }
        public int DamageGiven { get { return damageGiven; } set { damageGiven = value; } }
    }

    // Protected variables used by all genetic algorithms of this solution.
    protected MatchSimulator m_simulator;
    protected List<Genome> m_population = new List<Genome>();
    protected List<Genome> m_childPop = new List<Genome>();

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
