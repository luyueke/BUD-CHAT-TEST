using System;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorAvatarSelectInput : MonoBehaviour
{
    [SerializeField] private Button selectBtn;
    [SerializeField] private GameObject iconGObject;
    [SerializeField] private RemoteImageBehaviour avatarImage;

    private Action onSelectClicked;

    public void Init(Action onSelect)
    {
        onSelectClicked = onSelect;
        selectBtn?.onClick.RemoveAllListeners();
        selectBtn?.onClick.AddListener(() => onSelectClicked?.Invoke());
    }

    private const string NarratorImagePath = "Assets/Loadable/UI/UIPanel/OCTheatrePanel/Theatre_narration.png";

    /// <summary>
    /// 根据 avatarId + avatarType 从缓存中解析 URL 并刷新显示。
    /// avatarId 为空或等于旁白 id(1) 时显示旁白图片。
    /// </summary>
    public void Refresh(string avatarId, string avatarType, TheatreEditorDataCenter dataCenter)
    {
        bool isNarrator = avatarId == TheatreEditorDataCenter.NarratorAvatarId;
        if (isNarrator)
        {
            if (iconGObject != null) iconGObject.SetActive(false);
            if (avatarImage != null)
            {
                avatarImage.gameObject.SetActive(true);
                avatarImage.Load(NarratorImagePath);
            }
            return;
        }

        string url = ResolveUrl(avatarId, avatarType, dataCenter);
        bool hasAvatar = !string.IsNullOrEmpty(url);

        if (iconGObject != null) iconGObject.SetActive(!hasAvatar);
        if (avatarImage != null)
        {
            avatarImage.gameObject.SetActive(hasAvatar);
            if (hasAvatar) avatarImage.Load(url);
        }
    }

    private static string ResolveUrl(string avatarId, string avatarType, TheatreEditorDataCenter dataCenter)
    {
        if (dataCenter == null || string.IsNullOrEmpty(avatarId)) return null;
        if (!dataCenter.AvatarInfoCache.TryGetValue(avatarId, out var avatarInfo)) return null;

        if (!string.IsNullOrEmpty(avatarType) && avatarInfo.expressions != null)
        {
            int sep = avatarType.IndexOf('|');
            string targetName = sep >= 0 ? avatarType.Substring(0, sep) : avatarType;
            int targetMType = (sep >= 0 && int.TryParse(avatarType.Substring(sep + 1), out int m)) ? m : -1;

            foreach (var expr in avatarInfo.expressions)
            {
                if (expr.expressionName == targetName
                    && (targetMType < 0 || expr.mType == targetMType)
                    && !string.IsNullOrEmpty(expr.expressionURL))
                    return expr.expressionURL;
            }
        }

        if (avatarInfo.expressions != null && avatarInfo.expressions.Count > 0)
            return avatarInfo.expressions[0].expressionURL;

        return avatarInfo.cover;
    }
}
