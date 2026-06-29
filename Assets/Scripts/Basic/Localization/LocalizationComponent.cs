using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalizationComponent : MonoBehaviour
{
    [Tooltip("localKey")]
    [TextArea(1, 3)] public string localizationKey;

    private Text textCom;
    private SuperTextMesh superTM;
    void Awake()
    {
        InitComponent();
        Translate();
    }


    private void InitComponent()
    {
        textCom = GetComponent<Text>();
        if (textCom == null)
        {
            superTM = GetComponent<SuperTextMesh>();
        }
    }

    public void Translate()
    {
        if (string.IsNullOrEmpty(localizationKey))
        {
            LoggerUtils.Log("Localization --> Localization key is not set...");
            return;
        }

        if (LocalizationManager.HasInstance && LocalizationManager.Inst.IsDefaultLang())
        {
            return;
        }


        if (textCom != null)
        {
            textCom.SetLocalText(localizationKey);
        }
        else if(superTM != null)
        {
            superTM.SetLocalText(localizationKey);
        }
        else
        {
            LoggerUtils.Log("Localization --> Text component is not found...");
        }
    }

#if UNITY_EDITOR

    public void Reset() {
        textCom = GetComponent<Text>();
        if (textCom != null) {
            localizationKey = textCom.text;
            return;
        }

        superTM = GetComponent<SuperTextMesh>();
        if (superTM != null) {
            localizationKey = superTM.text;
        }

    }
#endif

}
