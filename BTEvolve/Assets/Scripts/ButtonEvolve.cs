using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonEvolve : MonoBehaviour {

    public GeneticAlgorithm accessedAlgorithm;

    private GameObject m_buttonCanvas;

    private void Awake()
    {
        m_buttonCanvas = GameObject.FindGameObjectWithTag("ButtonCanvas");
        if (m_buttonCanvas == null)
            Debug.LogError("No gameobject with tag ButtonCanvas was found");
    }

    public void StartEvolutionButton()
    {
        accessedAlgorithm.StartEvolution();
        m_buttonCanvas.SetActive(false);
    }
}
