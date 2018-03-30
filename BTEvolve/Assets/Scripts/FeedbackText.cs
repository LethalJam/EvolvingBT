using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackText : MonoBehaviour {

    private Text m_uiText;

    private void Awake()
    {
        m_uiText = GetComponent<Text>();
        m_uiText.text = "Welcome!";
    }

    public void SetText(string text)
    {
        m_uiText.text = text;
    }
}
