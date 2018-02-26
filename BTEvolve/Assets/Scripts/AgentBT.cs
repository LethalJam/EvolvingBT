using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentBT : MonoBehaviour {

    private N_Root btRoot = null;

    private void Awake()
    {
        N_Root testTree = new N_Root();
        testTree.Child = BehaviourSubtrees.Tree_PatrolOrKite(gameObject.GetComponent<ShooterAgent>());
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
