using System.Collections.Generic;
using Game.MusicalInstrument;
using UGCAsset;
using UI;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;


public class PublishTonePanel : BasePanel<PublishTonePanel>
{
    public CButton closeBtn;
    public CButton publishBtn;
    public UgcToneList ugcToneList;

    private ToneStudioPanelData _curPanelData = new ToneStudioPanelData();

    public override void OnCreate()
    {
        base.OnCreate();
        closeBtn.onClick.AddListener(() =>
        {
            if (ugcToneList.Adapter != null)
            {
                MusicalInstrumentManager.Inst.StopPreviewUgcTone(ugcToneList.Adapter.gameObject);
            }
            CloseSelf();
        });
        publishBtn.onClick.AddListener(OnPublishBtnClicked);
        ugcToneList.GetPublishedData();
        ugcToneList.SetOnToneItemSelectAct(toneInfo =>
        {
            _curPanelData.CurToneInfo = toneInfo;
            _curPanelData.OnSelectToneItem?.Invoke(toneInfo);
            var spriteatlasPath = "Assets/Loadable/UI/UIPanel/CommonSprite/CommonSprite.spriteatlas";
            GameObjectEx.FindChildByName(transform, "publishBtn").GetComponent<Image>().sprite =
                XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "Yellow_Btn_3", gameObject);
        });
    }
    
    private void OnPublishBtnClicked()
    {
        if (_curPanelData.CurToneInfo == null)
        {
            return;
        }
        if (ugcToneList.Adapter != null)
        {
            MusicalInstrumentManager.Inst.StopPreviewUgcTone(ugcToneList.Adapter.gameObject);
        }
        var publishMachine = new UGCPublishStateMachine();
        var stateList = new List<UGCPublishStateBase>() {
        };
        stateList.Add(new UGCToneDetailState());
        publishMachine.SetStates(stateList);

        publishMachine.SetEditData(new ToneEditData() {
            draftInfo = _curPanelData.CurToneInfo,
            currencyType = CurrencyType.PinkCoin
        });
        publishMachine.SetFinishCallBack(() => {
            CloseSelf();
        });
        publishMachine.Start();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
    }

    public string GetCurToneId()
    {
        return _curPanelData?.CurToneInfo?.id;
    }
}