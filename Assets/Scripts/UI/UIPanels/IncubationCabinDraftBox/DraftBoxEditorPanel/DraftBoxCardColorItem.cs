using Es;
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc: 卡片颜色选择 Item，展示一个颜色色块，支持选中高亮态
/// Date:26-04-02
/// </summary>
public class DraftBoxCardColorItem : MonoBehaviour
{
    [SerializeField] private Button ColorBtn;      // 颜色按钮，image 背景色即为配置的颜色值
    [SerializeField] private Image ColorImage;      // 颜色按钮，image 背景色即为配置的颜色值
    [SerializeField] private GameObject SelectObj; // 选中态高亮节点
    [SerializeField] private GameObject vipIcon; // vip图标

    private Action _onClick;
    private DraftBoxCardColorConfig config;
    private void Awake()
    {
        ColorBtn.onClick.AddListener(OnClick);
    }

    /// <summary>初始化 Item：将配置的十六进制颜色设置到按钮背景，注入点击回调，默认取消选中态</summary>
    public void SetData(DraftBoxCardColorConfig data, Action onClick)
    {
        config = data;
        _onClick = onClick;
        SelectObj.SetActive(false);
        vipIcon.SetActive(data.VipLevel > 0);
        if (ColorUtility.TryParseHtmlString($"#{data.Color}", out Color colorstart))
            ColorImage.color = colorstart;
    }

    /// <summary>切换选中高亮态</summary>
    public void SetSelected(bool isSelected)
    {
        SelectObj.SetActive(isSelected);
    }

    private void OnClick()
    {
        if (config != null && config.VipLevel > 0 && !VipDataManager.Inst.isVip)
        {
            TipPanel.ShowToast("需要VIP月卡可用");
            return;
        }
        _onClick?.Invoke();
    }
}
