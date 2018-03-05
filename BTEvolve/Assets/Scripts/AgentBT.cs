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

        // Start by copying roots child and adding its children to the queue.
        //nodeCopies.Enqueue(copyRoot.Child);
        Type copyType = btRoot.Child.GetType();

        if (btRoot.Child.GetType().IsSubclassOf(typeof(N_CompositionNode)))
        {
            // Cast as composition.
            N_CompositionNode comp = btRoot.Child as N_CompositionNode;

            // Save children as comp.
            List<Node> children = comp.GetChildren();
            // Create new instance of the node.
            copyRoot.Child = Activator.CreateInstance(copyType) as Node;
            foreach (Node n in children)
            {
                n.Parent = copyRoot.Child;
                nodeCopies.Enqueue(n);
            }
            N_CompositionNode copiedComp = copyRoot.Child as N_CompositionNode;
            copiedComp.SetChildren(children);
        }



        //while (nodeCopies.Count > 0)
        //{
        //    Node current = nodeCopies.Dequeue();
        //    if (current.GetType().IsSubclassOf(typeof(N_CompositionNode)))
        //    {

        //    }
        //}

        return copyRoot;
    }
}
