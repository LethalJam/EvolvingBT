using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonEvolv : MonoBehaviour {

    public GeneticAlgorithm accessedAlgorithm;

    GameObject buttonCanvas;

    private void Awake()
    {
        buttonCanvas = GameObject.FindGameObjectWithTag("ButtonCanvas");
        if (buttonCanvas == null)
            Debug.LogError("No gameobject with tag ButtonCanvas was found");
    }

    public void StartEvolutionButton()
    {
        accessedAlgorithm.StartEvolution();
        buttonCanvas.SetActive(false);
    }
}
