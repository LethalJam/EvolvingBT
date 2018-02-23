using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentResults
{
    public float damageGiven = 0.0f, damageTaken = 0.0f;
    public bool winner = false;
}

public class MatchSimulator : MonoBehaviour {

    // Public parameters
    [Tooltip("Attach corresponding agents to these variables.")]
    public GameObject agent0, agent1;
    [Tooltip("Determines the timescale of each simulation. 1 is default.")]
    [Range(0.1f, 100.0f)]
    public float simulationTimeScale = 1.0f;
    //public bool pauseOnStartup = false;
    public bool startGameOnStartup = false;
    // Public event called when match is over.
    public event EventHandler MatchOver;
    public float matchTime = 60.0f;

    // Privates
    // Positioner och rotationer.
    private Vector3 startPos_agent0, startPos_agent1;
    private Quaternion startRot_agent0, startRot_agent1;
    // Used for direct interfacing with functions of the agent.
    private ShooterAgent behave_agent0, behave_agent1;
    private AgentBT bt_agent0, bt_agent1;
    private AgentResults result_agent0, result_agent1;
    private bool m_matchInProgress = false;
    // Timer variables used for the match timing out when no agent is able to win.
    private float m_matchTimer = 0.0f;


    protected virtual void OnMatchOver(EventArgs args)
    {
        EventHandler handler = MatchOver;
        if (handler != null)
        {
            handler(this, args);
        }
    }

	void Awake ()
    {

        if (agent0 != null || agent1 != null)
        {
            // Set default values of agents when found.
            startPos_agent0 = agent0.transform.position;
            startPos_agent1 = agent1.transform.position;
            startRot_agent0 = agent0.transform.rotation;
            startRot_agent1 = agent1.transform.rotation;
            behave_agent0 = agent0.GetComponent<ShooterAgent>();
            behave_agent1 = agent1.GetComponent<ShooterAgent>();
            bt_agent0 = agent0.GetComponent<AgentBT>();
            bt_agent1 = agent1.GetComponent<AgentBT>();
            result_agent0 = new AgentResults();
            result_agent1 = new AgentResults();
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
    // Reset the parameters for the purpose of initiating a new match session.
    public void StartMatch()
    {
        ResetAgents();
        Time.timeScale = simulationTimeScale;
        m_matchInProgress = true;
        m_matchTimer = 0.0f;
    }
    // Reset the values for all agents.
    public void ResetAgents()
    {
        result_agent0 = new AgentResults();
        result_agent1 = new AgentResults();
        agent0.transform.SetPositionAndRotation(startPos_agent0, startRot_agent0);
        agent1.transform.SetPositionAndRotation(startPos_agent1, startRot_agent1);
        behave_agent0.ResetValues();
        behave_agent1.ResetValues();
    }

    // Set the behaviour trees of the agents.
    public void SetAgentBTs (N_Root bt0, N_Root bt1)
    {
        bt_agent0.SetTree(bt0);
        bt_agent1.SetTree(bt1);
    }

    // Contionously check the state of the agents/the match session.
    private void Update()
    {
        if (m_matchInProgress)
        {
            m_matchTimer += Time.deltaTime;
            // If either of the agents died, end the match.
            if (behave_agent0.StateOfAgent == ShooterAgent.AgentState.dead
                || behave_agent1.StateOfAgent == ShooterAgent.AgentState.dead
                || m_matchTimer >= matchTime)
            {
                if (m_matchTimer >= matchTime)
                    Debug.Log("Timed out!");

                m_matchInProgress = false;
                Time.timeScale = 0.0f;
                // Damage taken/given agent0
                result_agent0.damageTaken = behave_agent0.TotalDamageTaken;
                result_agent0.damageGiven = behave_agent1.TotalDamageTaken;
                // Damage taken/given agent1
                result_agent1.damageTaken = behave_agent1.TotalDamageTaken;
                result_agent1.damageGiven = behave_agent0.TotalDamageTaken;
                // Set who won
                result_agent0.winner = behave_agent0.StateOfAgent != ShooterAgent.AgentState.dead;
                result_agent1.winner = behave_agent1.StateOfAgent != ShooterAgent.AgentState.dead;
                // Invoke that a match is over.
                MatchOver.Invoke(this, new EventArgs());
            }
        }
    }

    // Get functions
    public bool MatchInProgress { get { return m_matchInProgress; } }
    public AgentResults Agent0Results { get { return result_agent0; } }
    public AgentResults Agent1Results { get { return result_agent1; } }
}
