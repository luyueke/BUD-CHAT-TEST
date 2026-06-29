using Com.TheFallenGames.OSA.Util.IO;
using System;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc: 姿势选择 Item，展示姿势封面图，支持选中高亮态
/// Date:26-04-01
/// </summary>
public class DraftBoxPoseItem : MonoBehaviour
{
    [SerializeField] private Button SelectBtn;                  // 点击选中按钮
    [SerializeField] private RemoteImageBehaviour CoverImage;   // 姿势封面图（远程加载）
    [SerializeField] private GameObject SelectedGo;             // 选中态高亮节点
    [SerializeField] private Text text;

    private string _emoteId;    // 姿势 ID
    private Action _onClick;    // 点击回调，由外部注入

    private void Awake()
    {
        SelectBtn.onClick.AddListener(OnClick);
    }

    /// <summary>初始化 Item 数据：绑定姿势 ID、加载封面图、注入点击回调，默认取消选中态</summary>
    public void SetData(string emoteId, string coverUrl ,string name, Action onClick)
    {
        _emoteId = emoteId;
        _onClick = onClick;
        SelectedGo.SetActive(false);

        if (!string.IsNullOrEmpty(coverUrl))
            CoverImage.Load(coverUrl);
        text.text = string.IsNullOrEmpty(name) ? string.Empty : (name.Length > 6 ? name.Substring(0, 5) + "…" : name);
    }

    /// <summary>切换选中高亮态</summary>
    public void SetSelected(bool isSelected)
    {
        SelectedGo.SetActive(isSelected);
    }

    /// <summary>转发点击事件给外部回调</summary>
    private void OnClick()
    {
        _onClick?.Invoke();
    }
}
