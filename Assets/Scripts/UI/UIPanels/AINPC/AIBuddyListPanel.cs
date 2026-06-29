using System;
using System.Collections.Generic;
using BUD.AnimPose;
using Game.Avatar;
using GameData;
using GameData.Account;
using GameData.BaseInfo;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.CommonConfirm;
using UnityEngine;
using UnityEngine.UI;

public enum AIBuddyUIMode
{
    Normal,
    Sort,
}

public class AIBuddyListPanel : BasePanel<AIBuddyListPanel>
{
    [SerializeField] private CButton backButton;
    [SerializeField] private CButton switchOcBtn;
    [SerializeField] private CButton deleteOcBtn;
    [SerializeField] private CButton nextBtn;
    [SerializeField] private CButton preBtn;
    [SerializeField] private CButton goProfileBtn;
    [SerializeField] private CButton switchExitBtn;
    [SerializeField] internal Transform transBg;
    [SerializeField] private Transform avatarRoot;
    [SerializeField] private AvatarCameraController avatarCameraController;
    [SerializeField] private Transform buddyCharactersRoot;
    [SerializeField] private RawImage mainPreviewImg;
    [SerializeField] private GameObject emptyTips;
    [SerializeField] private AIBuddyAnimAvatar avatarPrefab;
    
    private CharacterWrap mainCharacterWrap;
    private PgcNpcIdleBehaviour mainPgcIdleBehav;
    private UgcNpcIdleBehaviour mainUgcIdleBehav;
    private AIBuddyInfo curSelectBuddyInfo;
    private List<AIBuddyPreviewItem> previewItems;
    private AIBuddyListRsp _buddyListRsp;
    private AIBuddyUIMode curUIMode = AIBuddyUIMode.Normal;
    private int currentPage = 0; // 当前页索引
    private int itemsPerPage = 4;// 每页的数量
  
    public override void OnCreate()
    {
        base.OnCreate();
        backButton.onClick.AddListener(OnBackBtnClick);
        switchOcBtn.onClick.AddListener(OnSwitchOcBtnClick);
        deleteOcBtn.onClick.AddListener(OnDeleteOcBtnClick);
        nextBtn.onClick.AddListener(OnNextBtnClick);
        preBtn.onClick.AddListener(OnPreBtnClick);
        goProfileBtn.onClick.AddListener(OnGoProfileBtnClick);
        switchExitBtn.onClick.AddListener(OnSwitchExitBtnClick);
        AccountDataManager.Inst.AddAIBuddyOpListener(OnAIBuddyOpChange);
        InitItemAndAvatar();
        InitUI();
   
        _buddyListRsp = AIBuddyDataManager.Inst.GetLocalAIBuddyListRsp();//先获取本地数据
        UpdatePage();
        AIBuddyDataManager.Inst.RequestAIBuddyList(OnBuddyListUpdate,OnGetBuddyListFail);
        MessageHelper.AddListener<AIBuddyInfoRsp>(MessageName.OnAIBuddyInfoUpdated, OnRefreshInfoSuccess);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (AccountDataManager.HasInstance)
        {
            AccountDataManager.Inst.RemoveAIBuddyOpListener(OnAIBuddyOpChange);
        }
        MessageHelper.RemoveListener<AIBuddyInfoRsp>(MessageName.OnAIBuddyInfoUpdated, OnRefreshInfoSuccess);

    }

