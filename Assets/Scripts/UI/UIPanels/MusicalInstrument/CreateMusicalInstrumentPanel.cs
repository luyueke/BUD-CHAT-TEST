using System;
using System.Collections.Generic;
using Es;
using Game.Base;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using GameData.UGCData;
using Newtonsoft.Json;
using UGCAsset;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.MusicalInstrument
{
    public class CreateMusicalInstrumentPanel : BasePanel<CreateMusicalInstrumentPanel>
    {
        public Transform BG;
        public CreateAndPublishEditBox EditNameBox;
        public BUD_Text Txt_Anim;
        public BUD_Text Txt_ToneName;
        public LoadingButton Btn_Confirm;
        public CButton Btn_Return;
        public CButton Btn_ChangeAnim;
        public CButton Btn_ChangeTone;
        public Image Img_Confirm;

        private EnterGameModel _enterGameModel;
        private DraftListItem _curDraftData;
        private string _curName;
        private string unEnableColor = "#D9D9D9";
        private string enableColor = "#FFD400";
        private SkinInfo _EmptySkinInfo;
        private SkinActionInfo _EmptySkinActionInfo;
        private const string tempSpriteatlasPath = "Assets/Loadable/UI/SpriteAltas/UGCAvatarIcon.spriteatlas";
        public override void OnCreate()
        {
            base.OnCreate();
            InitDefaultData();
            InitBG();
            Btn_Return.onClick.AddListener(CloseSelf);
            Btn_ChangeAnim.onClick.AddListener(OnBtnChangeAnimClick);
            Btn_ChangeTone.onClick.AddListener(OnBtnChangeToneClick);
            EditNameBox.SetAfterTextChangeAction(AfterTextChangeAction);
            EditNameBox.InitUI("给你的创作起个名字");
            Img_Confirm.color = DataUtil.DeSerializeColorCheckHash(unEnableColor);
            Btn_Confirm.SetClickAble(false);
            
            Btn_Confirm.onClick.RemoveAllListeners();
            Btn_Confirm.onClick.AddListener(CreateEmptyInstrument);
            
            Txt_Anim.SetLocalText("电子琴");
            Txt_ToneName.SetLocalText("电子琴");
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            if (!GameController.IsInHallScene())
            {
                TipPanel.ShowToast("游玩过程中无法进行创作哦");
                UIManager.Inst.ClosePanel(PanelId.CreateMusicalInstrumentPanel);
                return;
            }
        }

        private void InitBG()
        {
            if (BG == null)
            {
                return;
            }

            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(BG);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
            {
                "music_icon_1", "music_icon_2", "music_icon_3"
            });
            item.gameObject.SetActive(true);
        }


        //创建初始Data
        private void InitDefaultData()
        {
            _EmptySkinInfo = MusicalInstrumentUtils.GetDefaultMusicalInstrumentSkinInfo();
            _EmptySkinActionInfo = MusicalInstrumentUtils.GetDefaultSkinActionInfo();
        }

        private void AfterTextChangeAction(string name)
        {
            _curName = name;
            Btn_Confirm.SetClickAble(!string.IsNullOrEmpty(_curName));

            var btnColor = string.IsNullOrEmpty(_curName) ? unEnableColor : enableColor;
            Img_Confirm.color = DataUtil.DeSerializeColorCheckHash(btnColor);
        }

        private void CreateEmptyInstrument()
        {
            Btn_Confirm.ShowLoading();
            _EmptySkinInfo.name = _curName;
            
            var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(tempSpriteatlasPath, "UGCMusicInstrument_1", gameObject);
            var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
            p.Init(new SkinInfo()
            {
                name = _curName
            },null ,LoadingType.MusicalInstrument,s:sprite);
            
            GameController.StartSkinActionGame(EnterGameModel.UgcMusicalInstrumentEmpty, _EmptySkinInfo, _EmptySkinActionInfo);
        }

        private void OnBtnChangeAnimClick()
        {
            InstrumentChooseAnimPanelData panelData = new InstrumentChooseAnimPanelData();
            panelData.CurInstrumentInfo = _EmptySkinActionInfo.instrumentInfo;
            panelData.CurSkinInfo = _EmptySkinInfo.Clone();
            panelData.OnSelectUgcAnim = OnSelectedUgcAnimInfo;
            UIManager.Inst.OpenPanel(PanelId.MusicalInstrumentChooseAnimPanel, panelData);
        }

        private void OnBtnChangeToneClick()
        {
            ToneStudioPanelData panelData = new ToneStudioPanelData();
            panelData.CurToneInfo = _EmptySkinActionInfo.instrumentInfo.toneInfo;
            panelData.OnSelectToneItem = OnSelectedToneInfo;
            UIManager.Inst.OpenPanel(PanelId.ToneStudioPanel, panelData);
        }

        private void OnSelectedToneInfo(ToneInfo toneInfo)
        {
            _EmptySkinActionInfo.instrumentInfo.toneInfo = toneInfo;
            Txt_ToneName.SetLocalText(toneInfo?.name);
        }
        
        private void OnSelectedUgcAnimInfo(string id)
        {
            var instrumentAniList = DataTables.GetInstrumentAniConfigList().FindAll((emoAniData) => emoAniData.emoId == id);
            var config = instrumentAniList[0];
            string animName = config.name;
            _EmptySkinActionInfo.instrumentInfo.moveId = id;
            string keyName = animName.Replace("DIY", "").Replace("BUD", "");
            Txt_Anim.SetLocalText(keyName);
        }
    }
}
