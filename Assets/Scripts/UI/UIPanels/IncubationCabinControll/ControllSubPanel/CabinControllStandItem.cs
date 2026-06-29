using Es;
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc: 控制台互动面板 - 待机动作列表 Item
///       展示动作名称，并提供预览按钮
/// Date: 26-04-09
/// </summary>
public class CabinControllStandItem : MonoBehaviour
{
    [SerializeField] private Text NameTxt;       // 动作名称文本
    [SerializeField] private Button PreviewBtn;  // 预览按钮

    /// <summary>点击预览按钮时触发，由父级面板绑定，最终通知 IncubationCabinControll 播放动作</summary>
    public Action onPreviewClick;

    private pEmoteData _data;

    // 绑定预览按钮点击事件
    void Awake()
    {
        PreviewBtn.onClick.AddListener(() => onPreviewClick?.Invoke());
    }

    /// <summary>
    /// 绑定数据并刷新显示
    /// </summary>
    public void Init(pEmoteData data)
    {
        _data = data;
        NameTxt.text = GetEmoteName(data);
    }

    /// <summary>
    /// 获取动作名称：PGC 从配置表查找，UGC 暂用占位文本
    /// </summary>
    private string GetEmoteName(pEmoteData data)
    {
        if (!string.IsNullOrEmpty(data.emoteId))
        {
            var config = DataTables.GetEmoAniConfigList()?.Find(x => x.emoId == data.emoteId);
            if (config != null && !string.IsNullOrEmpty(config.name))
                return config.name;
            return data.emoteId;
        }
        return "待机动作";
    }
}
