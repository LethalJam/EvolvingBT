using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentBT : MonoBehaviour {

    private N_Root btRoot = null;

    private void Awake()
    {
        ShooterAgent agent = gameObject.GetComponent<ShooterAgent>();
        N_Root testTree = new N_Root();

        N_Sequence reloadSequence = new N_Sequence();
        reloadSequence.AddLast(BehaviourSubtrees.Tree_ShootAtEnemy(agent));
        reloadSequence.AddLast(BehaviourSubtrees.Tree_ReloadIfLow(agent, 5));
        testTree.Child = reloadSequence;
        //testTree.Child = BehaviourSubtrees.Tree_FollowEnemy(agent);
        //testTree.Child = BehaviourSubtrees.Tree_PatrolOrKite(agent);
        //testTree.Child = BehaviourSubtrees.Tree_PatrolOrKite(gameObject.GetComponent<ShooterAgent>());
        SetTree(testTree);
    }

    public void SetTree(N_Root treeRoot)
    {
        btRoot = treeRoot;
    }
    private void FixedUpdate ()
    {
        if (btRoot != null)
        {
            btRoot.Execute();
        }
    }
}
