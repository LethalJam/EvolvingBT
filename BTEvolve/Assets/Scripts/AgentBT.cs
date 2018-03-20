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

        N_AgentNode.AgentType agent;

        if (StaticMethods.GetInstance().GetAgent0() == this.GetComponent<ShooterAgent>())
            agent = N_AgentNode.AgentType.agent0;
        else
            agent = N_AgentNode.AgentType.agent1;


        // Temporary code for testing out a simple BT
        N_Root testTree = new N_Root();
        N_Selection select = new N_Selection();

        N_Sequence seq = new N_Sequence();
        seq.AddLast(BehaviourSubtrees.Tree_PatrolOrKite(agent));
        seq.AddFirst(BehaviourSubtrees.Tree_ShootAtEnemy(agent));
        select.AddLast(BehaviourSubtrees.Tree_ReloadIfLow(agent, 5));
        select.AddLast(seq);

        testTree.Child = select;

        FileSaver.GetInstance().SaveTree(testTree, gameObject.name);
        N_Root loadedTree = FileSaver.GetInstance().LoadTree(gameObject.name);

        // Set the tree before being able to copy
        SetTree(loadedTree, agent);

    }

    // Simple function for setting the tree of the agent.
    // All trees are defined by their rootnode: N_Root.
    public void SetTree(N_Root treeRoot, N_AgentNode.AgentType agent)
    {
        btRoot = treeRoot;
        // Find all agentnodes and set their agent to be this one.
        List<Node> agentNodes = TreeOperations.RetrieveNodesOfType(treeRoot, typeof(N_AgentNode));
        foreach (N_AgentNode n in agentNodes)
        {
            n.SetAgent(agent);
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
}
