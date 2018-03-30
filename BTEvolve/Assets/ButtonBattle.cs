using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonBattle : MonoBehaviour {

    N_Root singleBT, multiBT;
    public string singleFilename;
    public string multiFilename;
    [Range(0.1f, 100.0f)]
    public float battleTimeScale = 1.0f;

    private bool battleOn = false;
    private MatchSimulator m_simulator;
    private GameObject m_buttonCanvas;
    private FeedbackText m_feedbackText;

    private void Awake()
    {
        m_simulator = GameObject.FindGameObjectWithTag("matchSimulator").GetComponent<MatchSimulator>();
        if (m_simulator == null)
            Debug.LogError("No matchsimulator found in ButtonBattle");
        if (m_simulator != null)
            m_simulator.MatchOver += MatchBattleOver;

        m_buttonCanvas = GameObject.FindGameObjectWithTag("ButtonCanvas");
        if (m_buttonCanvas == null)
            Debug.LogError("No gameobject with tag ButtonCanvas was found");

        m_feedbackText = GameObject.FindGameObjectWithTag("FeedbackText").GetComponent<FeedbackText>();
        if (m_feedbackText == null)
            Debug.LogError("No FeedbackText could be found in ButtonBattle");
    }

    // Load both trees and assign them to the match simulator.
    public void StartBattle()
    {
        singleBT = FileSaver.GetInstance().LoadTree(singleFilename);
        multiBT = FileSaver.GetInstance().LoadTree(multiFilename);

        if (singleBT == null || multiBT == null)
        {
            Debug.LogError("Couldn't find requested behaviourtrees.");
            m_feedbackText.SetText("There doesn't exist a file for each algorithm. Please evolve using one of each before battling.");
            return;
        }


        // Start match and set timescale
        battleOn = true;
        m_simulator.liveSimulationScale = battleTimeScale;
        m_simulator.SetAgentBTs(singleBT, multiBT);
        m_simulator.StartMatch();
        m_feedbackText.SetText("Battle started!");
        // Disable canvas while running
        m_buttonCanvas.SetActive(false);
    }

    private void MatchBattleOver(object sender, EventArgs args)
    {
        // Reset values when match is over
        if (battleOn)
        {
            m_buttonCanvas.SetActive(true);
            battleOn = false;
            m_simulator.liveSimulationScale = m_simulator.simulationTimeScale;

            AgentResults agent0 = m_simulator.Agent0Results;
            AgentResults agent1 = m_simulator.Agent1Results;

            if (agent0.winner && !agent1.winner)
                m_feedbackText.SetText("Single won!");
            else if (agent1.winner && !agent0.winner)
                m_feedbackText.SetText("Multi won!");
            else
                m_feedbackText.SetText("Battle was a tie...");


        }

    }
}
