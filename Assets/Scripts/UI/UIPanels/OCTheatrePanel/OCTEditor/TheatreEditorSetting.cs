using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorSetting : MonoBehaviour
{
    [SerializeField] private Button ExitBtn;
    [SerializeField] private Button SaveBtn;
    private Action onExit;
    private Action onSave;
    public void OnInit(Action onExit, Action onSave)
    {
        this.onExit = onExit;
        this.onSave = onSave;
        ExitBtn?.onClick.RemoveAllListeners();
        ExitBtn?.onClick.AddListener(() => this.onExit?.Invoke());
        SaveBtn?.onClick.RemoveAllListeners();
        SaveBtn?.onClick.AddListener(() => this.onSave?.Invoke());
    }

}
