using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FieldClamper : MonoBehaviour {

    private InputField m_field;

    public int min, max;

    private void Awake()
    {
        m_field = GetComponent<InputField>();
        if (m_field == null)
            Debug.LogError("No InputField found on gameobject.");
    }

    public void ClampAsInt ()
    {
        int current;
        bool parsed = int.TryParse(m_field.text, out current);

        if (parsed)
        {
            if (current > max)
                m_field.text = max.ToString();
            if (current < min)
                m_field.text = min.ToString();
        }
    }

    public void ClampAsFloat ()
    {
        float current;
        bool parsed = float.TryParse(m_field.text, out current);

        if (parsed)
        {
            if (current > max)
                m_field.text = max.ToString();
            if (current < min)
                m_field.text = min.ToString();
        }
    }
}
