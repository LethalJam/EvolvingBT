using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentResults
{
    public int damageGiven = 0, damageTaken = 0;
    public bool winner = false;
}

public class MatchSimulator : MonoBehaviour {

    // Public parameters
    [Tooltip("Attach corresponding agents to these variables.")]
    public GameObject agent0, agent1;
    [Tooltip("Determines the timescale of each simulation. 1 is default.")]
    [Range(0.1f, 100.0f)]
    public float simulationTimeScale = 1.0f;
    [Range(0.1f, 100.0f)]
    public float liveSimulationScale = 1.0f;
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
    // Hp relevant values
    private List<Vector3> m_hpPositions = new List<Vector3>();
    private GameObject m_hpPrefab;

    #region Handlers for various events
    protected virtual void OnMatchOver(EventArgs args)
    {
        EventHandler handler = MatchOver;
        if (handler != null)
        {
            handler(this, args);
        }
    }
    #endregion

    private GameObject[] GetHealthPacks ()
    {
        return GameObject.FindGameObjectsWithTag("healthPack");
    }

    void Awake ()
    {
        // Find prefab of healthPack for respawning purposes.
        m_hpPrefab = Resources.Load("healthPack") as GameObject;
        if (m_hpPrefab == null)
            Debug.LogError("No healthPack prefab found in resources.");
        // Save spawning positions of healthpacks
        GameObject[] hpArray = GetHealthPacks();
        foreach(GameObject hp in hpArray)
        {
            m_hpPositions.Add(hp.transform.position);
        }

        liveSimulationScale = simulationTimeScale;
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
        ResetHp();
        ResetAgents();
        Time.timeScale = simulationTimeScale;
        m_matchInProgress = true;
    }

    // Reset the amounnt and positions of healthpacks
    public void ResetHp()
    {
        // First, remove old healthPacks
        GameObject[] healthPacks = GetHealthPacks();
        foreach (GameObject hp in healthPacks)
        {
            Destroy(hp);
        }
        // Then, spawn new ones.
        foreach (Vector3 hpVec in m_hpPositions)
        {
            GameObject newHp = Instantiate(m_hpPrefab);
            newHp.transform.position = hpVec;
        }
    }
    // Reset the values for all agents.
    public void ResetAgents()
    {
        // Create new sheets of results for next match.
        result_agent0 = new AgentResults();
        result_agent1 = new AgentResults();

        // Reset the position and rotation of agents.
        behave_agent0.SetNavpos(startPos_agent0);
        behave_agent0.SetRot(startRot_agent0);
        behave_agent1.SetNavpos(startPos_agent1);
        behave_agent1.SetRot(startRot_agent1);

        // Reset values on behaviours of agents.
        behave_agent0.ResetValues();
        behave_agent1.ResetValues();
    }

    // Set the behaviour trees of the agents.
    public void SetAgentBTs (N_Root bt0, N_Root bt1)
    {
        bt_agent0.SetTree(bt0, N_AgentNode.AgentType.agent0);
        bt_agent1.SetTree(bt1, N_AgentNode.AgentType.agent1);
    }


    private void Update()
    {
        // Change the live simulation scale using the given variable.
        if (liveSimulationScale != Time.timeScale && m_matchInProgress)
            Time.timeScale = liveSimulationScale;
    }
    // Contionously check the state of the agents/the match session.
    private void FixedUpdate()
    {
        if (m_matchInProgress)
        {
            m_matchTimer += Time.deltaTime;
            // If either of the agents died, end the match.
            if (m_matchTimer >= matchTime)
            {
                if (m_matchTimer >= matchTime)
                    Debug.Log("Timed out at " + m_matchTimer + " sec.");

                EndMatch();
            }
        }
    }

    public void EndMatch()
    {
        m_matchInProgress = false;
        Time.timeScale = 0.0f;
        m_matchTimer = 0.0f;
        // Damage taken/given agent0
        result_agent0.damageTaken = behave_agent0.TotalDamageTaken;
        result_agent0.damageGiven = behave_agent1.TotalDamageTaken;
        // Damage taken/given agent1
        result_agent1.damageTaken = behave_agent1.TotalDamageTaken;
        result_agent1.damageGiven = behave_agent0.TotalDamageTaken;
        // Set who won If both win => draw? GA determines this.
        result_agent0.winner = behave_agent0.StateOfAgent != ShooterAgent.AgentState.dead;
        result_agent1.winner = behave_agent1.StateOfAgent != ShooterAgent.AgentState.dead;
        // Invoke that a match is over.
        MatchOver.Invoke(this, new EventArgs());
    }

    // Get functions
    public bool MatchInProgress { get { return m_matchInProgress; } }
    public AgentResults Agent0Results { get { return result_agent0; } }
    public AgentResults Agent1Results { get { return result_agent1; } }
}
