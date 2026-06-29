using System;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorAvatarItem : MonoBehaviour
{
    [SerializeField] private Button selectBtn;
    [SerializeField] private GameObject selectedObj;
    [SerializeField] private RemoteImageBehaviour avatarImg;
    [SerializeField] private Text nameText;

    private bool isSelected;

    /// <summary>
    /// 初始化演员 Item。图片加载由 Adapter 通过 RemoteImageBehaviour 进行（支持纹理池），此处仅设置文字与选中状态。
    /// </summary>
    public void Init(string avatarId, OCTheatreAvatarInfo avatarInfo, bool selected, Action<bool> onToggle)
    {
        isSelected = selected;

        if (nameText != null)
            nameText.text = avatarInfo?.name ?? "";

        selectedObj?.SetActive(isSelected);

        selectBtn?.onClick.RemoveAllListeners();
        selectBtn?.onClick.AddListener(() =>
        {
            isSelected = !isSelected;
            selectedObj?.SetActive(isSelected);
            onToggle?.Invoke(isSelected);
        });
    }

    /// <summary>
    /// 从外部强制刷新选中状态（例如超出上限时由 Adapter 回退）。
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        selectedObj?.SetActive(isSelected);
    }

    /// <summary>
    /// 在 OCTheatreAvatarInfo 的 expressions 中找出 expressionName=="常态" 的 URL；
    /// 找不到时回退到 cover。
    /// </summary>
    public static string ResolveDisplayUrl(OCTheatreAvatarInfo info)
    {
        if (info?.expressions != null)
        {
            foreach (var expr in info.expressions)
            {
                if (expr.expressionName == "常态" && !string.IsNullOrEmpty(expr.expressionURL))
                    return expr.expressionURL;
            }
        }
        return info?.cover;
    }
}
