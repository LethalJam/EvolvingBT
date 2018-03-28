﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GA_NSGA2 : GeneticAlgorithm {

    // Max is given by maxhp + all possible healthpacks in the game.
    private int damageTakenMin = 0, damageTakenMax = 400;
    private int damageGivenMin = 0, damageGivenMax = 400;

    // NSGA2 specific sub-populations
    private List<Genome> prevPop;
    private List<Genome> currentPop;

    #region DominationGenome
    // "Decorator" used for tracking dominated genomes and their relations
    public class DominationGenome
    {
        private Genome m_genome;
        private int m_dominationCount;
        private List<DominationGenome> m_dominatedGenomes;
        
        public DominationGenome(Genome genome)
        {
            m_genome = genome;
            m_dominationCount = 0;
            m_dominatedGenomes = new List<DominationGenome>();
        }

        public Genome Genome { get { return m_genome; } set { m_genome = value; } }
        public int DominationCount { get { return m_dominationCount; } set { m_dominationCount = value; } }
        public List<DominationGenome> DominatedGenomes { get { return m_dominatedGenomes; } }
    }
    #endregion

    protected new void Awake()
    {
        // Call inherited awake for initialization.
        base.Awake();
    }

    public void SortFitnessByNondomination(List<Genome> genomes)
    {
        List<DominationGenome> gByDom = new List<DominationGenome>();
        List<DominationGenome> prevFront = new List<DominationGenome>();
        List<DominationGenome> front = new List<DominationGenome>();

        foreach (Genome g in genomes)
        {
            gByDom.Add(new DominationGenome(g));
        }


        // First assign domination count and set of dominated genomes for each genome.
        for (int i = 0; i < gByDom.Count; i++)
        {
            for (int j = 0; j < gByDom.Count; j++)
            {
                Genome current = gByDom[i].Genome;
                Genome other = gByDom[j].Genome;

                // Check if other dominates current
                if ((other.DamageGiven >= current.DamageGiven && other.DamageTaken <= current.DamageTaken)
                    && (other.DamageGiven > current.DamageGiven || other.DamageTaken < current.DamageTaken))
                { // other dominates current
                    // Add to the dominationcount of current
                    gByDom[i].DominationCount++;
                }
                else if ((current.DamageGiven >= other.DamageGiven && current.DamageTaken <= other.DamageTaken)
                    && (current.DamageGiven > other.DamageGiven || current.DamageTaken < other.DamageTaken))
                { // current dominates other
                    // Add other to list of currents dominated genomes.
                    gByDom[i].DominatedGenomes.Add(gByDom[j]);
                }
            }

            // Identify the first front as the genomes that aren't dominated by any other genomes.
            if (gByDom[i].DominationCount == 0)
            {
                front.Add(gByDom[i]);
            }
        }

        // Lower rank (and thereby fitness) is better.
        int rank = 1;
        // Keep iterationg through each front while there is a next one.
        while (front.Count > 0)
        {
            prevFront = front;
            front = new List<DominationGenome>();
            // For all genomes in previous front
            foreach (DominationGenome dg in prevFront)
            {
                // Set fitness of the current front equal to the rank.
                dg.Genome.Fitness = rank;
                for (int i = 0; i < dg.DominatedGenomes.Count; i++)
                {
                    // Reduce the dominated genomes domination count by one and add it to the next front if
                    // it's at zero.
                    DominationGenome dominated = dg.DominatedGenomes[i];
                    dominated.DominationCount--;
                    if (dominated.DominationCount <= 0)
                        front.Add(dominated);
                }
            }
            // Increase rank to make fitness higher (worse) for every new front.
            rank++;
        }

    }

    public void CalculateCrowdingDistance(List<Genome> genomes)
    {
        // Calculate crowding distance by DamageTaken
        SortByDamagetaken(genomes);

        genomes[0].CrowdingDistance = genomes[genomes.Count - 1].CrowdingDistance = Mathf.Infinity;
        for (int i = 1; i < genomes.Count - 1; i++)
        {
            genomes[i].CrowdingDistance
                = genomes[i].CrowdingDistance + 
                (genomes[i + 1].DamageTaken - genomes[i - 1].DamageTaken) / (damageTakenMax - damageTakenMin);
        }


        // Calculate crowding distance by DamageGiven
        SortByDamageGiven(genomes);

        genomes[0].CrowdingDistance = genomes[genomes.Count - 1].CrowdingDistance = Mathf.Infinity;
        for (int i = 1; i < genomes.Count-1; i++)
        {
            genomes[i].CrowdingDistance
                = genomes[i].CrowdingDistance +
                (genomes[i + 1].DamageGiven - genomes[i - 1].DamageGiven) / (damageGivenMax - damageGivenMin);
        }
    }

    public void SortByDamagetaken(List<Genome> genomes)
    {
        for (int i = 1; i < genomes.Count; i++)
        {
            int j = i;
            while (j > 0 && genomes[j-1].DamageTaken > genomes[j].DamageTaken)
            {
                // Swap
                Genome back = genomes[j - 1];
                Genome front = genomes[j];
                genomes[j - 1] = front;
                genomes[j] = back;
                // Decrement inner
                j--;
            }
        }
    }

    public void SortByDamageGiven(List<Genome> genomes)
    {
        for (int i = 0; i < genomes.Count; i++)
        {
            int j = i;
            while (j > 0 && genomes[j-1].DamageGiven > genomes[j].DamageGiven)
            {
                // Swap
                Genome back = genomes[j - 1];
                Genome front = genomes[j];
                genomes[j - 1] = front;
                genomes[j] = back;
                // Decrement inner
                j--;
            }
        }
    }

    private Genome BinaryCrowdedTournamentSelect(List<Genome> genomes)
    {
        List<Genome> contestents = new List<Genome>();
        // First, randomize number of contestents.
        for (int i = 0; i < 2; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, genomes.Count);
            contestents.Add(genomes[randomIndex]);
        }

        Genome highestFit = contestents[0];
        // Find highest fitness among contestents
        foreach (Genome g in contestents)
        {
            if (g.Fitness < highestFit.Fitness) // Lower rank/fitness is better in this case.
                highestFit = g;
            else if (g.Fitness == highestFit.Fitness) // If they belong to the same front
            {
                // In that case, choose based on crowding distance instead.
                if (g.CrowdingDistance > highestFit.CrowdingDistance)
                    highestFit = g;
            }
        }

        // Create a copy of the best genome and return it.
        Genome copy = highestFit.GenomeCopy();
        copy.RootNode = StaticMethods.DeepCopy<N_Root>(highestFit.RootNode);

        return copy;
    }

    #region Evolution
    // Update is called once per frame
    public override IEnumerator Evolve()
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

        // First, process the initial population differently.

        Debug.Log("Starting simulation step of initial population...");
        // Start simulation and wait until done.
        simulationDone = false;
        StartCoroutine(Simulate());
        yield return new WaitUntil(() => simulationDone);
        Debug.Log("Simulation step of initial population complete!");

        // Assign fitness to each genome according to nondomination principle.
        SortFitnessByNondomination(m_population);
        CalculateCrowdingDistance(m_population);

        // Initial "next population" processing.
        while (m_childPop.Count < m_population.Count)
        {
            Genome parent0, parent1;
            parent0 = BinaryCrowdedTournamentSelect(m_population);
            parent1 = BinaryCrowdedTournamentSelect(m_population);

            // Combine parents to retrieve two children
            Genome child0, child1;
            Combine(out child0, out child1, parent0, parent1);

            Mutate(child0.RootNode);
            Mutate(child1.RootNode);
            m_childPop.Add(child0);
            m_childPop.Add(child1);
        }

        // Run general algorithm for remaining -th generations. (index 1 and forward
        for (int i = 1; i < generations; i++)
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
        FileSaver.GetInstance().SaveTree(bestGenome.RootNode, "multiEvolved");
        buttonCanvas.SetActive(true);

        yield return null;
    }
    #endregion
}
