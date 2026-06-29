using System;
using System.Collections.Generic;
using System.Linq;
using Game.Audio;
using Game.Base;
using Game.CommunityGame;
using Game.PropStore;
using Game.Store;
using GameData;
using GameData.BaseInfo;
using GameData.UGCData;
using Message;
using UGCAsset;
using UI.Base;
using UI.BaseWidgets;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;

public class UGCMusicScoreEditPanel : BasePanel<UGCMusicScoreEditPanel>
{
    private MusicScoreInfo currentMusicScoreInfo;
    private CButton backButton;
    private LoadingButton saveButton;
    private CButton editInfoButton;
    private CButton tryPlayButton;
    private CButton addPartButton;
    private CButton deletePartButton;
    private Text deletePartButtonText;
    private CButton editBPMButton;
    private Text editBPMText;
    private Transform editSyllableContent;
    [SerializeField]private GameObject editSyllableprefab;
    [SerializeField] private Transform _trans_Bg;
    private List<EditSyllableItem> editSyllableItemList = new List<EditSyllableItem>();
    private const int partSyllableCount = 16;//一页的音节数
    private int curPartIndex = 0;//乐谱页数
    private List<MusicScorePartItem> partItems = new List<MusicScorePartItem>();
    private SelectSyllableView _selectSyllableView;
    [SerializeField]private GameObject partItemPrefab;
    private Transform partItemContent;

    public AddPartTipView addPartTipView;
    public DeletePartTipView deletePartTipView;
    public EditBPMView editBPMView;
    //是否选中了删除小节按钮
    private bool isDeletePartSelect;
    private const string TAG = "UGCMusicScoreEditPanel";
    private GoodsData curPreviewInstrumentData;
    public override void OnCreate()
    {
        
        backButton = GameObjectEx.FindComponentByName<CButton>(transform,"BackButton");
        saveButton = GameObjectEx.FindComponentByName<LoadingButton>(transform,"SaveButton");
        editInfoButton = GameObjectEx.FindComponentByName<CButton>(transform,"EditInfoButton");
        tryPlayButton = GameObjectEx.FindComponentByName<CButton>(transform,"TryPlayButton");
        addPartButton = GameObjectEx.FindComponentByName<CButton>(transform,"AddPartButton");
        deletePartButton = GameObjectEx.FindComponentByName<CButton>(transform,"DeletePartButton");
        deletePartButtonText = GameObjectEx.FindComponentByName<Text>(transform,"DeletePartButtonText");
        editSyllableContent = GameObjectEx.FindChildByName(transform,"EditSyllableContent");
        _selectSyllableView = GameObjectEx.FindComponentByName<SelectSyllableView>(transform,"SelectSyllableView");
        partItemContent = GameObjectEx.FindChildByName(transform,"PartItemContent");
        addPartTipView = GameObjectEx.FindComponentByName<AddPartTipView>(transform,"AddPartTipView");
        deletePartTipView = GameObjectEx.FindComponentByName<DeletePartTipView>(transform,"DeletePartTipView");
        editBPMView = GameObjectEx.FindComponentByName<EditBPMView>(transform,"EditBPMView");
        editBPMButton= GameObjectEx.FindComponentByName<CButton>(transform,"EditBPMButton");
        editBPMText = GameObjectEx.FindComponentByName<Text>(editBPMView.transform,"CurBPM");
        backButton.onClick.AddListener(OnBackClick);
        saveButton.onClick.AddListener(OnSaveClick);
        editInfoButton.onClick.AddListener(OnEditInfoClick);
        tryPlayButton.onClick.AddListener(OnTryPlayClick);
        addPartButton.onClick.AddListener(OnAddPartClick);
        deletePartButton.onClick.AddListener(OnDeletePartClick);
        editBPMButton.onClick.AddListener(OnEditBPMBtnClick);
        InitFirstPartItem();
        InitSyllableItems();
        _selectSyllableView.Init();
        addPartTipView.Init();
        deletePartTipView.Init();
        editBPMView.Init(OnBPMValueChange);
        InitBG();
        MessageHelper.AddListener<GoodsData>(MessageName.OnMusicScorePreviewSelect,SetCurPreviewInstrument );
        deletePartButtonText.SetLocalText("删除小节");
    }
    public override void OnShow(params object[] args)
    {
        if (args==null||args.Length==0)
        {
            return;
        }
        MusicScoreInfo info = args[0] as MusicScoreInfo;
        currentMusicScoreInfo = info.Clone();
        SetBPMText();
        if (currentMusicScoreInfo.partList == null)
        {
            currentMusicScoreInfo.AddPart();
        }
        else
        {
            if (currentMusicScoreInfo.partList.Count>1)
            {
                for (int i = 1; i < currentMusicScoreInfo.partList.Count; i++)
                {
                    AddPartItem();
                }
            }
        }
        RefrashPartData();
        GameTimeUtils.Inst.StartCollect(TAG);
        AkSoundManager.Inst.StopBGSound();
    }
    private void InitBG()
    {
        if (_trans_Bg == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(_trans_Bg);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        {
            "music_icon_1", "music_icon_2", "music_icon_3"
        });
        item.gameObject.SetActive(true);
    }
    public void InitSyllableItems()
    {
        for (int j = 0; j < partSyllableCount; j++)
        {
            var obj = Instantiate(editSyllableprefab, editSyllableContent);
            obj.SetActive(true);
            var item = obj.GetComponent<EditSyllableItem>();
            item.Init(OnSetSyllable);
            int id = j;
            item.SetId(id);
            editSyllableItemList.Add(item);
        }
        
    }
    private void InitFirstPartItem()
    {
        AddPartItem().SetSelected(true);
    }
    private MusicScorePartItem AddPartItem()
    {
        var obj = Instantiate(partItemPrefab, partItemContent);
        obj.SetActive(true);
        var item = obj.GetComponent<MusicScorePartItem>();
        item.Init(OnPartDelete,OnPartSelect);
        item.SetInfo(partItems.Count);
        item.SetDelete(isDeletePartSelect);
        partItems.Add(item);
        return item;
    }

