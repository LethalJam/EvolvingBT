using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

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
        int damageTaken, damageGiven = 0;
        bool wonLastMatch = false;
        int fitness = 0;

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
        public int Fitness { get { return fitness;  } set { fitness = value; } }
        public int DamageTaken { get { return damageTaken; } set { damageTaken = value; } }
        public int DamageGiven { get { return damageGiven; } set { damageGiven = value; } }
        public bool WonLastMatch { get { return wonLastMatch;  } set { wonLastMatch = value; } }

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
    protected Genome bestGenome;
    protected bool simulating = false;
    protected bool simulationDone = false;

    // Results sheets used for evaluation.
    AgentResults results0, results1;

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
            StartCoroutine(Evolve());
    }
    #endregion

    protected void MatchSessionOver(object sender, EventArgs args)
    {
        simulating = false;
        Debug.Log("Match over!");
    }

    #region BT generation
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
                currentComp.AddLast(subComp);
                currentComp = subComp;
            }
            else if (randomChance < (additionalCompChance+0.2f))
            {
                nestled = false;
                currentComp = firstComp;
            }

            // Lastly, add a random subtree to the current composition.
             currentComp.AddLast(RandomSubtree());
        }

        root.Child = firstComp;

        return root;
    }

    private Node RandomSubtree()
    {
        int randomSubtreeIndex = UnityEngine.Random.Range(0, 7);
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
        int randomComposition = UnityEngine.Random.Range(0, 3);
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
    #endregion

    protected IEnumerator Simulate()
    {
        // Simulate the genomes against the current best ones.
        foreach (Genome g in m_population)
        {
            // Set the agent BTs and start match.
            m_simulator.SetAgentBTs(g.RootNode, bestGenome.RootNode);
            m_simulator.StartMatch();
            simulating = true;
            // Yield until simulation of match is over.
            yield return new WaitUntil(() => !simulating);

            AgentResults results = m_simulator.Agent0Results;
            // Save results in actual genome structure for later evaluation.
            g.DamageGiven = results.damageGiven;
            g.DamageTaken = results.damageTaken;
            g.WonLastMatch = results.winner;
        }
        simulationDone = true;

        yield return null;
    }

    protected void Evaluate()
    {
        // Assign fitness value for each genome based on its statistics.
        foreach (Genome g in m_population)
        {
            if (g.WonLastMatch)
                g.Fitness += 150;
            g.Fitness -= g.DamageTaken;
            g.Fitness += g.DamageGiven;

            // Make the floor of all fitness values 0 to make the roulette selection valid.
            if (g.Fitness < 0)
                g.Fitness = 0;
        }
    }

    protected void RouletteSelect(out N_Root parent0, out N_Root parent1)
    {
        Dictionary<Genome, float> genomeToWeight = new Dictionary<Genome, float>();

        parent0 = parent1 = null;

        // Sum up the total weight of all fitness values.
        float totalFitness = 0.0f;
        foreach (Genome g in m_population)
            totalFitness += (float)g.Fitness;

        // Calculate the relative fotness for each genome
        float fitnessSum = 0.0f;
        foreach (Genome g in m_population)
        {
            float weight = fitnessSum + ((float)g.Fitness / totalFitness);
            genomeToWeight.Add(g, weight);
            fitnessSum += (g.Fitness / totalFitness); 
        }

        // Select two parents using semi-randomized roulette selection.
        for (int i = 0; i < 2; i++)
        {
            float random = UnityEngine.Random.Range(0.0f, 1.0f);
            N_Root selectedTree = null;
            for (int j = 0; j < genomeToWeight.Count; j++)
            {
                var current = genomeToWeight.ElementAt(j);
                if (j == 0)
                {
                    if (random < current.Value)
                        selectedTree = current.Key.RootNode;
                }
                else
                {
                    var prev = genomeToWeight.ElementAt(j - 1);
                    if (random < current.Value && random > prev.Value)
                    {
                        selectedTree = current.Key.RootNode;
                    }
                }
            }
            if (selectedTree == null)
                Debug.LogError("Selected tree is null...");

            // Assign parents during different iterations.
            switch (i)
            {
                case 0:
                    parent0 = TreeOperations.GetCopy(selectedTree, null);
                    break;
                case 1:
                    parent1 = TreeOperations.GetCopy(selectedTree, null);
                    break;
                default:
                    parent0 = new N_Root();
                    parent1 = new N_Root();
                    Debug.LogError("No parent was set in roulette");
                    break;
            }

        }

        if (parent0 == null || parent1 == null)
        {
            Debug.LogError("Parents weren't assigned during roulette selection!");
        }
    }

    protected void Combine (out N_Root child0, out N_Root child1, N_Root parent0, N_Root parent1)
    {
        // Temporary
        child0 = parent0;
        child1 = parent1;
    }
    protected void Mutate (N_Root child)
    {
        // First, check if there'll be any mutation at all.
        float random = UnityEngine.Random.Range(0.0f, 1.0f);
        if (random >  mutationRate)
        {

        }

    }

    // Function for starting the actual evolutionary progress.
    public virtual IEnumerator Evolve()
    {
        // Start by randomly generating the initial population.
        for (int i = 0; i < populationSize; i++)
        {
            m_population.Add(new Genome(RandomBT()));
        }
        // Randomly generate the first best genome
        bestGenome = new Genome(RandomBT());

        // Run algorithm for the given amount of generations.
        for (int i = 0; i < generations; i++)
        {
            Debug.Log("Starting simulation step of generation " + i + "... ");
            // Start simulation and wait until done.
            simulationDone = false;
            StartCoroutine(Simulate());
            yield return new WaitUntil(() => simulationDone);
            Debug.Log("Simulation step complete!");

            // After simulation, start evaluation.
            Evaluate();

            // Add genomes to childpop until it's the same size as previous.
            while (m_childPop.Count < m_population.Count)
            {
                N_Root parent0, parent1;
                // Select genomes given the results of simulation.
                RouletteSelect(out parent0, out parent1); // Implicit evaluation

                N_Root child0, child1;
                // Combine parents to retrieve two children.
                Combine(out child0, out child1, parent0, parent1);

                // Finally, randomly mutate children and then add them to population.
                Mutate(child0);
                Mutate(child1);
                m_childPop.Add(new Genome(child0));
                m_childPop.Add(new Genome(child1));
            }

            // Set the previous population to the current childPop.
            m_population = m_childPop;
        }

        yield return null;
    }
}
