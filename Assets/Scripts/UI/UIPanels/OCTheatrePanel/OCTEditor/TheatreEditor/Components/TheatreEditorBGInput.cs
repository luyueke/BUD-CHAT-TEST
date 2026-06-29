using System;
using Com.TheFallenGames.OSA.Util.IO;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorBGInput : MonoBehaviour
{
    [SerializeField] private Button SelectBtn;
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private Button changeBtn;
    [SerializeField] private Button deleteBtn;
    [SerializeField] private GameObject hintGObject;
    [SerializeField] private GameObject hintGObject2;
    [SerializeField] private RemoteImageBehaviour BGImage;

    public Action onBoardOpen;
    public Action onBoardClose;

    private Action onSelectClicked;
    private Action onDeleteClicked;
    private int currentBGIndex = -1;
    private TheatreEditorDataCenter dataCenter;

    public void Init(Action onSelect, Action onDelete, TheatreEditorDataCenter dc)
    {
        onSelectClicked = onSelect;
        onDeleteClicked = onDelete;
        dataCenter = dc;

        SelectBtn?.onClick.RemoveAllListeners();
        SelectBtn?.onClick.AddListener(OnSelectBtnClicked);

        changeBtn?.onClick.RemoveAllListeners();
        changeBtn?.onClick.AddListener(OnChangeBtnClicked);

        deleteBtn?.onClick.RemoveAllListeners();
        deleteBtn?.onClick.AddListener(OnDeleteBtnClicked);

        optionPanel?.SetActive(false);
    }

    /// <summary>
    /// bgIndex: section.BackgroundUrl (0 = 未设置, >0 = AllBackgrounds 的 index 从1起，但数据实际是0-based index)
    /// 注意: BackgroundUrl 存的是 AllBackgrounds 的 index（0-based），0 表示使用第0张或未设置
    /// 根据需求说明 backgroundIndex 落地到 BackgroundUrl，0 = 未配置
    /// </summary>
    public void Refresh(int bgIndex)
    {
        currentBGIndex = bgIndex;
        bool hasBG = bgIndex > 0 && dataCenter != null
                     && bgIndex <= dataCenter.AllBackgrounds.Count
                     && !string.IsNullOrEmpty(dataCenter.AllBackgrounds[bgIndex - 1]);

        if (hintGObject != null) hintGObject.SetActive(!hasBG);
        if (hintGObject2 != null) hintGObject2.SetActive(!hasBG);

        if (BGImage != null)
        {
            BGImage.gameObject.SetActive(hasBG);
            if (hasBG) BGImage.Load(dataCenter.AllBackgrounds[bgIndex - 1]);
        }

        optionPanel?.SetActive(false);
    }

    public void CloseOptionPanel() => optionPanel?.SetActive(false);

    private void OnSelectBtnClicked()
    {
        if (currentBGIndex > 0)
        {
            optionPanel?.SetActive(true);
            onBoardOpen?.Invoke();
        }
        else
            onSelectClicked?.Invoke();
    }

    private void OnChangeBtnClicked()
    {
        optionPanel?.SetActive(false);
        onSelectClicked?.Invoke();
    }

    private void OnDeleteBtnClicked()
    {
        optionPanel?.SetActive(false);
        onBoardClose?.Invoke();
        onDeleteClicked?.Invoke();
    }

}
