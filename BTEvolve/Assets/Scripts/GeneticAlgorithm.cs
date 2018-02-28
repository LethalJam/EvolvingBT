using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GeneticAlgorithm : MonoBehaviour {

    #region Genome structure
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
        public Genome(N_Root root)
        {
            subRoots = new List<Node>();
            rootNode = root;
        }
        // Set and get functions for various values.
        public N_Root RootNode { get { return rootNode; } set { rootNode = value; } }
        public List<Node> SubRoots { get { return subRoots; } set { subRoots = value; } }
        public int DamageTaken { get { return damageTaken;  } set { damageTaken = value; } }
        public int DamageGiven { get { return damageGiven; } set { damageGiven = value; } }
    }
    #endregion

    [Header("Start algorithm on startup of program?")]
    public bool evolveOnStart = false;
    // Adjustable parameters.
    [Header("Variables for adjusting the settings of the genetic algorithm.")]
    [Tooltip("Set amount of generations for the execution.")]
    public int generations = 10;
    [Tooltip("Set size of the population for each generation.")]
    public int populationSize = 40;
    [Tooltip("Set the probability of a random mutation occuring.( 0 - 100%) ")]
    [Range(0.0f, 1.0f)]
    public float mutationRate = 0.05f;

    // Protected variables used by all genetic algorithms of this solution.
    protected MatchSimulator m_simulator;
    protected List<Genome> m_population = new List<Genome>();
    protected List<Genome> m_childPop = new List<Genome>();

    #region Initialization (Awake/Start)
    // Make sure that common GA functions are at lowest protected, not private.
    protected void Awake()
    {
        // Initialize values
        m_simulator = GameObject.FindGameObjectWithTag("matchSimulator").GetComponent<MatchSimulator>();

        if (m_simulator != null)
            m_simulator.MatchOver += MatchSessionOver;
        else
            Debug.LogError("No matchsimulator was found in GeneticAlgorithm");
    }

    protected void Start()
    {
        if (evolveOnStart)
            Evolve();
    }
    #endregion

    protected void MatchSessionOver(object sender, EventArgs args)
    {
        Debug.Log("Match over! " + Time.time);
    }

    // Randomize a new BT using given defined subtrees and definitions.
    protected N_Root RandomBT()
    {
        throw new NotImplementedException();
    }

    protected void Simulate()
    {

    }
    protected void Select(out N_Root parent0, out N_Root parent1)
    {
        // Temporary
        parent0 = new N_Root();
        parent1 = new N_Root();
    }
    protected void Combine (out N_Root child0, out N_Root child1, N_Root parent0, N_Root parent1)
    {
        // Temporary
        child0 = new N_Root();
        child1 = new N_Root();
    }
    protected void Mutate (ref N_Root child)
    {

    }

    // Function for starting the actual evolutionary progress.
    public virtual void  Evolve()
    {
        // Start by randomly generating the initial population.
        for (int i = 0; i < populationSize; i++)
        {
            m_population.Add(new Genome(RandomBT()));
        }

        // Run algorithm for the given amount of generations.
        for (int i = 0; i < generations; i++)
        {
            // Start off by simulating all genomes.
            Simulate();

            // Add genomes to childpop until it's the same size as previous.
            while (m_childPop.Count < m_population.Count)
            {
                N_Root parent0, parent1;
                // Select genomes given the results of simulation.
                Select(out parent0, out parent1); // Implicit evaluation

                N_Root child0, child1;
                // Combine parents to retrieve two children.
                Combine(out child0, out child1, parent0, parent1);

                // Finally, randomly mutate children and then add them to population.
                Mutate(ref child0);
                Mutate(ref child1);
                m_childPop.Add(new Genome(child0));
                m_childPop.Add(new Genome(child1));
            }

            // Set the previous population to the current childPop.
            m_population = m_childPop;
            m_childPop.Clear();
        }

    }
}
