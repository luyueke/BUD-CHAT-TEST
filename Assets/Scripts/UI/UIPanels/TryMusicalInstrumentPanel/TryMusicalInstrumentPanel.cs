using Game.Avatar;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using GameData.PgcData;
using Network;
using Newtonsoft.Json.Linq;
using Network.Http;
using Newtonsoft.Json;
using GameData.UGCData;
using Pb.Game;
using GameData.BaseInfo;
using Com.TheFallenGames.OSA.Util.IO;
using Game.MusicalInstrument;

public class TryMusicalInstrumentPanel : BasePanel<TryMusicalInstrumentPanel>
{
    [SerializeField] Button closeButton;
    [SerializeField] Transform highRoot;
    [SerializeField] Transform midRoot;
    [SerializeField] Transform lowRoot;
    [SerializeField] PlayMusicalNote playMusicalNote;
    [SerializeField] GameObject switchToggleRoot;
    [SerializeField] Toggle count15Toggle;
    [SerializeField] Toggle count22Toggle;
    [SerializeField] Button musicScoreButton;
    [SerializeField] Button musicScoreStopButton;
    [SerializeField] RemoteImageBehaviour musicScoreImage;
    [SerializeField] UIDragUtil dragUtil;
    
    [SerializeField] internal Transform characterRoot;

    internal CharacterWrap characterWrap;
    internal PlayerAnimationCtrl animationCtrl;
    internal PlayerHoldBehaviour playerHold;
    internal PlayMusicScoreBev playMusicScoreBev;

    private InstrumentInfo _inputInstrumentInfo;
    private List<PlayMusicalNote> items;
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        Message.MessageHelper.AddListener<MusicScoreInfo>(Message.MessageName.OnMusicScorePlay, OnMusicScorePlay);

        closeButton.onClick.AddListener(CloseSelf);
        musicScoreStopButton.onClick.AddListener(OnStopMusicScore);
        var characterData = args[0] as CharacterData;

        characterWrap = AvatarController.Inst.CreateUIAvatar(characterData);
        characterWrap.SetParent(characterRoot, true);
        dragUtil.RotateTarget = characterWrap.Avatar.transform;
        animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        playerHold = characterWrap.Avatar.GetComponentInChildren<PlayerHoldBehaviour>();
        playMusicScoreBev = GetComponent<PlayMusicScoreBev>();

        InitMusicalNote();

        count15Toggle.onValueChanged.AddListener((isOn) => { if (isOn) SwitchCount(0); });
        count22Toggle.onValueChanged.AddListener((isOn) => { if (isOn) SwitchCount(1); });

