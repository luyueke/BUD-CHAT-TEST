using DG.Tweening;
using Es;
using EventTracking;
using Game.Avatar;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;
using View.UI.PopupPanelSystem;
using View.UI.PopupPanelSystem.Data;
using View.UI.PopupPanelSystem.ExtendsPopups;


public class FittingRecommendationPopPanel : BasePanel<FittingRecommendationPopPanel>
{
    // UI组件
    public CButton closeButton;
    public CButton skitButton;
    public CButton confirmButton;
    public Transform labelContainer; // 标签容器
    public GameObject labelPrefab;   // 标签预制体
    public RectTransform layout;
    public RectTransform showView;
    public NewBieAvataConfig avataConfigs;
    [SerializeField] private AvatarCameraController avatarCameraController;
    public GameObject modelRoot; // 用于放置角色模型的根节点
    public Transform CButParent;

    public bool isStore = false;


    public int _currId;
    // 回调
    public Action OnPanelClose;

    SearchSettingSectionListRsp _data;
    int _classType;
    int _ugcType;
    // 标记面板是否已经关闭
    private bool _isClosed = false;

    // 保存已选择标签的ID列表
    public List<string> selectedLabelIds = new();

    string SendLabelInterface = "/recommend/setLabels";

    //展示标签
    public List<Text> texs;
    Dictionary<string, int> choesTexts;

    public Action closeFunc;

    private void Start()
    {
        OnCreate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(layout);
    }

