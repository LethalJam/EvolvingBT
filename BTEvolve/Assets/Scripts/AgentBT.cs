using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentBT : MonoBehaviour {

    private N_Root btRoot = null;

    public void SetTree(N_Root treeRoot)
    {
        btRoot = treeRoot;
    }
    private void Update()
    {
        if (btRoot != null)
        {
            btRoot.Execute();
        }
    }
}
