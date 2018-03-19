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

        // Copy non-node values and return genome copy.
        public Genome GenomeCopy()
        {
            Genome copy = new Genome();
            copy.damageGiven = damageGiven;
            copy.damageTaken = damageTaken;
            copy.fitness = fitness;
            copy.wonLastMatch = wonLastMatch;

            return copy;
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
    [Range(0.0f, 1.0f)]
    public float combinationRate = 0.5f;

    // Protected variables used by all genetic algorithms of this solution.
    protected MatchSimulator m_simulator;
    protected List<Genome> m_population = new List<Genome>();
    protected List<Genome> m_childPop = new List<Genome>();
    protected Genome bestGenome;
    protected bool simulating = false;
    protected bool simulationDone = false;

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
    protected Genome RandomGenome()
    {
        N_Root root = new N_Root();
        Genome randomGenome = new Genome();
        
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

            // Lastly, add a random subtree to the current composition as
            // well as to the list of subtree roots.
            Node subTree = RandomSubtree();
            randomGenome.SubRoots.Add(subTree);
            currentComp.AddLast(subTree);
        }

        root.Child = firstComp;
        randomGenome.RootNode = root;

        return randomGenome;
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
            g.Fitness = 100;
            // If the genome won the match and it wasn't a draw (assuming 0 damage given is a draw)
            // set fitness to 150.
            if (g.WonLastMatch && g.DamageGiven > 0)
                g.Fitness += 150;
            g.Fitness -= g.DamageTaken;
            g.Fitness += g.DamageGiven;

            // Make the floor of all fitness values 0 to make the roulette selection valid.
            if (g.Fitness < 0)
                g.Fitness = 0;

            // Find the genome with highest fitness, making sure that the simulations are always
            // compared to the currently best genome.
            if (g.Fitness > bestGenome.Fitness)
            {
                List<Node> noroots = new List<Node>();
                bestGenome.RootNode = TreeOperations.GetCopy(g.RootNode, null, g.SubRoots,  out noroots);
                bestGenome.Fitness = g.Fitness;
            }

        }
    }

    protected void RouletteSelect(out Genome parent0, out Genome parent1)
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
            Genome selectedTree = null;
            for (int j = 0; j < genomeToWeight.Count; j++)
            {
                var current = genomeToWeight.ElementAt(j);
                if (j == 0)
                {
                    if (random < current.Value)
                        selectedTree = current.Key;
                }
                else
                {
                    var prev = genomeToWeight.ElementAt(j - 1);
                    if (random < current.Value && random > prev.Value)
                    {
                        selectedTree = current.Key;
                    }
                }
            }
            if (selectedTree == null)
                Debug.LogError("Selected tree is null...");

            Genome parentG = selectedTree.GenomeCopy();

            // Assign parents during different iterations.
            switch (i) // Make sure to also copy conditions and subroots 
            {
                case 0:
                    parent0 = parentG;
                    List<Node> newSubroots0 = new List<Node>();
                    parent0.RootNode = TreeOperations.GetCopy(selectedTree.RootNode, null, 
                        selectedTree.SubRoots, out newSubroots0);
                    parent0.SubRoots = newSubroots0;
                    parent0.Conditions = TreeOperations.RetrieveNodesOfType(parent0.RootNode, typeof(N_Condition));
                    break;
                case 1:
                    parent1 = parentG;
                    List<Node> newSubroots1 = new List<Node>();
                    parent1.RootNode = TreeOperations.GetCopy(selectedTree.RootNode, null,
                        selectedTree.SubRoots, out newSubroots1);
                    parent1.SubRoots = newSubroots1;
                    parent1.Conditions = TreeOperations.RetrieveNodesOfType(parent1.RootNode, typeof(N_Condition));
                    break;
                default:
                    parent0 = new Genome();
                    parent1 = new Genome();
                    Debug.LogError("No parent was set in roulette");
                    break;
            }

        }

        if (parent0 == null || parent1 == null)
        {
            Debug.LogError("Parents weren't assigned during roulette selection!");
        }
    }

    // Parents are copies of trees in the population.
    protected void Combine (out Genome child0, out Genome child1, Genome parent0, Genome parent1)
    {
        Debug.Log("Combining!!");
        Node subtree0, subtree1 = null;

        // First, check if there'll be any combination/crossover.
        float random = UnityEngine.Random.Range(0.0f, 1.0f);
        if (random > combinationRate)
        {
            // Fetch to random subtrees from the parents.
            int randomIndex = UnityEngine.Random.Range(0, parent0.SubRoots.Count);
            subtree0 = parent0.SubRoots.ElementAt(randomIndex);

            randomIndex = UnityEngine.Random.Range(0, parent1.SubRoots.Count);
            subtree1 = parent1.SubRoots.ElementAt(randomIndex);

            // Swap subtrees between 0 and 1.
            if (subtree0.Parent.GetType().IsSubclassOf(typeof(N_CompositionNode)))
            {
                N_CompositionNode compRoot = subtree0.Parent as N_CompositionNode;
                compRoot.ReplaceChild(subtree0, subtree1);
            }
            else if (subtree0.Parent.GetType().IsSubclassOf(typeof(N_Decorator)))
            {
                N_Decorator decRoot = subtree0.Parent as N_Decorator;
                decRoot.Child = subtree1;
                subtree1.Parent = decRoot;
                // If subtree0s parent is still set to old one, set it to null instead.
                if (subtree0.Parent == decRoot)
                    subtree0.Parent = null;
            }
            // Update subroots list.
            parent0.SubRoots.Remove(subtree0);
            parent0.SubRoots.Add(subtree1);

            // Swap subtrees between 1 and 0.
            if (subtree1.Parent.GetType().IsSubclassOf(typeof(N_CompositionNode)))
            {
                N_CompositionNode compRoot = subtree1.Parent as N_CompositionNode;
                compRoot.ReplaceChild(subtree1, subtree0);
            }
            else if (subtree1.Parent.GetType().IsSubclassOf(typeof(N_Decorator)))
            {
                N_Decorator decRoot = subtree1.Parent as N_Decorator;
                decRoot.Child = subtree0;
                subtree0.Parent = decRoot;
                // If subtree1s parent is still set to old one, set it to null instead.
                if (subtree1.Parent == decRoot)
                    subtree1.Parent = null;
            }
            parent1.SubRoots.Remove(subtree1);
            parent1.SubRoots.Add(subtree0);

        }
        
        // Regardless of combination, assign the children to be the parents. (combined or not)
        child0 = parent0;
        child1 = parent1;
    }
    protected void Mutate (N_Root child)
    {
        // First, check if there'll be any mutation at all.
        float random = UnityEngine.Random.Range(0.0f, 1.0f);
        if (random >  mutationRate)
        {
            // Randomise between whether to change thresholds or add new nodes.
            random = UnityEngine.Random.Range(0.0f, 1.0f);
            if (random > 0.5f) // Change threshold
            {

            }
            else // Add new node
            {

            }

        }

    }

    // Function for starting the actual evolutionary progress.
    public virtual IEnumerator Evolve()
    {
        // Start by randomly generating the initial population.
        for (int i = 0; i < populationSize; i++)
        {
            m_population.Add(RandomGenome());
        }
        // Randomly generate the first best genome
        bestGenome = RandomGenome();
        // Set the starting fitness of best to be the same as default fitness of 
        // population genomes.
        bestGenome.Fitness = 100;

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
                Genome parent0, parent1;
                // Select genomes given the results of simulation.
                RouletteSelect(out parent0, out parent1);

                Genome child0, child1;
                // Combine parents to retrieve two children.
                Combine(out child0, out child1, parent0, parent1);

                // Finally, randomly mutate children and then add them to population.
                Mutate(child0.RootNode);
                Mutate(child1.RootNode);
                m_childPop.Add(child0);
                m_childPop.Add(child1);
            }

            // Set the previous population to the current childPop.
            m_population = m_childPop;
        }

        yield return null;
    }
}
