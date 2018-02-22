using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class BehaviourSubtrees : MonoBehaviour {

    // Construct and return a GetHP-subtree.
    public Node GetHpTree (ShooterAgent agent, int threshold)
    {
        N_Selection hpTree = new N_Selection();
        N_DecFlip flipHealthCheck = new N_DecFlip();
        N_DecFlip flipGetHp = new N_DecFlip();

        flipHealthCheck.Child = new N_HealthThreshold(agent, threshold);
        flipGetHp.Child = new N_GotoHealthpack(agent);

        hpTree.AddChild(flipHealthCheck);
        hpTree.AddChild(flipGetHp);

        return hpTree;
    }

    
}


