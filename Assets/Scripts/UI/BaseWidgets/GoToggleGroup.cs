using System.Collections.Generic;
using UnityEngine;

public class GoToggleGroup : MonoBehaviour
{
    [SerializeField]
    private bool m_AllowSwitchOff = false;

    public bool allowSwitchOff
    {
        get => m_AllowSwitchOff;
        set => m_AllowSwitchOff = value;
    }

    private List<GoToggle> m_Toggles = new List<GoToggle>();

    public List<GoToggle> Toggles => m_Toggles;

    public void RegisterToggle(GoToggle toggle)
    {
        if (!m_Toggles.Contains(toggle))
        {
            m_Toggles.Add(toggle);
            toggle.isOn = m_Toggles.Count == 1;
        }
    }

    public void UnregisterToggle(GoToggle toggle)
    {
        m_Toggles.Remove(toggle);
        if (toggle.isOn && m_Toggles.Count>0)
        {
            m_Toggles[0].isOn = true;
        }
    }

    public void NotifyToggleOn(GoToggle activatedToggle)
    {
        foreach (var toggle in m_Toggles)
        {
            if (toggle != activatedToggle && toggle.isOn)
            {
                toggle.Set(false, true);
            }
        }
    }
}