        musicScoreButton.onClick.AddListener(OnMusicScoreOpen);
        if (args.Length == 2)
        {
            _inputInstrumentInfo = args[1] as InstrumentInfo;

            StartPlayInEditInstrument(_inputInstrumentInfo);
        }
        else 
        {
            StartPlay();
        }
    }

    public override void OnHidden()
    {
        base.OnHidden();
        Message.MessageHelper.RemoveListener<MusicScoreInfo>(Message.MessageName.OnMusicScorePlay, OnMusicScorePlay);
        playerHold.StopPreviewInstrument();
        playMusicScoreBev.StopPLay();
    }

    private void InitMusicalNote()
    {
        items = new List<PlayMusicalNote>();
        int index = 1;
        for (int i = 0; i < 8; i++)
        {
            var item = Instantiate(playMusicalNote.gameObject, highRoot);
            var note = item.GetComponent<PlayMusicalNote>();
            note.SetIndex(index, OnMusicalNotePlay);
            items.Add(note);
            index++;
        }

        for (int i = 0; i < 7; i++)
        {
            var item = Instantiate(playMusicalNote.gameObject, midRoot);
            var note = item.GetComponent<PlayMusicalNote>();
            note.SetIndex(index, OnMusicalNotePlay);
            items.Add(note);
            index++;
        }

        for (int i = 0; i < 7; i++)
        {
            var item = Instantiate(playMusicalNote.gameObject, lowRoot);
            var note = item.GetComponent<PlayMusicalNote>();
            note.SetIndex(index, OnMusicalNotePlay);
            items.Add(note);
            index++;
        }
    }

    private void SwitchCount(int type)
    {
        switch (type)
        {
            case 0:
                highRoot.gameObject.SetActive(true);
                midRoot.gameObject.SetActive(true);
                lowRoot.gameObject.SetActive(false);
                break;
            case 1:
                highRoot.gameObject.SetActive(true);
                midRoot.gameObject.SetActive(true);
                lowRoot.gameObject.SetActive(true);
                break;
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
    public void OnPlaySyllable(List<SyllablePlayData> data)
    {
        if (playerHold != null)
        {
            playerHold.PlayMusicSyllable(data);
        }
    }

    private void OnMusicalNotePlay(int index)
    {
        _playDatas.Add(new SyllablePlayData()
        {
            SyllId = index
        });
       
        MusicalInstrumentManager.Inst.CheckInstrumentIsUgcToneAndShowToast(_inputInstrumentInfo?.toneInfo);
    }

    private void OnMusicScoreOpen()
    {
        UIManager.Inst.SwapPanel(PanelId.MusicScoreBagPanel);
    }

    private void StartPlay()
    {
        var part = characterWrap.ChaData.GetPartData(UniqueType.GetAvatar(AvatarSubType.MusicalInstrument));
        var ugcPart = characterWrap.ChaData.GetPartData(UniqueType.GetUgcAvatar(AvatarSubType.MusicalInstrument));

        if (part != null && !part.IsNull())
        {
            playerHold.PreviewPGCInstrument(part.Id);
            
            var instrumentConfig = Es.DataTables.GetInstrumentConfig(part.Id);
            InitPgcButton(instrumentConfig.toneId);
            return;
        }

        if (ugcPart != null && !ugcPart.IsNull())
        {
            JObject req = new JObject()
            {
                ["idList"] = ugcPart.UId,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GetClothesBatchInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
            {
                if (this == null || playerHold == null) return;
                BatchDetailRsp rspData = JsonConvert.DeserializeObject<BatchDetailRsp>(content);
                if (rspData.skinList == null || rspData.skinList.Count == 0 || rspData.skinList[0].skinActionInfo == null)
                {
                    TipPanel.ShowToast("乐器详情请求失败~");
                    return;
                }
                var instrumentInfo = rspData.skinList[0].skinActionInfo.instrumentInfo;
                _inputInstrumentInfo = instrumentInfo;
                playerHold.PreviewUGCInstrument(instrumentInfo);
                var toneInfo = instrumentInfo.toneInfo;
                if (toneInfo.IsPgc())
                {
                    InitPgcButton(toneInfo.id);
                }
                else
                {
                    InitUgcButton(instrumentInfo);
                }
            },
            (msg) =>
            {

            });

            return;
        }
    }
    
    private void StartPlayInEditInstrument(InstrumentInfo info)
    {
        playerHold.PreviewUGCInstrument(info);
        var toneInfo = info.toneInfo;
        if (toneInfo.IsPgc())
        {
            InitPgcButton(toneInfo.id);
        }
        else
        {
            InitUgcButton(info);
        }
    }

    private void InitPgcButton(string pgcToneId)
    {
        var pgcToneConfig = Es.DataTables.GetInstrumentToneConfig(pgcToneId);
        ToneType toneType = (ToneType)pgcToneConfig.toneType;
        switch (toneType)
        {
            case ToneType.Both:
                switchToggleRoot.gameObject.SetActive(true);
                count22Toggle.SetIsOnWithoutNotify(true);
                SwitchCount(1);
                break;
            case ToneType.Fifteen:
                switchToggleRoot.gameObject.SetActive(false);
                SwitchCount(0);
                break;
            case ToneType.TwentyTwo:
                switchToggleRoot.gameObject.SetActive(false);
                SwitchCount(1);
                break;
        }
    }

    private void InitUgcButton(InstrumentInfo instrumentInfo)
    {
        ToneType toneType = (ToneType)instrumentInfo.toneInfo.toneType;
        switch (toneType)
        {
            case ToneType.Both:
                switchToggleRoot.gameObject.SetActive(true);
                count22Toggle.SetIsOnWithoutNotify(true);
                SwitchCount(1);
                break;
            case ToneType.Fifteen:
                switchToggleRoot.gameObject.SetActive(false);
                SwitchCount(0);
                break;
            case ToneType.TwentyTwo:
                switchToggleRoot.gameObject.SetActive(false);
                SwitchCount(1);
                break;
        }
    }

    private void OnMusicScorePlay(MusicScoreInfo musicScoreInfo)
    {
      
        MusicalInstrumentManager.Inst.CheckInstrumentCanPlayMusicScore(playerHold.curToneInfo,musicScoreInfo, () =>
        {
            TipPanel.ShowToast("这个乐谱是22音，你的乐器是15音，听起来可能会少音哦");
        });
        MusicalInstrumentManager.Inst.CheckInstrumentIsUgcToneAndShowToast(playerHold.curToneInfo);
        playMusicScoreBev.StartPLay(musicScoreInfo, OnMusicScorePlaying);
        musicScoreStopButton.gameObject.SetActive(true);
        musicScoreImage.Load(musicScoreInfo.cover);
    }

    private void OnMusicScorePlaying(List<SyllablePlayData> data)
    {
        _playDatas.AddRange(data);
        SetItemSelect(data);
    }
    public void SetItemSelect(List<SyllablePlayData> dataList)
    {
        if (items!=null)
        {
            for (int i = 0; i < dataList.Count; i++)
            {
                var item = items.Find(x => x.mIndex == dataList[i].SyllId);
                if (item)
                {
                    item.SetColor(dataList[i]);
                }
            }
            
        }
    }
    private void OnStopMusicScore()
    {
        musicScoreStopButton.gameObject.SetActive(false);
        playMusicScoreBev.StopPLay();
    }
}