    private void CreateAvatar(AIBuddyInfo buddyInfo)
    {
        var saveCharacterData =  CharacterData.DeserializeObject(buddyInfo.npc.npcAvatarJson);
        if (saveCharacterData != null)
        {
            mainCharacterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData,avatarRoot);
            var animationCtrl = mainCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarCameraController.RotateTarget = avatarRoot;
            mainPgcIdleBehav = mainCharacterWrap.Avatar.AddComponent<PgcNpcIdleBehaviour>();
            mainPgcIdleBehav.Init(animationCtrl);
            
            mainUgcIdleBehav = mainCharacterWrap.Avatar.AddComponent<UgcNpcIdleBehaviour>();
            var playerIkController = mainCharacterWrap.Avatar.GetComponent<AnimIKController>();
            mainUgcIdleBehav.Init(playerIkController);
            
        }
    }

    private void RefreshAvatar(AIBuddyInfo buddyInfo)
    {
        if (mainCharacterWrap == null)
        {
            CreateAvatar(buddyInfo);
        }
        else
        {
            var characterData =  CharacterData.DeserializeObject(buddyInfo.npc.npcAvatarJson);
            mainCharacterWrap.RefreshAvatar(characterData);
        }
        
        mainPgcIdleBehav.SetData(null);
        mainUgcIdleBehav.ResetData();
        
        if (buddyInfo.npc != null && buddyInfo.npc.npcAnimations != null && buddyInfo.npc.npcAnimations.Count > 0)
        {
            var npcInfo = buddyInfo.npc;
            bool isPgcRes = npcInfo.animResType == (int)AnimResType.PGC;
            var ikController = mainCharacterWrap.Avatar.GetComponent<AnimIKController>();
            ikController.ChangeAnimResType(isPgcRes ? AnimResType.PGC : AnimResType.UGC);
            ikController.RemovePropIks();
            if (isPgcRes)
            {
                mainPgcIdleBehav.SetData(npcInfo.npcAnimations);
                mainPgcIdleBehav.PlayMainAnim();
            }
            else
            {
                mainUgcIdleBehav.SetData(npcInfo.npcAnimations);
                mainUgcIdleBehav.PlayMainAnim();
            }
        }

    }

    private void InitUI()
    {
        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        string prefabPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab";
        var itemObj = Loader.Load<GameObject>(prefabPath).Instantiate(transBg);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#8860F8", atlasPath, new List<string>()
        {
            "avatar_icon_1", "avatar_icon_2", "avatar_icon_3","avatar_icon_4"
        });
        item.gameObject.SetActive(true);
        item.SetImagesColor(new Color(1,1,1,0.4f));
        emptyTips.SetActive(false);
        SetUIMode(AIBuddyUIMode.Normal);
        SetUISelect(false);
    }


    private void InitItemAndAvatar()
    {
        int diff = 500;//3D人物的间隔
        var previewItemArray = GetComponentsInChildren<AIBuddyPreviewItem>(true);
        previewItems = new List<AIBuddyPreviewItem>(previewItemArray);
        for (int i = 0; i < previewItems.Count; i++)
        {
            int index = i;
            var buddyAvatar = Instantiate(avatarPrefab, buddyCharactersRoot);
            buddyAvatar.transform.localPosition = new Vector3(index * diff,index * diff,0);
            var previewItem =  previewItems[index];
            previewItem.BindAnimAvatar(buddyAvatar);
            previewItem.AddItemClickListener(OnItemSelect);
            previewItem.AddSortClickListener(OnSortClick);
            previewItem.AddBuySlotClickListener(OnBuySlotClick);
        }
    }

    private void UpdatePage()
    {
        int startIndex = currentPage * itemsPerPage;

        // 处理页码范围内的卡位显示
        for (int i = 0; i < previewItems.Count; i++)
        {
            int slotIndex = startIndex + i;

            // 如果当前页的卡位索引小于总卡位数，显示相应数据
            if (slotIndex < _buddyListRsp.totalSlot)
            {
                
                AIBuddyInfo buddyInfo = slotIndex < _buddyListRsp.list.Count ? _buddyListRsp.list[slotIndex] : null;
                AIBuddyItemType itemType;

                if (slotIndex < _buddyListRsp.list.Count) // 有数据
                {
                    itemType = AIBuddyItemType.Added;
                }
                else if (slotIndex < _buddyListRsp.totalSlot) // 有卡位但无数据
                {
                    itemType = AIBuddyItemType.ToAdd;
                }
                else // 无卡位
                {
                    itemType = AIBuddyItemType.ToBuy;
                }

                previewItems[i].SetData(buddyInfo, itemType);
                if (buddyInfo != null && buddyInfo == curSelectBuddyInfo)
                {
                    previewItems[i].SetSelectWithoutNotify(true);
                }

                if (curUIMode == AIBuddyUIMode.Sort)
                {
                    previewItems[i].SetSortVisible((buddyInfo != curSelectBuddyInfo));
                }
                previewItems[i].SetUIMode(curUIMode);
            }
            else
            {
                // 超出范围，清空显示
                previewItems[i].SetData(null, AIBuddyItemType.ToBuy);
            }
        }

        // 更新按钮显示状态
        UpdateButtons();
    }
    
    private void UpdateButtons()
    {
        
        // 如果totalSlot是itemsPerPage的倍数，则增加一页ToAdd
        bool hasNextPage = (currentPage + 1) * itemsPerPage < 
                           (_buddyListRsp.totalSlot % itemsPerPage == 0 
                               ? _buddyListRsp.totalSlot + itemsPerPage 
                               : _buddyListRsp.totalSlot);
        // 是否有上一页
        bool hasPreviousPage = currentPage > 0;

        // 更新按钮状态
        nextBtn.gameObject.SetActive(hasNextPage);
        preBtn.gameObject.SetActive(hasPreviousPage);
    }

    private void SetUIMode(AIBuddyUIMode uiMode)
    {
        curUIMode = uiMode;
        if (uiMode == AIBuddyUIMode.Normal)
        {
            switchExitBtn.gameObject.SetActive(false);
            if (curSelectBuddyInfo != null)
            {
                SetUISelect(true);
            }
            SetItemsSortVisible(false);
        }
        else if(uiMode == AIBuddyUIMode.Sort)
        {
            switchExitBtn.gameObject.SetActive(true);
            SetUISelect(false);
            SetItemsSortVisible(true);
        }
        
        foreach (var previewItem in previewItems)
        {
            previewItem.SetUIMode(uiMode);
        }
    }

    private void SetUISelect(bool isSelect)
    {
        switchOcBtn.gameObject.SetActive(isSelect);
        goProfileBtn.gameObject.SetActive(isSelect);
        deleteOcBtn.gameObject.SetActive(isSelect);
    }


    private void OnAIBuddyOpChange(AIBuddyOp op,AIBuddyInfo aiBuddyInfo)
    {
        //当ai buddy发生变化时，重新拉取列表
        AIBuddyDataManager.Inst.RequestAIBuddyList(OnBuddyListUpdate,OnGetBuddyListFail);
    }


    private void OnBuddyListUpdate(AIBuddyListRsp buddyListRsp)
    {
        if (!this || buddyListRsp == null) return;
        _buddyListRsp = buddyListRsp;
        if (_buddyListRsp.list == null)
        {
            _buddyListRsp.list = new List<AIBuddyInfo>();
        }
        currentPage = 0;
        emptyTips.SetActive(buddyListRsp.list.Count == 0);
        mainPreviewImg.gameObject.SetActive(buddyListRsp.list.Count > 0);
        UpdatePage();
        
        //默认选中第一个
        if (buddyListRsp.list.Count > 0)
        {
            previewItems[0].SetSelect(true);
        }
    }

    private void OnGetBuddyListFail(string errData)
    {
        if (!this) return;
    }
    
    private void OnRefreshInfoSuccess(AIBuddyInfoRsp infoRsp)
    {
        if (!this) return;
        if (infoRsp == null || infoRsp.info == null)
        {
            LoggerUtils.Log("OnRefreshInfoSuccess aiBuddyInfo is null");
            return;
        }

        if (_buddyListRsp != null && _buddyListRsp.list != null)
        {
            for (int i = 0; i < _buddyListRsp.list.Count; i++)
            {
                var buddyInfo = _buddyListRsp.list[i];
                if (buddyInfo.id == infoRsp.info.id)
                {
                    _buddyListRsp.list[i] = infoRsp.info;
                    break;
                }
            }
        }
    }

    private void OnSortSucces(AIBuddyInfo fromInfo,AIBuddyInfo toInfo)
    {
        if (_buddyListRsp == null || _buddyListRsp.list == null) return;
        //交换列表位置，并刷新
        int fromIndex = _buddyListRsp.list.IndexOf(fromInfo);
        int toIndex = _buddyListRsp.list.IndexOf(toInfo);

        if (fromIndex == -1 || toIndex == -1) return; // 找不到时直接返回
        var temp = _buddyListRsp.list[fromIndex];
        _buddyListRsp.list[fromIndex] = _buddyListRsp.list[toIndex];
        _buddyListRsp.list[toIndex] = temp;
        AIBuddyDataManager.Inst.UpdateLocalAIBuddyListRsp(_buddyListRsp);
        UpdatePage();
    }

    private void OnDeleteBuddySuccess(AIBuddyInfo fromInfo)
    {
        if (_buddyListRsp == null || _buddyListRsp.list == null) return;
        if (_buddyListRsp.list.Contains(fromInfo))
        {
            _buddyListRsp.list.Remove(fromInfo);
            _buddyListRsp.usedSlot--;
            AIBuddyDataManager.Inst.UpdateLocalAIBuddyListRsp(_buddyListRsp);
        }
   
        
        //当前页第一个如果没数据，则翻到第一页
        if (previewItems[0].GetBindData()== null)
        {
            currentPage = 0;
        }
        emptyTips.SetActive(_buddyListRsp.list.Count == 0);
        mainPreviewImg.gameObject.SetActive(_buddyListRsp.list.Count > 0);
        UpdatePage();
        
        //默认选中第一个
        if (_buddyListRsp.list.Count > 0 && previewItems[0].GetBindData() != null)
        {
            previewItems[0].SetSelect(true);
        }
        
    }

    private void SetItemsSortVisible(bool isShow)
    {
        if (!isShow)
        {
            foreach (var previewItem in previewItems)
            {
                previewItem.SetSortVisible(false);
            }
        }
        else
        {
            foreach (var previewItem in previewItems)
            {
                var itemData = previewItem.GetBindData();
                if (itemData != null && itemData == curSelectBuddyInfo)
                {
                    previewItem.SetSortVisible(false);
                }
                else
                {
                    previewItem.SetSortVisible(true);
                }
            }
        }

        
    }

    private void UnSelectAll()
    {
        foreach (var previewItem in previewItems)
        {
            previewItem.SetSelectWithoutNotify(false);
        }
    }
    

    private void OnItemSelect(AIBuddyPreviewItem itemNode,AIBuddyInfo buddyInfo)
    {
        if(curSelectBuddyInfo == buddyInfo) return;
        UnSelectAll();
        if (buddyInfo != null)
        {
            curSelectBuddyInfo = buddyInfo;
            SetUISelect(true);
            RefreshAvatar(buddyInfo);
        }
    }
    
    private void OnSortClick(AIBuddyInfo buddyInfo)
    {
        if (curSelectBuddyInfo != null && buddyInfo != null)
        {
            AIBuddyDataManager.Inst.RequestAIBuddyResort(curSelectBuddyInfo.id,buddyInfo.id,(isSuccess)=>{
                if (isSuccess)
                {
                    OnSortSucces(curSelectBuddyInfo,buddyInfo);
                }
            });
        }
    }

    private void OnBuySlotClick()
    {
        var panel = UIManager.Inst.OpenPanel<BuyOcPanel>(PanelId.BuyOcPanel,true,SlotType.AIBuddySlot);
        panel.SetOnBuySuccessAct(() =>
        {
            //刷新列表
            AIBuddyDataManager.Inst.RequestAIBuddyList(OnBuddyListUpdate,OnGetBuddyListFail);
        });
    }

    private void OnSwitchOcBtnClick()
    {
        SetUIMode(AIBuddyUIMode.Sort);
    }

    private void OnSwitchExitBtnClick()
    {
        SetUIMode(AIBuddyUIMode.Normal);
    }

    private void OnDeleteOcBtnClick()
    {
        if (curSelectBuddyInfo == null)
        {
            TipPanel.ShowToast("请先选中要删除的伙伴");
            return;
        }

        CommonConfirmPanel commonConfirmPanel =
            UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        string buddyName = !string.IsNullOrEmpty(curSelectBuddyInfo.npc.npcName) ? curSelectBuddyInfo.npc.npcName : "";
        string contentText = LocalizationManager.Inst.GetLocalizedText("你确定要删除伙伴【{0}】吗？\n注意：删除伙伴会清空你们所有的聊天记录和亲密度等级。",buddyName);
        commonConfirmPanel.SetLocalText("删除伙伴",contentText , "删除", "取消");
        commonConfirmPanel.SetOnClickAction(
            () =>
            {
                AccountDataManager.Inst.DeleteAIBuddy(curSelectBuddyInfo, OnDeleteBuddySuccess);
            }, null);
    }



    private void OnNextBtnClick()
    {
        // 计算下一页是否超出了总卡位数量
        if ((currentPage + 1) * itemsPerPage <= _buddyListRsp.totalSlot)
        {
            currentPage++;
            UpdatePage();
        }
        // 产品需求：如果当前页已经是最后一页，且 totalSlot 是 itemsPerPage 的倍数，则添加额外的 ToAdd 页
        else if (_buddyListRsp.totalSlot % itemsPerPage == 0 && currentPage * itemsPerPage < _buddyListRsp.totalSlot)
        {
            currentPage++;
            UpdatePage();
        }
    }

    private void OnPreBtnClick()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdatePage();
        }
    }

    private void OnGoProfileBtnClick()
    {
        if (curSelectBuddyInfo != null)
        {
            UIManager.Inst.OpenPanel(PanelId.AIBuddyProfilePanel, curSelectBuddyInfo);
        }
    }

    private void OnBackBtnClick()
    {
        CloseSelf();
    }
}