    public override void OnCreate()
    {
        base.OnCreate();

        // -- Defensive code to prevent NullReferenceException and provide better error messages --
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmButtonClick);
        }
        choesTexts = new Dictionary<string, int>();
        // 初始化UI引用
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClick);
        }
        if (skitButton != null)
        {
            skitButton.gameObject.SetActive(false);
            skitButton.onClick.AddListener(OnCloseButtonClick);
            skitButton.onClick.AddListener(OnSkiClick);
        }
        var UIRoot = GameObject.Find("UIRoot");
        // 初始化时禁用确认按钮，直到用户选择了至少一个标签
        UpdateConfirmButtonState();
    }
    public void SetCallBack(Action callback)
    {
        OnPanelClose += callback;
    }
    protected override void OnDestroy()
    {
        PlayerPrefs.SetInt("FirstOpenAccurateRecommendation", 1);
        PlayerPrefs.Save();
        base.OnDestroy();

        // 移除监听器
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonClick);
        }
        if (skitButton != null)
        {
            skitButton.onClick.RemoveListener(OnCloseButtonClick);
            skitButton.onClick.RemoveListener(OnSkiClick);
        }
    }

    // 设置数据
    public void SetData(int classType, int ugcType)
    {
        _classType = classType;
        _ugcType = ugcType;
        // 清空已选择的标签列表
        selectedLabelIds.Clear();
        GetHttpData();

        // 初始时禁用确认按钮，直到有标签被选择
        UpdateConfirmButtonState();

    }
    void GetHttpData()
    {
        SearchLogicMgr.Inst.GetSearchSettingSectionList(_classType, _ugcType, (success, data) =>
        {
            if (success)
            {
                _data = data;
                UpdateLabels();
            }
        });
    }


    // 更新标签显示
    private void UpdateLabels()
    {
        // List<LabelData> labels
        if (_data == null || _data.List.Count == 0)
        {
            // 没有标签数据，隐藏标签相关UI
            if (labelContainer != null)
            {
                labelContainer.gameObject.SetActive(false);
            }
            return;
        }

        // 显示标签容器
        if (labelContainer != null)
        {
            labelContainer.gameObject.SetActive(true);

            // 创建新标签
            if (labelPrefab != null)
            {
                foreach (var label in _data.List)
                {
                    var labelObj = Instantiate(labelPrefab, labelContainer);
                    // 获取标签组件
                    var labelItem = labelObj.GetComponent<FittingLabelItem>();
                    if (labelItem != null)
                    {
                        labelItem.SetData(label);

                        // 设置标签点击事件
                        labelItem.OnLabelClicked = OnLabelClicked;

                        // 初始状态为未选中
                        if (_data.selectedIds.Contains(label.sectionId))
                        {
                            selectedLabelIds.Add(labelItem.GetData().sectionId);
                            labelItem.SetSelected(true);
                            UpdateConfirmButtonState();
                        }
                        else
                        {
                            labelItem.SetSelected(false);
                        }
                    }
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(layout);
            }
            else
            {
                LoggerUtils.LogError("标签预制体未设置，无法创建标签");
            }
        }
    }

    // 处理标签点击事件
    private void OnLabelClicked(FittingLabelItem labelItem)
    {

        if (labelItem == null) return;

        string labelId = labelItem.GetData().sectionId;

        // 检查是否已经选择了该标签
        bool isSelected = selectedLabelIds.Contains(labelId);

        if (isSelected)
        {
            // 如果已选择，则取消选择
            int index;
            if (choesTexts.TryGetValue(labelId, out index))
            {
                choesTexts.Remove(labelId);
                texs[index].text = "";
                texs[index].transform.parent.gameObject.SetActive(false);
            }
            selectedLabelIds.Remove(labelId);
            labelItem.SetSelected(false);
        }
        else
        {
            // 如果未选择，则选择
            selectedLabelIds.Add(labelId);
            for (int i = 0; i < texs.Count; i++)
            {
                if (texs[i].text == "")
                {
                    Text targetText = texs[i];
                    texs[i].transform.parent.gameObject.SetActive(true);
                    targetText.text = labelItem.GetData().sectionName;

                    // Reset scale and animate with DOTween
                    targetText.transform.localScale = Vector3.zero;
                    targetText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

                    choesTexts.Add(labelId, i);
                    break;
                }
            }
            labelItem.SetSelected(true);
        }

        // 更新确认按钮状态
        UpdateConfirmButtonState();
    }

    // 更新确认按钮状态
    private void UpdateConfirmButtonState()
    {
        skitButton.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(true);

        if (confirmButton != null)
        {
            // 如果有选择的标签，则启用按钮，否则禁用
            var key = selectedLabelIds.Count > 0;
            if (key != confirmButton.interactable)
            {
                confirmButton.interactable = key;
                confirmButton.GetComponent<Image>().color = key ? new Color(1f, 0.831f, 0f, 1f) : new Color(0.537f, 0.537f, 0.537f, 1f);
                confirmButton.transform.GetComponentInChildren<Text>().text = key ? "确定" : "至少选择1个兴趣";
            }
        }
    }

    void OnSkiClick()
    {
        CloseSelf();
    }
    // 关闭按钮点击事件
    private void OnCloseButtonClick()
    {
        CloseSelf();
    }

    // 确认按钮点击事件
    private void OnConfirmButtonClick()
    {
        // 确保有选择的标签
        if (selectedLabelIds.Count > 0)
        {
            SearchLogicMgr.Inst.SetSearchSettingSectionList(_classType, _ugcType, selectedLabelIds, null);
            CloseSelf();
        }

    }
    //animCtrl.SetPlayerState(PlayerState.ChangeClothes);
    public void InitCharacter(int _id)
    {
        _currId = _id;
        // 1. Destroy all previously created character parts under modelRoot.
        // This is simpler and more robust, as you suggested.
        if (modelRoot != null)
        {
            foreach (Transform child in modelRoot.transform)
            {
                Destroy(child.gameObject);
            }
        }
        else
        {
            return;
        }


        var avatarInfo = avataConfigs.configs.Find(x => x.id == _id);

        var saveCharacterData = CharacterData.DeserializeObject(avatarInfo.avataJason);
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, modelRoot.transform);
        avatarCameraController.RotateTarget = modelRoot.transform;
        var userInfo = new AccountUserInfo
        {
            avatarJson = avatarInfo.avataJason
        };
        // 新增：播放换装动画
        var animCtrl = characterWrapper.Avatar.GetComponent<PlayerAnimationCtrl>();
        if (animCtrl != null)
        {
            animCtrl.PlayerChangeClothesForUICharacer();
        }
        else
        {
            Debug.LogWarning("PlayerAnimationCtrl 组件未找到，无法播放换装动画");
        }
        // The following loop from the original code appears to be unused as `pgcIds` is empty.
        // It's kept here to maintain the original structure.
        var pgcIds = new List<string>()
        {
        };
        foreach (var pgcId in pgcIds)
        {
            var pgcConfig = PgcUtils.GetPgcConfigData(pgcId);
            var config = DataTables.GetAvatarCommonData(pgcId);
            var classType = UniqueType.GetAvatar(pgcId);
            characterWrapper.ChangePart(UniqueType.GetAvatar((AvatarSubType)pgcConfig.SubType), pgcId);
            characterWrapper.ChangeColor(classType, config.defaultColor);
            characterWrapper.Move(classType, config.pDef);
            characterWrapper.Rotate(classType, config.rDef);
            characterWrapper.Scale(classType, config.sDef);
            characterWrapper.HVScale(classType, config.vhSDef);
            characterWrapper.SetLeftOrRight(classType, config.leftRightType);
        }
    }
    public string SetPlayerAvata()
    {
        var avatarInfo = avataConfigs.configs.Find(x => x.id == _currId);
        return avatarInfo.avataJason;
        /*
        
        SetImageReq req = new SetImageReq();
        req.userInfo = new AccountUserInfo
        {
            avatarJson = avatarInfo.avataJason
        };
        req.setType = 4;
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setImage,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: arg0 =>
            {
                GetImageRes serverData = JsonConvert.DeserializeObject<GetImageRes>(arg0);

            }, onFail: arg0 =>
            {
            }, retryCount: 3);
        */
    }
    /// <summary>
    /// 使用DOTween动画，将showView从屏幕右侧滑入到其在编辑器中预设好的位置。
    /// </summary>
    /// <param name="animationDuration">动画持续时间（秒）。</param>
    public void ShowLabelSelectionViewAnimated(bool isShowView, float animationDuration = 0.5f)
    {
        if (!isShowView)
        {
            showView.gameObject.SetActive(false);
            return;
        }
        // 1. 激活GameObject，并记录下在编辑器中设置好的"最终位置"
        showView.gameObject.SetActive(true);
        Vector2 finalAnchoredPosition = showView.anchoredPosition;

        // 2. 计算屏幕外的起始位置
        // 我们需要它的父级容器来确定"屏幕外"是多远
        var parentRect = showView.parent as RectTransform;
        if (parentRect == null)
        {
            // 即使出错，也直接将它设置到最终位置，避免UI错乱
            showView.anchoredPosition = finalAnchoredPosition;
            return;
        }

        // 计算一个安全的、肯定在屏幕右侧之外的X坐标
        // 父容器宽度 + showView自身宽度的一半（确保整个view都在外面）
        float offscreenX = parentRect.rect.width + (showView.rect.width * (1 - showView.pivot.x));

        // 3. 立即将showView移动到屏幕外的起始位置
        // 注意：我们只改变X坐标，保持Y坐标不变，以实现平滑的水平滑入
        showView.anchoredPosition = new Vector2(offscreenX, finalAnchoredPosition.y);

        // 4. 使用DOTween创建动画，移动到我们记录好的最终位置
        showView.DOAnchorPos(finalAnchoredPosition, animationDuration)
                .SetEase(Ease.OutCubic); // 使用平滑的缓动函数
    }


    // 关闭面板
    private void CloseSelf()
    {
        if (_isClosed) return;

        _isClosed = true;


        closeFunc?.Invoke();
        // 关闭面板
        UIManager.Inst.ClosePanel(this);
        Destroy(this);
    }
}


