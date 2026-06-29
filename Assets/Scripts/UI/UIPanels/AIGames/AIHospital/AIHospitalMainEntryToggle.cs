using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIHospitalMainEntryToggle : MonoBehaviour
{
    public GameObject _normal;
    public GameObject _select;
    // Start is called before the first frame update
    public void SetSelect(bool value)
    {
        _normal.SetActive(!value);
        _select.SetActive(value);
    }
}
