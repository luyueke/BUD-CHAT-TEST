using System.Collections.Generic;
using GameData.BaseInfo;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.MusicalInstrument
{
    public class CreateUgcTonePanel : BasePanel<CreateUgcTonePanel>
    {
        public Transform _trans_Bg;
        public CreateAndPublishEditBox EditNameBox;
        public LoadingButton Btn_Confirm;
        public CButton Btn_Return;
        public Image Img_Confirm;

        public Toggle Syllable15Toggle;
        public Toggle Syllable22Toggle;
        public SyllableUploadPanel UploadPanel;
        public ToneInfoEditUtill _toneInfoEditUtill;

        private string _curToneName;
        private string unEnableColor = "#D9D9D9";
        private string enableColor = "#FFD400";

        public override void OnCreate()
        {
            base.OnCreate();
            InitBG();
            _toneInfoEditUtill.CreateNewInfo();
            Btn_Confirm.onClick.AddListener(OnBtnConfirmClick);
            Btn_Return.onClick.AddListener(CloseSelf);
            EditNameBox.SetAfterTextChangeAction(AfterTextChangeAction);
#if PACKAGE_TYPE_US
            EditNameBox.maxLength = 12;
            EditNameBox.InitUI("请给你的音色命名");
            EditNameBox.RefreshLimitText();
#else
            EditNameBox.InitUI("请给你的音色命名");
#endif
            Img_Confirm.color = DataUtil.DeSerializeColorCheckHash(unEnableColor);
            Btn_Confirm.SetClickAble(false);
            UploadPanel.Init();
            Syllable15Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                    OnSelectSyllableType(ToneType.Fifteen);
            });
            Syllable22Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                    OnSelectSyllableType(ToneType.TwentyTwo);
            });
            Syllable15Toggle.isOn = true;
            OnSelectSyllableType(ToneType.Fifteen);
            
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

        public void AfterTextChangeAction(string toneName)
        {
            _curToneName = toneName;
            _toneInfoEditUtill.SetToneName(toneName);
            SetCreateButtonEnable();
        }

        private void OnSelectSyllableType(ToneType toneType)
        {
            _toneInfoEditUtill.SetToneType(toneType);
            UploadPanel.SwitchToneType(toneType, OnUploadSyllableAct);
            UploadPanel.RestoreFromCache(_toneInfoEditUtill.Temp_Syllable_Dict);
        }

        private void OnUploadSyllableAct(SyllableType syllableType, string upLoadUrl)
        {
            _toneInfoEditUtill.SetUgcSyllable((int)syllableType, upLoadUrl);
            SetCreateButtonEnable();
        }

        private void SetCreateButtonEnable()
        {
            bool enable = _toneInfoEditUtill.CheckToneInfoIsLegal();
            Btn_Confirm.SetClickAble(enable);
            var btnColor = !enable ? unEnableColor : enableColor;
            Img_Confirm.color = DataUtil.DeSerializeColorCheckHash(btnColor);
        }

        private void OnBtnConfirmClick()
        {
            Btn_Confirm.ShowLoading();
            Btn_Confirm.SetClickAble(false);

            var curToneInfo = _toneInfoEditUtill.GetCurToneInfo();
            EditToneInfoReq req = new EditToneInfoReq()
            {
                musicToneInfo = curToneInfo,
                setType = (int)SetType.Create
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SetUGCTone, HttpMethod.POST, JsonConvert.SerializeObject(req), OnCreateToneSuccess, OnCreateToneError);
        }

        private void OnCreateToneSuccess(string content)
        {
            Btn_Confirm.HideLoading();
            Btn_Confirm.SetClickAble(true);
            MessageHelper.Broadcast(MessageName.OnUgcTonePublishedListChange);
            CloseSelf();
        }

        private void OnCreateToneError(string error)
        {
            Btn_Confirm.HideLoading();
            Btn_Confirm.SetClickAble(true);
        }
    }
}