using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Avatar;
using GameData.BaseInfo;
using Pb.Game;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.MusicalInstrument
{
    public class GuestInstrumentPlayPanel : BasePanel<GuestInstrumentPlayPanel>
    {
        public CButton Btn_Exit;
        public Button Btn_StopScore;
        public RemoteImageBehaviour Rm_ScoreCover;
        [Header("高音")]
        public GameObject HighGroup;
        [Header("中音")]
        public GameObject MiddleGroup;
        [Header("低音")]
        public GameObject LowGroup;

        public GameObject Go_ToggleGroup;
        public Toggle Syllable15Toggle;
        public Toggle Syllable22Toggle;
        public CButton Btn_MusicScore;

        private PlayerHoldBehaviour _playerHoldBehaviour;
        private PlayMusicScoreBev _playMusicScoreBev;
        private List<CommonSyllableItem> _syllableItems = new List<CommonSyllableItem>();
        private string _itemPath = "Assets/Loadable/UI/UIPanel/MusicalInstrument/CommonSyllableItem.prefab";

        private string _curInstrumentId;
        private InstrumentInfo _curInstrumentInfo;
        private ToneInfo _curToneInfo;
        
        public override void OnCreate()
        {
            base.OnCreate();
            Btn_Exit.onClick.AddListener(ExitPlayMusicState);
            Message.MessageHelper.AddListener<MusicScoreInfo>(Message.MessageName.OnMusicScorePlay, OnMusicScorePlay);
            _playMusicScoreBev = this.GetComponent<PlayMusicScoreBev>();
            Btn_MusicScore.onClick.AddListener(OnBtnMusicScoreClick);
            Btn_StopScore.onClick.AddListener(OnStopMusicScore);
            
            _playerHoldBehaviour = AvatarController.Inst.SelfController.GetComponentInChildren<PlayerHoldBehaviour>(true);

            if (UIManager.Inst.TryFindPanel(PanelId.UIOperationOnWorldPanel, out UIOperationOnWorldPanel panel))
            {
                panel.gameObject.SetActive(false);
            }
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            _curInstrumentId = (string)args[0];
            _curInstrumentInfo = (InstrumentInfo)args[1];
            _curToneInfo = _curInstrumentInfo.toneInfo;
            
            InitUploadItems();
            InitToggle();
            if (args.Length >= 3 && (bool)args[2] == true)
            {
                SetParkInfo();
            }
        }

        public override void OnHidden()
        {
            base.OnHidden();
            Message.MessageHelper.RemoveListener<MusicScoreInfo>(Message.MessageName.OnMusicScorePlay, OnMusicScorePlay);
            
            if (UIManager.Inst.TryFindPanel(PanelId.UIOperationOnWorldPanel, out UIOperationOnWorldPanel panel))
            {
                panel.gameObject.SetActive(true);
            }
        }

        private void OnMusicScorePlay(MusicScoreInfo info)
        {
            MusicalInstrumentManager.Inst.CheckInstrumentCanPlayMusicScore(_curToneInfo,info, () =>
            {
                TipPanel.ShowToast("这个乐谱是22音，你的乐器是15音，听起来可能会少音哦");
            });
            MusicalInstrumentManager.Inst.CheckInstrumentIsUgcToneAndShowToast(_curToneInfo);
            _playMusicScoreBev.StartPLay(info, OnPlayMusicScoreSyllable);
            Btn_StopScore.gameObject.SetActive(true);
            Rm_ScoreCover.Load(info.cover);
        }

        private void InitToggle()
        {
            Syllable15Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                    SwitchToneType(ToneType.Fifteen);
            });
            Syllable22Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                    SwitchToneType(ToneType.TwentyTwo);
            });
            Syllable15Toggle.isOn = true;
            SwitchToneType(ToneType.Fifteen);

            if (_curInstrumentInfo.toneInfo.toneType == (int)ToneType.Fifteen)
            {
                Go_ToggleGroup.SetActive(false);
            }
        }
        
        public void SwitchToneType(ToneType toneType)
        {
            switch (toneType)
            {
                case ToneType.Fifteen:
                    HighGroup.SetActive(true);
                    MiddleGroup.SetActive(true);
                    LowGroup.SetActive(false);
                    break;

                case ToneType.TwentyTwo:
                    HighGroup.SetActive(true);
                    MiddleGroup.SetActive(true);
                    LowGroup.SetActive(true);
                    break;
            }
        }
        
        private void InitUploadItems()
        {
            _syllableItems.Clear();
            foreach (var syllableType in MusicalInstrumentUtils.High_Config)
            {
                var itemObj = Loader.Load<GameObject>(_itemPath).Instantiate(HighGroup.transform);
                var item = itemObj.GetComponent<CommonSyllableItem>();
                item.InitData(_curToneInfo, (int)syllableType, OnItemSelected);
                _syllableItems.Add(item);
            }
            foreach (var syllableType in MusicalInstrumentUtils.Middle_Config)
            {
                var itemObj = Loader.Load<GameObject>(_itemPath).Instantiate(MiddleGroup.transform);
                var item = itemObj.GetComponent<CommonSyllableItem>();
                item.InitData(_curToneInfo, (int)syllableType, OnItemSelected);
                _syllableItems.Add(item);
            }
            foreach (var syllableType in MusicalInstrumentUtils.Low_Config)
            {
                var itemObj = Loader.Load<GameObject>(_itemPath).Instantiate(LowGroup.transform);
                var item = itemObj.GetComponent<CommonSyllableItem>();
                item.InitData(_curToneInfo, (int)syllableType, OnItemSelected);
                _syllableItems.Add(item);
            }
        }

        private List<SyllablePlayData> _playDatas = new List<SyllablePlayData>();
        private float curWaitTime;
        private float NeedWaitTime = 0.06f;
            
        protected override void Update()
        {
            if (_playDatas.Count>0)
            {
                if (curWaitTime >= NeedWaitTime)
                {
                    curWaitTime = 0;
                    OnPlaySyllable(_playDatas);
                    _playDatas.Clear();
                }
                else
                {
                    curWaitTime += Time.deltaTime;
                } 
            }
            
            
        }

        private void OnItemSelected(ToneInfo toneInfo, int syllableId)
        {
            MusicalInstrumentManager.Inst.CheckInstrumentIsUgcToneAndShowToast(toneInfo);
            
            _syllableItems.ForEach(x => x.SetSelectState(false));
            SyllablePlayData data = new SyllablePlayData();
            data.SyllId = syllableId;
            _playDatas.Add(data);
            // OnPlaySyllable(data);
        }
        public void OnPlayMusicScoreSyllable(List<SyllablePlayData> data)
        {
            // 用最新一拍替换待播列表：高BPM场景下多拍积压后同帧触发会叠音，产生爆音或和弦串音
            _playDatas.Clear();
            _playDatas.AddRange(data);
            SetItemSelect(data);
        }
        public void SetItemSelect(List<SyllablePlayData> dataList)
        {
            if (_syllableItems!=null)
            {
                for (int i = 0; i < dataList.Count; i++)
                {
                    var item = _syllableItems.Find(x => x._syllableId == dataList[i].SyllId);
                    if (item)
                    {
                        item.SetSelect(dataList[i]);
                    }
                }
                
            }
        }
        public void OnPlaySyllable(List<SyllablePlayData> data)
        {
            if (_playerHoldBehaviour != null)
            {
                _playerHoldBehaviour.PlayMusicSyllable(data);
            }
            MusicalInstrumentNetManager.Inst.SendMusicPlay(data);
        }

        private void OnBtnMusicScoreClick()
        {
            UIManager.Inst.OpenPanel(PanelId.MusicScoreBagPanel);
        }
        
        private void OnStopMusicScore()
        {
            Btn_StopScore.gameObject.SetActive(false);
            _playMusicScoreBev.StopPLay();
        }
        
        private void ExitPlayMusicState()
        {
            GuestInstrumentOPManager.Inst.ExitPlayMusicInstrumentState();
        }

        public void SetParkInfo() 
        {
            Btn_MusicScore.gameObject.SetActive(false);
            Btn_Exit.gameObject.SetActive(false);
        }
    }
}
