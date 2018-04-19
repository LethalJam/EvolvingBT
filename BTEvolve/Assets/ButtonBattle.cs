using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonBattle : MonoBehaviour {

    N_Root singleBT, multiBT;
    public string singleFilename;
    public string multiFilename;
    [Range(0.1f, 100.0f)]
    public float battleTimeScale = 1.0f;
    public int simulationMatches = 100;

    // Public input fields
    public InputField matchField;
    public InputField simspeedField;

    private bool battleOn = false;
    private MatchSimulator m_simulator;
    private GameObject m_buttonCanvas;
    private FeedbackText m_feedbackText;

    // Match stats
    private int m_singleWon = 0;
    private int m_multiWon = 0;

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

    // Read fields and change variables
    public void ReadFields()
    {
        if (matchField.text.Length > 0)
            simulationMatches = int.Parse(matchField.text);
        if (simspeedField.text.Length > 0)
            battleTimeScale = float.Parse(simspeedField.text);
    }

    // Called by button to start the coroutine
    public void InitiateBattle()
    {
        StartCoroutine(StartBattle());
    }

    // Load both trees and assign them to the match simulator.
    private IEnumerator StartBattle()
    {
        singleBT = FileSaver.GetInstance().LoadTree(singleFilename);
        multiBT = FileSaver.GetInstance().LoadTree(multiFilename);

        // Reset matches won
        m_singleWon = m_multiWon = 0;

        if (singleBT == null || multiBT == null)
        {
            Debug.LogError("Couldn't find requested behaviourtrees.");
            m_feedbackText.SetText("There doesn't exist a file for each algorithm. Please evolve using one of each before battling.");
            yield return null;
        }

        // Disable button canvas before starting simulation
        m_buttonCanvas.SetActive(false);
        for (int i = 0; i < simulationMatches; i++)
        {
            m_feedbackText.SetText("Battle " + i);

            // Start match and set timescale
            battleOn = true;
            m_simulator.liveSimulationScale = battleTimeScale;

            // Start match and wait until over.
            m_simulator.SetAgentBTs(singleBT, multiBT);
            m_simulator.StartMatch();
            yield return new WaitUntil(() => !battleOn);
        }

        ScreenCapture.CaptureScreenshot("Resultat.png");
        m_feedbackText.SetText("Single won: " + m_singleWon + ", Multi won: " + m_multiWon);
        m_buttonCanvas.SetActive(true);
        // Reset simulation scale before yielding
        m_simulator.liveSimulationScale = m_simulator.simulationTimeScale;
        yield return null;
    }

    private void MatchBattleOver(object sender, EventArgs args)
    {
        // Reset values when match is over
        if (battleOn)
        {
            AgentResults agent0 = m_simulator.Agent0Results;
            AgentResults agent1 = m_simulator.Agent1Results;

            if (agent0.winner && !agent1.winner)
                m_singleWon++;
            else if (agent1.winner && !agent0.winner)
                m_multiWon++;

            battleOn = false;
        }

    }

}
