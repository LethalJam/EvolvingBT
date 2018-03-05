﻿using System.Collections;
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
        // Condition nodes are saved seperately for use in mutation.
        List<Node> conditions;
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
            // Retrieve all condition nodes.
            conditions = TreeOperations.RetrieveNodesOfType(rootNode, typeof(N_Condition));
        }

        // Set and get functions for various values.
        public N_Root RootNode { get { return rootNode; } set { rootNode = value; } }
        public List<Node> SubRoots { get { return subRoots; } set { subRoots = value; } }
        public List<Node> Conditions { get { return conditions;  } set { conditions = value; } }
        public int DamageTaken { get { return damageTaken;  } set { damageTaken = value; } }
        public int DamageGiven { get { return damageGiven; } set { damageGiven = value; } }
    }
    #endregion

    [Header("Start algorithm on startup of program?")]
    public bool evolveOnStart = false;
    [Header("Adjust the chance for additional compositions being generated during randomization.")]
    [Range(0.0f, 0.8f)]
    public float additionalCompChance = 0.3f;
    // Adjustable parameters.
    [Header("Variables for adjusting the settings of the genetic algorithm.")]
    [Tooltip("Set amount of generations for the execution.")]
    public int generations = 10;
    [Tooltip("Set size of the population for each generation.")]
    public int populationSize = 40;
    [Tooltip("Set the amount of subtrees within the genome.")]
    public int genomeSubtrees = 6;
    [Tooltip("Set the probability of a random mutation occuring.( 0 - 100%) ")]
    [Range(0.0f, 1.0f)]
    public float mutationRate = 0.05f;

    // Protected variables used by all genetic algorithms of this solution.
    protected MatchSimulator m_simulator;
    protected List<Genome> m_population = new List<Genome>();
    protected List<Genome> m_childPop = new List<Genome>();
    protected bool simulating = false;

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
        simulating = false;
        Debug.Log("Match over! " + Time.time);
    }
    
    // Randomize a new BT using given defined subtrees and definitions.
    protected N_Root RandomBT()
    {
        N_Root root = new N_Root();

        // First, randomize starting composition.
        N_CompositionNode firstComp = RandomComp();
        
        N_CompositionNode currentComp = firstComp;
        bool nestled = false;

        // Generate random subtrees 
        for (int i = 0; i < genomeSubtrees; i++)
        {
            // Calculate random chance for adding a new composition as well
            // as ending it.
            float randomChance = UnityEngine.Random.Range(0.0f, 1.0f);
            if (randomChance < additionalCompChance && !nestled)
            {
                nestled = true;
                N_CompositionNode subComp = RandomComp();

                // Attach new composition and add a subtree to it.
                currentComp.AddFirst(subComp);
                currentComp = subComp;
            }
            else if (randomChance < (additionalCompChance+0.2f))
            {
                nestled = false;
                currentComp = firstComp;
            }

            // Lastly, add a random subtree to the current composition.
             currentComp.AddFirst(RandomSubtree());
        }

        root.Child = firstComp;

        return root;
    }

    private Node RandomSubtree()
    {
        int randomSubtreeIndex = UnityEngine.Random.Range(0, 6);
        Node subtree;
        int randomThreshold;
        switch (randomSubtreeIndex)
        {
            case 0:
                randomThreshold = UnityEngine.Random.Range(0, 100);
                subtree = BehaviourSubtrees.Tree_GetHealthpackIfLow(null, randomThreshold);
                break;
            case 1:
                randomThreshold = UnityEngine.Random.Range(0, 15);
                subtree = BehaviourSubtrees.Tree_ReloadIfLow(null, randomThreshold);
                break;
            case 2:
                subtree = BehaviourSubtrees.Tree_ShootAtEnemy(null);
                break;
            case 3:
                subtree = BehaviourSubtrees.Tree_Patrol(null);
                break;
            case 4:
                subtree = BehaviourSubtrees.Tree_PatrolOrKite(null);
                break;
            case 5:
                subtree = BehaviourSubtrees.Tree_TurnWhenShot(null);
                break;
            case 6:
                subtree = BehaviourSubtrees.Tree_FollowEnemy(null);
                break;
            default:
                subtree = null;
                Debug.LogError("No subtree was chosen in randomization." +
                    " Random value of incorrect range?");
                break;
        }
        return subtree;
    }

    private N_CompositionNode RandomComp()
    {
        N_CompositionNode randomComp;
        int randomComposition = UnityEngine.Random.Range(0, 2);
        switch (randomComposition)
        {
            case 0:
                randomComp = new N_Selection();
                break;
            case 1:
                randomComp = new N_Sequence();
                break;
            case 2:
                randomComp = new N_ProbabilitySelector();
                break;
            default:
                randomComp = null;
                Debug.LogError("No comp was chosen for generation of BT.");
                break;
        }
        return randomComp;
    }

    protected void Simulate()
    {
        // Against best

    }

    protected void Select(out N_Root parent0, out N_Root parent1)
    {
        // Temporary
        parent0 = new N_Root();
        parent1 = new N_Root();
    }
    // DONT FORGET TO DEEP COPY WHEN COMBINING!!!!!!!
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
