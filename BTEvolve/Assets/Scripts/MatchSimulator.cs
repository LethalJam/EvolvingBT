using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchSimulator : MonoBehaviour {

    // Public parameters
    [Tooltip("Attach corresponding agents to these variables.")]
    public GameObject agent0, agent1;
    [Tooltip("Determines the timescale of each simulation. 1 is default.")]
    [Range(0.1f, 100.0f)]
    public float simulationTimeScale = 1.0f;
    //public bool pauseOnStartup = false;
    public bool startGameOnStartup = false;

    // Privates
    private Vector3 startPos_agent0, startPos_agent1;
    private bool m_matchInProgress = false;

	void Awake ()
    {

        if (agent0 != null || agent1 != null)
        {
            startPos_agent0 = agent0.transform.position;
            startPos_agent1 = agent1.transform.position;
        }
        else
            Debug.LogError("MatchSimulator missing agents.");
        if (startGameOnStartup)
        {
            StartMatch();
        }
        else
            Time.timeScale = 0.0f;

    }

    public void StartMatch()
    {
        Time.timeScale = simulationTimeScale;
        m_matchInProgress = true;
    }

}
