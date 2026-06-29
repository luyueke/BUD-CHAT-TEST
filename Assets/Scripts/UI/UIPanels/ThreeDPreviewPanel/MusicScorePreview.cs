using System;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.MusicalInstrument;
using GameData.BaseInfo;
using GameData.PgcData;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pb.Game;
using UnityEngine;

public class MusicScorePreview : MonoBehaviour
{
    public Transform characterRoot;
    public CharacterWrap characterWrap;
    public UIDragUtil dragUtil;
    private PlayerHoldBehaviour playerHoldBehaviour;
    private PlayMusicScoreBev _playMusicScoreBev;
    public void StartPreview(MusicScoreInfo ugcInfo)
    {
        this.gameObject.SetActive(true);
        var avatarJson = AccountDataManager.Inst.UserInfo.avatarJson;
        CharacterData tempAvatarInfo = CharacterData.DeserializeObject(avatarJson).Clone();
        if (characterWrap == null)
        {
            characterWrap = AvatarController.Inst.CreateUIAvatar(tempAvatarInfo);
            characterWrap.SetParent(characterRoot, true);
        }
        else
        {
            characterWrap.RefreshAvatar(tempAvatarInfo);
        }
        dragUtil.RotateTarget = characterWrap.Avatar.transform;
        _playMusicScoreBev = transform.GetComponent<PlayMusicScoreBev>();
        playerHoldBehaviour = characterWrap.Avatar.GetComponentInChildren<PlayerHoldBehaviour>();
        PreviewMusicScore(ugcInfo);
    }
    private void PreviewMusicScore(MusicScoreInfo ugcInfo)
    {
        var isPgc = AccountDataManager.Inst.TryListenMIData.isPgc;
        var id = AccountDataManager.Inst.TryListenMIData.id;
        if (isPgc)
        {
            characterWrap.ChangePart(UniqueType.GetAvatar(AvatarSubType.MusicalInstrument), id);
            playerHoldBehaviour.PreviewPGCInstrument(id);
            MusicalInstrumentManager.Inst.CheckInstrumentCanPlayMusicScore(playerHoldBehaviour.curToneInfo,ugcInfo, () =>
            {
                TipPanel.ShowToast("这个乐谱是22音，你的乐器是15音，听起来可能会少音哦");
            });
            _playMusicScoreBev.StartPLay(ugcInfo,OnPlaySingleSyllable);
            return;
        }
        GetMIDetailInfo(id, (rspData) =>
        {
            characterWrap.ChangeUGCPart(rspData.skinInfo);
            playerHoldBehaviour.PreviewUGCInstrument(rspData.skinActionInfo.instrumentInfo);
            MusicalInstrumentManager.Inst.CheckInstrumentCanPlayMusicScore(playerHoldBehaviour.curToneInfo,ugcInfo, () =>
            {
                TipPanel.ShowToast("这个乐谱是22音，你的乐器是15音，听起来可能会少音哦");
            });
            MusicalInstrumentManager.Inst.CheckInstrumentIsUgcToneAndShowToast(playerHoldBehaviour.curToneInfo);
            _playMusicScoreBev.StartPLay(ugcInfo,OnPlaySingleSyllable);
        });
    }
    private void GetMIDetailInfo(string id, Action<DetailRsp> suc)
    {
        JObject req = new JObject()
        {
            ["idList"] = id,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GetClothesBatchInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
        {
            if (this == null) return;
            BatchDetailRsp rspData = JsonConvert.DeserializeObject<BatchDetailRsp>(content);
            if (rspData.skinList == null || rspData.skinList.Count == 0) return;
            suc?.Invoke(rspData.skinList[0]);
        },
        (msg) =>
        {

        });
    }
    public void OnPlaySingleSyllable(List<SyllablePlayData> playData)
    {
        playerHoldBehaviour.PlayMusicSyllable(playData);
    }
}
