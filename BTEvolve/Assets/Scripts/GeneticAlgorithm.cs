using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.UI;

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
        int damageTaken, damageGiven = 0;
        bool wonLastMatch = false;
        int fitness = 0;
        // Only used for NSGA2
        float crowdingDistance = 0.0f;

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
        public int Fitness { get { return fitness;  } set { fitness = value; } }
        public int DamageTaken { get { return damageTaken; } set { damageTaken = value; } }
        public float CrowdingDistance { get { return crowdingDistance; } set { crowdingDistance = value; } }
        public int DamageGiven { get { return damageGiven; } set { damageGiven = value; } }
        public bool WonLastMatch { get { return wonLastMatch;  } set { wonLastMatch = value; } }

    }
    #endregion

    [Header("Start algorithm on startup of program?")]
    public bool evolveOnStart = false;
    [Header("Adjust the chance for additional compositions being generated during randomization. [DEPRICATED]")]
    [Range(0.0f, 0.8f)]
    public float additionalCompChance = 0.3f;
    // Adjustable parameters.
    [Header("General settings")]
    [Tooltip("Set amount of generations for the execution.")]
    public int generations = 10;
    [Tooltip("Set size of the population for each generation.")]
    public int populationSize = 40;
    [Tooltip("Set the amount of subtrees within the genome.")]
    public int genomeSubtrees = 6;

    // Selection related
    [Header("Tournament Selection")]
    public int tourneyContestents = 4;

    // Combination related
    [Header("Combination")]
    [Tooltip("Set the probability of a random combination occuring.( 0 - 100%) ")]
    [Range(0.0f, 1.0f)]
    public float combinationRate = 0.5f;

    // Mutation related
    [Header("Mutation")]
    [Tooltip("Set the probability of a random mutation occuring.( 0 - 100%) ")]
    [Range(0.0f, 1.0f)]
    public float mutationRate = 0.05f;
    [Tooltip("Set the range of possible mutation offsets for thresholds")]
    [Range(-20, 0)]
    public int minThresholdOffset = -10;
    [Range(0, 20)]
    public int maxTresholdOffset = 10;
    [Range(5.0f, 100.0f)]
    [Tooltip("Increase or decrease probability based on a procentile offset relative to " +
        "the total weight of probabilities. ( 5 - 100%) ")]
    public float relativeProbabilityMutation = 5.0f;

    // Inputfield for generations
    public InputField generationField;

    // Protected variables used by all genetic algorithms of this solution.
    protected MatchSimulator m_simulator;
    protected List<Genome> m_population = new List<Genome>();
    protected List<Genome> m_childPop = new List<Genome>();
    protected Genome bestGenome;
    protected bool simulating = false;
    protected bool simulationDone = false;
    // Used for re-enabling interface after evolution
    protected GameObject buttonCanvas;
    protected FeedbackText feedbackText;

    #region Initialization (Awake/Start)
    // Make sure that common GA functions are at lowest protected, not private.
    protected void Awake()
    {
        feedbackText = GameObject.FindGameObjectWithTag("FeedbackText").GetComponent<FeedbackText>();
        if (feedbackText == null)
            Debug.LogError("No FeedbackText found in GeneticAlgorithm");
        // Initialize values
        m_simulator = GameObject.FindGameObjectWithTag("matchSimulator").GetComponent<MatchSimulator>();

        if (m_simulator != null)
            m_simulator.MatchOver += MatchSessionOver;
        else
            Debug.LogError("No matchsimulator was found in GeneticAlgorithm");

        buttonCanvas = GameObject.FindGameObjectWithTag("ButtonCanvas");
        if (buttonCanvas == null)
            Debug.LogError("No gameobject with tag ButtonCanvas was found");

        if (generationField == null)
            Debug.LogError("No Field for generation input was found");
        else
            generationField.text = generations.ToString();

        if (tourneyContestents > populationSize)
            tourneyContestents = populationSize;
    }

    // Methods for starting during Start initialization
    // or through publically accessable function.
    protected void Start()
    {
        if (evolveOnStart)
            StartCoroutine(Evolve());
    }
    public void StartEvolution()
    {
        // Attempt to set generations based on input field
        int inputGenerations;
        bool parsed = int.TryParse(generationField.text, out inputGenerations);
        if (parsed)
            generations = inputGenerations;

        StartCoroutine(Evolve());
    }

    #endregion

    protected void MatchSessionOver(object sender, EventArgs args)
    {
        simulating = false;
        //Debug.Log("Match over!");
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
            else if (randomChance < (additionalCompChance + 0.2f) && nestled)
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
                subtree = BehaviourSubtrees.Tree_GetHealthpackIfLow(N_AgentNode.AgentType.agent0, randomThreshold);
                break;
            case 1:
                randomThreshold = UnityEngine.Random.Range(0, 15);
                subtree = BehaviourSubtrees.Tree_ReloadIfLow(N_AgentNode.AgentType.agent0, randomThreshold);
                break;
            case 2:
                subtree = BehaviourSubtrees.Tree_ShootAtEnemy(N_AgentNode.AgentType.agent0);
                break;
            case 3:
                subtree = BehaviourSubtrees.Tree_Patrol(N_AgentNode.AgentType.agent0);
                break;
            case 4:
                subtree = BehaviourSubtrees.Tree_PatrolOrKite(N_AgentNode.AgentType.agent0);
                break;
            case 5:
                subtree = BehaviourSubtrees.Tree_TurnWhenShot(N_AgentNode.AgentType.agent0);
                break;
            case 6:
                subtree = BehaviourSubtrees.Tree_FollowEnemy(N_AgentNode.AgentType.agent0);
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

    #region Evolution

    // Simulate genomes against the currently best one and save results of match
    protected IEnumerator Simulate(List<Genome> genomes)
    {
        //Debug.Log("Simulating " + genomes.Count + " genomes.");
        // Simulate the genomes against the current best ones.
        foreach (Genome g in genomes)
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

    // Assign fitness values according to their performance in the simulation step
    protected void Evaluate()
    {
        bestGenome = m_population[0];
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
                bestGenome.RootNode = StaticMethods.DeepCopy<N_Root>(g.RootNode);
                bestGenome.Fitness = g.Fitness;
            }

        }
    }

    // Select two candidates for next generation based on their fitness values
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
                    parent0.RootNode = StaticMethods.DeepCopy<N_Root>(selectedTree.RootNode);
                    break;
                case 1:
                    parent1 = parentG;
                    parent1.RootNode = StaticMethods.DeepCopy<N_Root>(selectedTree.RootNode);
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

    // Select a genome using tournament.
    protected virtual Genome TournamentSelect(List<Genome> population)
    {
        List<Genome> contestents = new List<Genome>();
        // First, randomize number of contestents.
        for (int i = 0; i < tourneyContestents; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, population.Count);
            contestents.Add(population[randomIndex]);
        }

        Genome highestFit = contestents[0];
        // Find highest fitness among contestents
        foreach (Genome g in contestents)
        {
            if (g.Fitness > highestFit.Fitness)
                highestFit = g;
        }

        // Create a copy of the best genome and return it.
        Genome copy = highestFit.GenomeCopy();
        copy.RootNode = StaticMethods.DeepCopy<N_Root>(highestFit.RootNode);

        return copy;
    }

    // Combine parents and create two new children based on them
    protected void Combine (out Genome child0, out Genome child1, Genome parent0, Genome parent1)
    {
        Node subtree0, subtree1 = null;
        // First, check if there'll be any combination/crossover.
        float random = UnityEngine.Random.Range(0.0f, 1.0f);
        if (random < combinationRate)
        {
            List<Node> subtrees0 = TreeOperations.RetrieveSubtreeNodes(parent0.RootNode);
            List<Node> subtrees1 = TreeOperations.RetrieveSubtreeNodes(parent1.RootNode);

            // Select to random subtrees...
            int randomIndex = UnityEngine.Random.Range(0, subtrees0.Count);
            subtree0 = subtrees0[randomIndex];
            randomIndex = UnityEngine.Random.Range(0, subtrees1.Count);
            subtree1 = subtrees1[randomIndex];

            // Swap subtrees between 0 and 1.
            if (subtree0.Parent.GetType().IsSubclassOf(typeof(N_CompositionNode)))
            {
                N_CompositionNode comp0 = subtree0.Parent as N_CompositionNode;
                comp0.ReplaceChild(subtree0, StaticMethods.DeepCopy<Node>(subtree1));
            }

            // Swap subtrees between 1 and 0.
            if (subtree1.Parent.GetType().IsSubclassOf(typeof(N_CompositionNode)))
            {
                N_CompositionNode comp1 = subtree1.Parent as N_CompositionNode;
                comp1.ReplaceChild(subtree1, StaticMethods.DeepCopy<Node>(subtree0));
            }

            #region Old Combination
            //// Get initial comp of parent 0
            //N_CompositionNode comp0 = parent0.RootNode.Child as N_CompositionNode;
            //// Fetch a random subtree from the parent's subtrees
            //int randomIndex = UnityEngine.Random.Range(0, comp0.GetChildren().Count);
            //subtree0 = comp0.GetChildren()[randomIndex];

            //// Get initial comp of parent 1
            //N_CompositionNode comp1 = parent1.RootNode.Child as N_CompositionNode;
            //// Fetch a random subtree from the parent's subtrees
            //randomIndex = UnityEngine.Random.Range(0, comp1.GetChildren().Count);
            //subtree1 = comp1.GetChildren()[randomIndex];

            //// Swap subtrees between 0 and 1.
            //if (subtree0.Parent.GetType().IsSubclassOf(typeof(N_CompositionNode)))
            //{
            //    comp0.ReplaceChild(subtree0, StaticMethods.DeepCopy<Node>(subtree1));
            //}

            //// Swap subtrees between 1 and 0.
            //if (subtree1.Parent.GetType().IsSubclassOf(typeof(N_CompositionNode)))
            //{
            //    comp1.ReplaceChild(subtree1, StaticMethods.DeepCopy<Node>(subtree0));
            //}
            #endregion
        }

        // Regardless of combination, assign the children to be the parents. (combined or not)
        child0 = parent0;
        child1 = parent1;
    }

    // Assign random mutations to the given tree.
    protected void Mutate (N_Root child)
    {
        // First, check if there'll be any mutation at all.
        float random = UnityEngine.Random.Range(0.0f, 1.0f);
        if (random <  mutationRate)
        {
            List<Node> thresholds = TreeOperations.RetrieveNodesOfType(child, typeof(N_Threshold));
            List<Node> probNodes = TreeOperations.RetrieveNodesOfType(child, typeof(N_ProbabilitySelector));

            // Randomise between whether to change thresholds or probabilities
            random = UnityEngine.Random.Range(0.0f, 1.0f);
            // If no probnode exists, change condition if there are any instead
            if ((random <= 0.25f && thresholds.Count > 0))
            {
                //Debug.Log("Changing threshold!");
                // Get random index within range of list
                int index = UnityEngine.Random.Range(0, thresholds.Count);
                N_Threshold thresh = thresholds[index] as N_Threshold;

                // Mutate threshold by random offset
                int offset = UnityEngine.Random.Range(minThresholdOffset, maxTresholdOffset);
                thresh.SetThreshold(thresh.Threshold + offset);
            }
            else if (random > 0.25f && random <= 0.5f && probNodes.Count > 0) // Adjust relative probabilities on a probability selector.
            {
                //Debug.Log("Changing prob!");
                // Get random index within range of list
                int index = UnityEngine.Random.Range(0, probNodes.Count);
                N_ProbabilitySelector probSelect = probNodes[index] as N_ProbabilitySelector;

                // Check so that the probablitySelector has any children.
                if (probSelect.GetChildren().Count <= 0)
                    return;

                // Calculate total probability and retrieve a relative offset based on it.
                float totalProb = 0.0f;
                foreach (Node n in probSelect.GetChildren())
                {
                    totalProb += probSelect.GetProbabilityWeight(n);
                }
                float offset = totalProb * (relativeProbabilityMutation / 100.0f);
                // Also, get a random sign to decide whether to substract or add offset
                // and a random index for which child to be changed.
                float randomSign = Mathf.Sign(UnityEngine.Random.Range(-1.0f, 1.0f));
                int randomChildIndex = UnityEngine.Random.Range(0, probSelect.GetChildren().Count);

                // Finally, offset the given childs probability by the random amount.
                probSelect.OffsetProbabilityWeight(probSelect.GetChildren()[randomChildIndex]
                    , offset * randomSign);
            }
            else if (random > 0.5f && random <= 0.75f) // Change subtree
            {
                //Debug.Log("Changing subtree!");
                List<Node> subtrees = TreeOperations.RetrieveSubtreeNodes(child);
                int randomIndex = UnityEngine.Random.Range(0, subtrees.Count);
                Node subtree = subtrees[randomIndex];
                N_CompositionNode parentComp = subtree.Parent as N_CompositionNode;

                // Get a random subtree index and replace its position in the tree with
                // a new randomized subtree instead.
                parentComp.ReplaceChild(subtree, RandomSubtree());
            }
            else if (random > 0.75f) // Change composition
            {
                //Debug.Log("Changing composition!");
                List<Node> comps = TreeOperations.RetrieveNodesOfType(child, typeof(N_CompositionNode));
                int randomIndex = UnityEngine.Random.Range(0, comps.Count);
                N_CompositionNode replaceComp = comps[randomIndex] as N_CompositionNode;
                
                // If parent is null, this comp is the initial comp.
                if (replaceComp.Parent != null)
                {
                    Node compParent = replaceComp.Parent;
                    List<Node> children = replaceComp.GetChildren();

                    // Attach old comps children to the new one.
                    N_CompositionNode newComp = RandomComp();
                    foreach (Node c in children)
                    {
                        c.Parent = newComp;
                        newComp.AddLast(c);
                    }

                    // Reattach parent to the new comp
                    if (compParent.GetType().IsSubclassOf(typeof(N_CompositionNode)))
                    {
                        N_CompositionNode compParent_comp = compParent as N_CompositionNode;
                        compParent_comp.ReplaceChild(replaceComp, newComp);
                    }
                    else if (compParent.GetType().IsSubclassOf(typeof(N_Decorator)))
                    {
                        N_Decorator compParent_dec = compParent as N_Decorator;
                        compParent_dec.Child = newComp;
                    }

                }
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
        // Randomly generate the first best genome.
        bestGenome = RandomGenome();
        // Set the starting fitness of best to be the same as default fitness of 
        // population genomes.
        bestGenome.Fitness = 100;

        // Run algorithm for the given amount of generations.
        for (int i = 0; i < generations; i++)
        {
            feedbackText.SetText("Generation " + i + " out of " + generations + "...");
            // Start simulation and wait until done.
            simulationDone = false;
            StartCoroutine(Simulate(m_population));
            yield return new WaitUntil(() => simulationDone);

            // After simulation, start evaluation.
            Evaluate();

            // Add genomes to childpop until it's the same size as previous.
            while (m_childPop.Count < m_population.Count)
            {
                Genome parent0, parent1;
                // Select genomes given the results of simulation.
                //RouletteSelect(out parent0, out parent1);
                parent0 = TournamentSelect(m_population);
                parent1 = TournamentSelect(m_population);

                // Combine parents to retrieve two children.
                Genome child0, child1;
                Combine(out child0, out child1, parent0, parent1);

                // Finally, randomly mutate children and then add them to population.
                Mutate(child0.RootNode);
                Mutate(child1.RootNode);
                m_childPop.Add(child0);
                m_childPop.Add(child1);
            }

            // Set the previous population to the current childPop.
            m_population = m_childPop;
            // Reset childpop for next generaion.
            m_childPop = new List<Genome>();
        }
        // Save the final best tree.
        FileSaver.GetInstance().SaveTree(bestGenome.RootNode, "singleEvolved");
        feedbackText.SetText("Single evolution complete!");
        buttonCanvas.SetActive(true);

        yield return null;
    }

    #endregion

}
