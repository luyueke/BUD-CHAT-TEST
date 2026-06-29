using System.Collections.Generic;
using Game.MusicalInstrument;
using GameData.Base;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class PreviewButton : CommonUIWidget
{
    public CButton Btn_Preview;
    public Image Img_Icon;
    public CText Txt_Title;
    public Sprite Icon_3D;
    public Sprite Icon_Skin;
    public Sprite Icon_Music;
    public Sprite Icon_Tone;
    public Sprite Icon_Tone_Pause;
    public Sprite Icon_Play;
    public Sprite Icon_NpcInfo;
    public Sprite Icon_ActorInfo;
    private AssetDetailType _curDetailType = AssetDetailType.Null;
    private UgcBaseInfo _curUgcInfo;
    private CharacterBoxInfo _curCharacterBoxInfo;

    private bool isTonePlaying = false;
    
    private void Awake()
    {
        Btn_Preview.onClick.AddListener(OnPreviewBtnClick);
    }

    /// <summary>
    /// 所需要的参数-args[0]AssetDetailType args[1]
    /// </summary>
    public override void SetData(params object[] args)
    {
        base.SetData(args);
        _curDetailType = (AssetDetailType)args[0];
        _curUgcInfo = (UgcBaseInfo)args[1];
        _curCharacterBoxInfo = args.Length > 2 ? args[2] as CharacterBoxInfo : null;
        InitTitle();
        
        this.gameObject.SetActive(true);
    }
    
    public void PauseTonePlay()
    {
        Img_Icon.sprite = Icon_Tone;
        switch (_curDetailType)
        {
            case AssetDetailType.MusicTone:
                MusicalInstrumentManager.Inst.StopPreviewUgcTone(this.gameObject);
                break;
            
            case AssetDetailType.UgcAnimMusic:
                UgcAnimToneManager.Inst.StopPreviewTone();
                break;
        }
    }

    private void InitTitle()
    {
        switch (_curDetailType)
        {
            case AssetDetailType.Prop:
                Img_Icon.sprite = Icon_3D;
                Txt_Title.SetLocalText("3D预览");
                break;
            case AssetDetailType.Mat:
                Img_Icon.sprite = Icon_3D;
                Txt_Title.SetLocalText("3D预览");
                break;
            
            case AssetDetailType.Skin:
                Img_Icon.sprite = Icon_Skin;
                Txt_Title.SetLocalText("试穿一下");
                break;
            case AssetDetailType.Instrument:
                Img_Icon.sprite = Icon_Music;
                Txt_Title.SetLocalText("试演奏");
                break;
            case AssetDetailType.MusicScore:
                Img_Icon.sprite = Icon_Music;
                Txt_Title.SetLocalText("试听");
                break;
            case AssetDetailType.UgcBundle:
                Img_Icon.sprite = Icon_Skin;
                Txt_Title.SetLocalText("试穿一下");
                break;
            case AssetDetailType.MusicTone:
            case AssetDetailType.UgcAnimMusic:
                Img_Icon.sprite = Icon_Tone;
                Txt_Title.SetLocalText("试听");
                break;
            
            case AssetDetailType.UgcPose:
                Img_Icon.sprite = Icon_Play;
                Txt_Title.SetLocalText("预览");
                break;
                
            case AssetDetailType.UgcAnim:
                Img_Icon.sprite = Icon_Play;
                Txt_Title.SetLocalText("预览");
                break;
            
            case AssetDetailType.AINpc:
                Img_Icon.sprite = Icon_NpcInfo;
                Txt_Title.SetLocalText("资料卡");
                break;
            case AssetDetailType.Actor:
                Img_Icon.sprite = Icon_ActorInfo;
                Txt_Title.SetLocalText("资料卡");
                break;
            case AssetDetailType.Vehicle:
                Img_Icon.sprite = Icon_3D;
                Txt_Title.SetLocalText("3D预览");
                break;
            case AssetDetailType.Theatre:
                Img_Icon.sprite = Icon_Play;
                Txt_Title.SetLocalText("进入剧场");
                break;
            case AssetDetailType.CharacterBox:
                Img_Icon.sprite = Icon_3D;
                Txt_Title.SetLocalText("3D预览");
                break;
        }
    }


    private void OnPreviewBtnClick()
    {
        var updateState = (ForceUpdate)_curUgcInfo.forceUpdate;
        if (updateState != ForceUpdate.Default)
        {
            UIManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, updateState);
            return;
        }

        ThreeDPreviewPanel panel = null; 
        switch (_curDetailType)
        {
            case AssetDetailType.Prop:
                panel = UIManager.Inst.OpenPanel<ThreeDPreviewPanel>(PanelId.ThreeDPreviewPanel, _curDetailType);
                panel.PreviewModel(_curUgcInfo);
                break;
            case AssetDetailType.Mat:
                panel = UIManager.Inst.OpenPanel<ThreeDPreviewPanel>(PanelId.ThreeDPreviewPanel, _curDetailType);
                panel.PreviewMat((MaterialInfo)_curUgcInfo);
                break;
            case AssetDetailType.Skin:
                panel = UIManager.Inst.OpenPanel<ThreeDPreviewPanel>(PanelId.ThreeDPreviewPanel, _curDetailType);
                var skinInfo = (SkinInfo)_curUgcInfo;
                switch ((SkinType)skinInfo.skinType)
                {
                    case SkinType.Pet:
                        panel.PreviewPet((SkinInfo)_curUgcInfo);
                        break;
                    
                    default:
                    case SkinType.Avatar:
                        panel.PreviewAvatar((SkinInfo)_curUgcInfo);
                        break;
                }
                break;
           case AssetDetailType.MusicScore:
                panel = UIManager.Inst.OpenPanel<ThreeDPreviewPanel>(PanelId.ThreeDPreviewPanel, _curDetailType);
                panel.PreviewMusicScore((MusicScoreInfo)_curUgcInfo);
                break;
            case AssetDetailType.UgcBundle:
                panel = UIManager.Inst.OpenPanel<ThreeDPreviewPanel>(PanelId.ThreeDPreviewPanel, _curDetailType);
                panel.PreviewBundle((SkinInfo)_curUgcInfo);
                break;
            
            case AssetDetailType.Instrument:
                var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();
                saveCharacterData.ChangeSkinData((SkinInfo)_curUgcInfo);
                UIManager.Inst.SwapPanel(PanelId.TryMusicalInstrumentPanel, saveCharacterData);
                break;
            
            case AssetDetailType.MusicTone:
                if (isTonePlaying)
                {
                    isTonePlaying = false;
                    Img_Icon.sprite = Icon_Tone;
                    MusicalInstrumentManager.Inst.StopPreviewUgcTone(this.gameObject);
                }
                else
                {
                    isTonePlaying = true;
                    Img_Icon.sprite = Icon_Tone_Pause;
                    var toneInfo = (ToneInfo)_curUgcInfo;
                    var previewList = new List<SyllableType>();
                    previewList.AddRange(MusicalInstrumentUtils.Low_Config);
                    previewList.AddRange(MusicalInstrumentUtils.Middle_Config);
                    previewList.AddRange(MusicalInstrumentUtils.High_Config);
                    MusicalInstrumentManager.Inst.PreviewUgcTone(toneInfo, this.gameObject, previewList);
                }
                break;
            
            case AssetDetailType.UgcAnim:
                panel = UIManager.Inst.OpenPanel<ThreeDPreviewPanel>(PanelId.ThreeDPreviewPanel, _curDetailType);
                panel.PreviewAnim((AnimInfo)_curUgcInfo);
                break;
            
            case AssetDetailType.UgcPose:
                panel = UIManager.Inst.OpenPanel<ThreeDPreviewPanel>(PanelId.ThreeDPreviewPanel, _curDetailType);
                panel.PreviewPose((PoseInfo)_curUgcInfo);
                break;
            
            case AssetDetailType.UgcAnimMusic:
                if (isTonePlaying)
                {
                    isTonePlaying = false;
                    Img_Icon.sprite = Icon_Tone;
                    UgcAnimToneManager.Inst.StopPreviewTone();
                }
                else
                {
                    isTonePlaying = true;
                    Img_Icon.sprite = Icon_Tone_Pause;
                    var animMusicInfo = (AnimMusicInfo)_curUgcInfo;
                    UgcAnimToneManager.Inst.PreviewTone(animMusicInfo);
                }
                break;
            
            case AssetDetailType.AINpc:
                UIManager.Inst.OpenPanel<AINpcInfoCardPopupPanel>(PanelId.AINpcInfoCardPopupPanel, _curUgcInfo);
                break;
            case AssetDetailType.Actor:
                UIManager.Inst.OpenPanel(PanelId.ActorCardInfoPanel, _curUgcInfo);
                break;
            case AssetDetailType.Vehicle:
                panel = UIManager.Inst.OpenPanel<ThreeDPreviewPanel>(PanelId.ThreeDPreviewPanel, _curDetailType);
                panel.PreviewVehicle((VehicleInfo)_curUgcInfo);
                break;
            case AssetDetailType.Theatre:
                UIManager.Inst.OpenPanel(PanelId.TheatreInfoPanel, _curUgcInfo, (int)GameData.TheatreEnterType.Store);
                break;
            case AssetDetailType.CharacterBox:
                panel = UIManager.Inst.OpenPanel<ThreeDPreviewPanel>(PanelId.ThreeDPreviewPanel, _curDetailType);
                panel.PreviewCharacterBox(_curCharacterBoxInfo);
                break;
        }
    }
}
