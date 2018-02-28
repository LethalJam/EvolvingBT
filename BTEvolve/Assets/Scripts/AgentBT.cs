using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class that runs the agent using its given BT
public class AgentBT : MonoBehaviour {

    private N_Root btRoot = null;

    private void Awake()
    {
        ShooterAgent agent = gameObject.GetComponent<ShooterAgent>();
        if (agent == null)
            Debug.Log("AgentBT not attached to agent GameObject.");


        // Temporary code for testing out a simple BT
        N_Root testTree = new N_Root();
        N_Selection select = new N_Selection();        

        N_Sequence seq = new N_Sequence();
        seq.AddLast(BehaviourSubtrees.Tree_PatrolOrKite(agent));
        seq.AddFirst(BehaviourSubtrees.Tree_ShootAtEnemy(agent));
        select.AddLast(BehaviourSubtrees.Tree_ReloadIfLow(agent, 5));
        select.AddLast(seq);

        testTree.Child = select;

        SetTree(testTree);
    }

    // Simple function for setting the tree of the agent.
    // All trees are defined by their rootnode: N_Root.
    public void SetTree(N_Root treeRoot)
    {
        btRoot = treeRoot;
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
