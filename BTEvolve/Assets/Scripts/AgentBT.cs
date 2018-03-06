using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class that runs the agent using its given BT
public class AgentBT : MonoBehaviour {

    private N_Root btRoot = null;
    private ShooterAgent m_agent;

    private void Awake()
    {
        m_agent = gameObject.GetComponent<ShooterAgent>();
        if (m_agent == null)
            Debug.Log("AgentBT not attached to agent GameObject.");


        // Temporary code for testing out a simple BT
        N_Root testTree = new N_Root();
        N_Selection select = new N_Selection();        

        N_Sequence seq = new N_Sequence();
        seq.AddLast(BehaviourSubtrees.Tree_PatrolOrKite(m_agent));
        seq.AddFirst(BehaviourSubtrees.Tree_ShootAtEnemy(m_agent));
        select.AddLast(BehaviourSubtrees.Tree_ReloadIfLow(m_agent, 5));
        select.AddLast(seq);

        testTree.Child = select;

        // Set the tree before being able to copy
        SetTree(testTree);

        testTree = GetCopy();

        SetTree(testTree);
    }

    // Simple function for setting the tree of the agent.
    // All trees are defined by their rootnode: N_Root.
    public void SetTree(N_Root treeRoot)
    {
        btRoot = treeRoot;
        // Find all agentnodes and set their agent to be this one.
        List<Node> agentNodes = TreeOperations.RetrieveNodesOfType(treeRoot, typeof(N_AgentNode));
        foreach (N_AgentNode n in agentNodes)
        {
            n.SetAgent(m_agent);
        }
    }

    // Run BT using timescaled FixedUpdate if a BT is attached.
    private void FixedUpdate ()
    {
        if (btRoot != null)
        {
            btRoot.Execute();
        }
    }

    // Deep copy tree and return new one.
    public N_Root GetCopy()
    {
        // Creat initial required variables.
        N_Root copyRoot = new N_Root();
        Queue<Node> nodeCopies = new Queue<Node>();

        // Start by copying initial composition.
        if (btRoot.Child.GetType() == typeof(N_ProbabilitySelector))
        {
            // Cast as probability selector.
            N_ProbabilitySelector probSelect = btRoot.Child as N_ProbabilitySelector;

            // Save children of probselect.
            List<Node> children = probSelect.GetChildren();

            // Create new instance of the node.
            N_ProbabilitySelector newSelector = new N_ProbabilitySelector();

            // For each child, attach it to the new selector and add it to the queue.
            foreach (Node n in children)
            {
                n.Parent = newSelector;
                newSelector.AddFirst(n, probSelect.GetProbabilityWeight(n));
                nodeCopies.Enqueue(n);
            }

            copyRoot.Child = newSelector;
        }
        else if (btRoot.Child.GetType().IsSubclassOf(typeof(N_CompositionNode)))
        {
            // Cast as composition.
            N_CompositionNode comp = btRoot.Child as N_CompositionNode;

            // Save children of comp.
            List<Node> children = comp.GetChildren();
            // Create new instance of the node.
            Type copyType = btRoot.Child.GetType();
            copyRoot.Child = Activator.CreateInstance(copyType) as Node;

            foreach (Node n in children)
            {
                n.Parent = copyRoot.Child;
                nodeCopies.Enqueue(n);
            }
            N_CompositionNode copiedComp = copyRoot.Child as N_CompositionNode;
            copiedComp.SetChildren(children);
        }


        while (nodeCopies.Count > 0)
        {
            // Get current node by dequeueing.
            Node current = nodeCopies.Dequeue();
            Node parentNode = current.Parent;
            // Create new instance of node.
            Type copyType = btRoot.Child.GetType();
            Node newNode = Activator.CreateInstance(copyType) as Node;

            // Update coupling to the parent.
            if (parentNode.GetType() == typeof(N_ProbabilitySelector))
            {
                N_ProbabilitySelector parentProb = parentNode as N_ProbabilitySelector;
                // Remove old and add new with the same prob weight.
                float prob = parentProb.GetProbabilityWeight(current);
                parentProb.RemoveChild(current);
                parentProb.AddFirst(newNode, prob);
            }
            else if (parentNode.GetType().IsSubclassOf(typeof(N_CompositionNode)))
            {
                N_CompositionNode parentComp = parentNode as N_CompositionNode;
                // Remove old
                parentComp.RemoveChild(current);
                // Add new
                parentComp.AddFirst(newNode);
            }
            else if (parentNode.GetType().IsSubclassOf(typeof(N_Decorator)))
            {
                N_Decorator parentDec = parentNode as N_Decorator;
                // Remove old and set new.
                parentDec.Child = newNode;
            }

            if (current.GetType() == typeof(N_ProbabilitySelector))
            {

            }
            // If current is a composition, add its children to the queue.
            else if (current.GetType().IsSubclassOf(typeof(N_CompositionNode)))
            {
                N_CompositionNode currentComp = current as N_CompositionNode;
                N_CompositionNode newComp = newNode as N_CompositionNode;
                List<Node> children = currentComp.GetChildren();

                // Add children to new comp and queue them up.
                foreach (Node n in children)
                {
                    newComp.AddFirst(n);
                    n.Parent = newComp;
                    nodeCopies.Enqueue(n);
                }
            }
        }

        return copyRoot;
    }
}
