using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 口令列表单项：显示口令文本，点击后触发对应互动。
/// </summary>
public class AIBoxBuddyCommandItem : MonoBehaviour
{
    [SerializeField] private Text commandText;
    [SerializeField] private Button btn;

    public void SetData(voiceCommands command, Action<voiceCommands> onClick)
    {
        commandText.text = command.command;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick?.Invoke(command));
    }
}