    private void OnPartSelect(MusicScorePartItem item)
    {
        for (int i = 0; i < partItems.Count; i++)
        {
            partItems[i].SetSelected(false);
        }
        item.SetSelected(true);
        curPartIndex = item.PartId;
        RefrashPartData();
    }
    private void OnPartDelete(MusicScorePartItem item)
    {
        deletePartTipView.Show(() =>
        {
            //第一段不支持删除
            if (item.PartId == 0)
            {
                return;
            }
            DeletePart(item.PartId);
        });
       
    }

    private void DeletePart(int partId)
    {
        currentMusicScoreInfo.DeletePart(partId);
        var item = partItems[partId];
        partItems.Remove(item);
        Destroy(item.gameObject);
       
        if (partItems.Count>partId)
        {
            for (int i = partId; i < partItems.Count; i++)
            {
                partItems[i].SetInfo(i);
            }
        }
        if (curPartIndex == partId)
        {
            if (partItems.Count <= partId)
            {
                curPartIndex--;
            }
            OnPartSelect(partItems[curPartIndex]);
        }
    }
    public void RefrashPartData()
    {
        if (currentMusicScoreInfo.partList.Count<=curPartIndex)
        {
            currentMusicScoreInfo.AddPart();
        }
        var infoList = currentMusicScoreInfo.partList[curPartIndex].syllableInfosList;
        for (int i = 0; i < infoList.Count; i++)
        {
            editSyllableItemList[i].SetShow(infoList[i]);
        }
    }

    private void SetCurPreviewInstrument(GoodsData data)
    {
        if (string.IsNullOrEmpty(data.Id))
        {
            curPreviewInstrumentData = null;
        }
        else
        {
            curPreviewInstrumentData = data;
        }
     
    }
    public void OnBackClick()
    {
        CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetLocalText("确认保存","保存当前的创作进度吗？", "保存", "不保存");
        commonConfirmPanel.SetIsCloseSelf(false);
        commonConfirmPanel.SetOnClickAction(() =>
        {
            commonConfirmPanel.SetConfirmLoadingVisible(true);
            SaveMusicScoreData((success) =>
            {
                TipPanel.ShowToast(success ? "保存成功:D" : "保存失败");
                if (success)
                {
                    if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
                    {
                        commonConfirmPanel.Close();
                    }
                    GameController.ExitGame(() =>
                    {
                        CloseSelf();
                        MessageHelper.Broadcast(DraftMessage.RefreshDraft);
                    });
                }
            });

        }, () =>
        {
            if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
            {
                commonConfirmPanel.Close();
            }
            GameController.ExitGame(() =>
            {
                CloseSelf();
                MessageHelper.Broadcast(DraftMessage.RefreshDraft);
            });
        });
    }
    private void SaveMusicScoreData(Action<bool> saveCallBack)
    {
        var draftInfo = MusicScoreAssetManager.Inst.GetOrCreateDraftInfo(currentMusicScoreInfo);
        int curEditTime = GameTimeUtils.Inst.RestartCollect(TAG);//重启编辑时长
        LoggerUtils.Log("###原编辑总时长："+draftInfo.editTime + "  当次编辑时长："+ curEditTime);
        draftInfo.editTime += curEditTime;
        draftInfo.EditDraftToServer((info, isSuccess) => {
            saveCallBack?.Invoke(isSuccess);
        });
    }
    public void OnSaveClick()
    {
        saveButton.SetLoadingVisible(true);
        SaveMusicScoreData((success) =>
        {
            TipPanel.ShowToast(success ? "保存成功:D" : "保存失败");
            saveButton.SetLoadingVisible(false);
        });
    }
    public void OnEditInfoClick()
    {
        UIManager.Inst.OpenPanel(PanelId.MusicScoreEditInfoPanel,true,currentMusicScoreInfo);
    }
    public void OnTryPlayClick()
    {
        UIAgentManager.Inst.OpenPanel(PanelId.MusicScorePreviewPanel,currentMusicScoreInfo,curPreviewInstrumentData);
    }
    public void OnSetSyllable(int id,MusicScoreSyllableInfo info)
    {
        _selectSyllableView.Open(id,info,OnSyllableSetSuccess,CanSelectLongSyllable(id),currentMusicScoreInfo.toneType);
    }

    private bool CanSelectLongSyllable(int id)
    {
        //第一位音节不可以选择连音
        int listIndex = editSyllableItemList.FindIndex(x=>x.curId == id);
        if (curPartIndex == 0&&listIndex<=0)
        {
            return false;
        }

        return true;

    }
    public void OnSyllableSetSuccess(int id,MusicScoreSyllableInfo info)
    {
        currentMusicScoreInfo.partList[curPartIndex].syllableInfosList[id] = info;
        SetSingleSyllableItem(id,info);
    }

    private void SetSingleSyllableItem(int id ,MusicScoreSyllableInfo info)
    {
        var item = editSyllableItemList.Find(x => x.curId == id);
        if (item!=null)
        {
            item.SetShow(info);
        }
    }
    public void OnAddPartClick()
    {
        if (CheckCanAddPart())
        {
            currentMusicScoreInfo.AddPart();
            var item = AddPartItem();
            OnPartSelect(item);
        }
        else
        {
            addPartTipView.Show();
        }
    }

    private bool CheckCanAddPart()
    {
        if (currentMusicScoreInfo.partList.Count>0)
        {
            if (currentMusicScoreInfo.partList.Count<4)
            {
                var part = currentMusicScoreInfo.partList[currentMusicScoreInfo.partList.Count - 1];
                for (int i = 0; i < part.syllableInfosList.Count; i++)
                {
                    //判断没有
                    if (part.syllableInfosList[i].syllableType == (int)MusicScoreSyllableType.Syllables
                        &&(part.syllableInfosList[i].syllablesList == null||part.syllableInfosList[i].syllablesList.Count==0))
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                TipPanel.ShowToast("当前最多支持8小节");
                return false;
            }
           
        }
        return false;
        
    }
    public void OnDeletePartClick()
    {
        if (!isDeletePartSelect)
        {
            deletePartButtonText.SetLocalText("取消删除");
            isDeletePartSelect = true;
        }
        else
        {
            deletePartButtonText.SetLocalText("删除小节");
            isDeletePartSelect = false;
        }
        SetPartsDeleteBtnShow(isDeletePartSelect);
    }

    private void OnEditBPMBtnClick()
    {
        editBPMView.Show();
    }
    private void OnBPMValueChange(float value)
    {
        currentMusicScoreInfo.bpm = (int)value;
        SetBPMText();
    }

    private void SetBPMText()
    {
        editBPMText.text = currentMusicScoreInfo.bpm.ToString();
    }
    public void SetPartsDeleteBtnShow(bool isShow)
    {
        if (partItems.Count>1)
        {
            for (int i = 1; i < partItems.Count; i++)
            {
                partItems[i].SetDelete(isShow);
            }
        }
        
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        AkSoundManager.Inst.PlayBGSound();
        MessageHelper.RemoveListener<GoodsData>(MessageName.OnMusicScorePreviewSelect,SetCurPreviewInstrument );
    }
}
