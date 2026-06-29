using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
using Pb.Base;
using Pb.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MusicalHoldState
{
    Back = 0,//在背上
    Play = 1,//在手上
}

public class PlayerHoldBehaviour : MonoBehaviour
{
    [HideInInspector] public string MusicEmoteId;

    [HideInInspector] public MusicalHoldState curInstrumentState = MusicalHoldState.Back;
    [HideInInspector] public bool isPreview;
    private List<InstrumentAniConfig> instrumentAniList;
    private List<InstrumentAniConfig> instrumentSingleAniList;
    private PlayerAnimationCtrl _animationCtrl;
    private List<GameObject> expressionGameObjectList;

    //节点替换相关
    private GameObject instrumentObj;
    private Vector3 instrumentLocalPos;
    private Vector3 instrumentLocalEul;
    private Vector3 instrumentLocalSca;
    private InstrumentDetailInfo _detailInfo;


    //音色相关
    public ToneInfo curToneInfo;

    private bool canPlaySingleMusic = true;
    private bool isUseCrossFade = true;
    private const float cooldownTime = 0.2f;
    private const float musicCrossFadeTime = 0.1f;
    private bool isInstrumentInited = false;
    private bool _isSelfPlayer;

    private PlayerAnimationCtrl animationCtrl
    {
        get
        {
            if (_animationCtrl == null)
            {
                _animationCtrl = gameObject.GetComponent<PlayerAnimationCtrl>();
            }
            return _animationCtrl;
        }
    }
    public void Init(CharacterWrap wrap)
    {
        _isSelfPlayer = this.GetComponent<SelfStateController>() != null;
    }
    //获取乐器播放挂点
    public Transform GetMusicInstrumentRoot()
    {
        return GameObjectEx.FindChildByName(transform, "Instrument_bone03");
    }
    //获取乐器穿戴部位挂点
    public Transform GetInstrumentRoot()
    {
        return GameObjectEx.FindChildByName(transform, "instrument_back");
    }
    //当联机需要先进入播放状态，再有换装数据时会强制将节点放入播放节点
    public void FouceSetToHand()
    {
        if (_detailInfo!=null&&curInstrumentState == MusicalHoldState.Play)
        {
            MoveInstrumentToHand(_detailInfo);
        }
    }
    //乐器开始时要放到乐器播放挂点
    private void MoveInstrumentToHand(InstrumentDetailInfo detailInfo)
    {
        var oParent = GetInstrumentRoot();
        Transform instrument = null;
        if (oParent.childCount>0)
        {
            // 因为同一帧换装乐器再播放，第一个节点是已经销毁的上个乐器，拿最后一个
            instrument = oParent.GetChild(oParent.childCount - 1);
        }
        var nParent = GetMusicInstrumentRoot();
        if (instrument!=null&&nParent!=null)
        {
            //乐器预制体节点调节
            instrumentObj = instrument.gameObject;
            instrumentLocalPos = instrument.localPosition;
            instrumentLocalEul = instrument.localEulerAngles;
            instrumentLocalSca = instrument.localScale;
            instrument.SetParent(nParent);
            instrument.localPosition = Vector3.zero;
            instrument.localEulerAngles = Vector3.zero;
            instrument.localScale = Vector3.one;
            //乐器微调节点调节
            SetInstrumentPos(detailInfo.pDef);
            SetInstrumentRot(detailInfo.rDef);
            SetInstrumentScl(detailInfo.sDef);
        }
    }
    //乐器结束时要放回乐器背部挂点
    private void MoveInstrumentToBack()
    {
        if (instrumentObj!=null)
        {
            var oParent = GetMusicInstrumentRoot();
            if (oParent!=null)
            {
                var nParent = GetInstrumentRoot();
                //乐器预制体节点调节
                instrumentObj.transform.SetParent(nParent);
                instrumentObj.transform.localPosition = instrumentLocalPos;
                instrumentObj.transform.localEulerAngles = instrumentLocalEul;
                instrumentObj.transform.localScale = instrumentLocalSca;
            }
            instrumentObj = null;
        }
        ClearExpression();
    }
    private void CreateExpression(PlayerAniConfig aniConfig)
    {
        expressionGameObjectList = animationCtrl.CreateExpression(aniConfig);
    }

    private void ClearExpression()
    {
        if (expressionGameObjectList!=null)
        {
            for (int i = 0; i < expressionGameObjectList.Count; i++)
            {
                if (expressionGameObjectList[i]!=null)
                {
                    expressionGameObjectList[i].transform.SetParent(null);
                }
            }
        }
        animationCtrl.ClearExpression(expressionGameObjectList);
        expressionGameObjectList?.Clear();
    }

    #region 预览
    public void PreviewUGCInstrument(InstrumentInfo info,Action<string> onToneDownLoadComplete = null)
    {
        isPreview = true;
        if (info==null)
        {
            return;
        }
        HideMusicHold();
        curInstrumentState = MusicalHoldState.Play;
        _detailInfo = new InstrumentDetailInfo();
        _detailInfo.pDef = info.animDetailInfo.pDef;
        _detailInfo.rDef = info.animDetailInfo.rDef;
        _detailInfo.sDef = info.animDetailInfo.sDef;
        PlayMusicStarAni(info.moveId,()=>
        {
            MoveInstrumentToHand(_detailInfo);
        });
        if (curToneInfo!=null)
        {
            SetAllLongSyllableStop();
        }
        curToneInfo = info.toneInfo;
        DownLoadTone(onToneDownLoadComplete);
    }
    public void PreviewPGCInstrument(string pgcId)
    {
        isPreview = true;
        var iData = DataTables.GetInstrumentConfig(pgcId);
        if (iData==null)
        {
            return;
        }
        HideMusicHold();
        curInstrumentState = MusicalHoldState.Play;
        _detailInfo = new InstrumentDetailInfo();
        _detailInfo.pDef = Vector3.zero;
        _detailInfo.rDef = Vector3.zero;
        _detailInfo.sDef = Vector3.one;
        PlayMusicStarAni(iData.moveId,()=>
        {
            MoveInstrumentToHand(_detailInfo);
        });
        if (curToneInfo!=null)
        {
            SetAllLongSyllableStop();
        }
        curToneInfo = MusicalInstrumentUtils.GetPgcToneInfoByPgcToneId(iData.toneId);
    }
    public void StopPreviewInstrument(bool isPlayEndMove = false)
    {
        ExitPlayInstrument(isPlayEndMove);
    }

    public void SetInstrumentPos(Vector3 pos)
    {
        var trans = GetMusicInstrumentRoot();
        if (trans == null) {
            return;
        }
        trans.localPosition = pos;
    }
    public void SetInstrumentRot(Vector3 rot)
    {
        var trans = GetMusicInstrumentRoot();
        if (trans == null) {
            return;
        }
        trans.localEulerAngles = new Vector3(rot.x-90, rot.y, rot.z);
    }
    public void SetInstrumentScl(Vector3 scl)
    {
        var trans = GetMusicInstrumentRoot();
        if (trans == null) {
            return;
        }
        trans.localScale = scl;
    }
    #endregion

    //场景内游玩联机相关
    public void StartPlayInstrument(MusicalSyncParam param)
    {
        isPreview = false;
        if (param==null||string.IsNullOrEmpty(param.resId))
        {
            return;
        }

        _detailInfo = param.detailInfo;

        bool isUgc = CheckIsUGC(param.resId);
        if (isUgc)
        {
            if (param.detailInfo!=null)
            {
                PlayMusicStarAni(param.moveId,()=>
                {
                    MoveInstrumentToHand(param.detailInfo);
                });
            }
            GetUGCInstrumentInfo(param.resId);
        }
        else
        {
            var iData = DataTables.GetInstrumentConfig(param.resId);
            if (iData==null)
            {
                return;
            }
            PlayMusicStarAni(iData.moveId,()=>
            {
                MoveInstrumentToHand(param.detailInfo);
            });
            curToneInfo = MusicalInstrumentUtils.GetPgcToneInfoByPgcToneId(iData.toneId);
        }
        curInstrumentState = MusicalHoldState.Play;
    }

    private void GetUGCInstrumentInfo(string ugcId)
    {
        JObject req = new JObject()
        {
            ["idList"] = ugcId,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GetClothesBatchInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
        {
            if (this == null) return;
            BatchDetailRsp rspData = JsonConvert.DeserializeObject<BatchDetailRsp>(content);
            if (rspData.skinList == null || rspData.skinList.Count == 0 || rspData.skinList[0].skinActionInfo == null) return;
            curToneInfo = rspData.skinList[0].skinActionInfo.instrumentInfo.toneInfo;
            DownLoadTone();
        },
        (msg) =>
        {
        });
    }

    private void DownLoadTone(Action<string> onToneDownLoadComplete = null)
    {
        MusicalInstrumentManager.Inst.PreLoadToneSyllableFiles(curToneInfo,onToneDownLoadComplete, this.gameObject);
    }
    //场景内游玩联机相关
    public void ExitPlayInstrument(bool isPlayEndMove,Action onComplete = null)
    {
        if (isPlayEndMove && isInstrumentInited)
        {
            PlayMusicEndAni(() =>
            {
                MoveInstrumentToBack();
                onComplete?.Invoke();
            });
        }
        else
        {
            HideMusicHold();
            onComplete?.Invoke();
        }
        //清除缓存数据
        ClearMusicalData();
    }

    private void ClearMusicalData()
    {
        SetAllLongSyllableStop();
        curToneInfo = null;
        _detailInfo = null;
        curInstrumentState = MusicalHoldState.Back;
        isInstrumentInited = false;
        MusicEmoteId = "";
    }

    private bool CheckIsUGC(string resId)
    {
        return DataTables.GetInstrumentConfig(resId) == null;
    }

    //隐藏音乐手持并放到背部
    public void HideMusicHold()
    {
        MoveInstrumentToBack();
    }

    public void SetActiveMusic(bool bo)
    {
        var oParent = GetInstrumentRoot();
        if (oParent != null && oParent.childCount > 0)
        {
            for (int i = 0; i < oParent.childCount; i++)
            {
                oParent.GetChild(i).gameObject.SetActive(bo);
            }
        }
        var root = GetMusicInstrumentRoot();
        if (root!= null && root.childCount > 0)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                root.GetChild(i).gameObject.SetActive(bo);
            }
        }
    }

    private void InitMusicAni(string emoteId,Action<bool,string>downloadComplete = null)
    {
        LoggerUtils.Log("InitMusicAni emoteId："+emoteId);
        isInstrumentInited = false;
        instrumentAniList = DataTables.GetInstrumentAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteId);
        instrumentSingleAniList = instrumentAniList.FindAll(x => x.aniType == PlayAniType.SingleOneTime.ToString());
        var instrumentAniConfig = instrumentAniList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(PlayAniType.SingleLoopStart.ToString()));
        var startAniConfig = instrumentAniConfig.ConvertToAniConfig();

        var playAniList = instrumentAniList.ConvertToAniConfig();
        ClearExpression();
        string initAniId = emoteId;
        MusicEmoteId = emoteId;
        animationCtrl.DownloadAnimationAB(playAniList, (isSuccess) =>
        {
            if (!this || this.gameObject == null)
            {
                return;
            }

            if (isSuccess && initAniId == MusicEmoteId)
            {
                CreateExpression(startAniConfig);
                isInstrumentInited = true;
            }
            downloadComplete?.Invoke(isSuccess,initAniId);
        });
    }

    public void PlayMusicStarAni(string emoteId,Action showAction = null,Action completeAction = null)
    {
        InitMusicAni(emoteId, (isSuccess,aniId) =>
        {
            if (!isSuccess)
            {
                LoggerUtils.LogError("下载乐器动作失败");
                return;
            }

            if (string.IsNullOrEmpty(MusicEmoteId))
            {
                LoggerUtils.Log("当前乐器动作已退出");
                return;
            }

            if (MusicEmoteId != aniId)
            {
                LoggerUtils.Log("当前乐器动作已发生改变");
                if (!string.IsNullOrEmpty(aniId))
                {
                    LoggerUtils.Log("MusicEmoteId:" + MusicEmoteId + "   aniId:" + aniId);
                }
                return;
            }

            var instrumentAniConfig = instrumentAniList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(PlayAniType.SingleLoopStart.ToString()));
            var startAniConfig = instrumentAniConfig.ConvertToAniConfig();
            showAction?.Invoke();

            foreach (var eGo in expressionGameObjectList)
            {
                eGo?.SetActive(true);
            }
            animationCtrl.PlayConfigAni(startAniConfig, () =>
            {
                LoggerUtils.Log("播放完毕");
                PlayMusicIdleAni();
                completeAction?.Invoke();
            });
            canPlaySingleMusic = true;
        });
    }

    public void PlayMusicStarWithNoAni(string emoteId,Action showAction = null,Action completeAction = null)
    {
        InitMusicAni(emoteId, (isSuccess,aniId) =>
        {
            //TODO:将乐器放到手上
            showAction?.Invoke();
            PlayMusicIdleAni();
            completeAction?.Invoke();
        });
        canPlaySingleMusic = true;
    }

    private void PlayMusicIdleAni()
    {
        var loopAniConfig = instrumentAniList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(PlayAniType.SingleLooping.ToString()));
        PlayAnim(loopAniConfig);
        canPlaySingleMusic = true;
    }

    public void PlayMusicEndAni(Action completeAction = null)
    {
        var endAniConfig = instrumentAniList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(PlayAniType.SingleLoopEnd.ToString()));
        PlayAnim(endAniConfig,completeAction);
        canPlaySingleMusic = true;
    }
    public void PlayMusicSyllable(SyllablePlayData syllabledata)
    {
        if (curInstrumentState == MusicalHoldState.Play)
        {
            PlayTone(new List<SyllablePlayData>{syllabledata});
            PlayMusicSingleAni();
        }

    }
    public void PlayMusicSyllable(List<SyllablePlayData> syllabledataList)
    {
        if (curInstrumentState == MusicalHoldState.Play)
        {
            if (IsEmptySyllable(syllabledataList))
            {
                StopTone();
                return;
            }
            PlayTone(syllabledataList);
            PlayMusicSingleAni();
        }
    }

    private bool IsEmptySyllable(List<SyllablePlayData> syllabledataList)
    {
        if (syllabledataList!=null&&syllabledataList.Count>0)
        {
            for (int i = 0; i < syllabledataList.Count; i++)
            {
                if ( syllabledataList[i].SyllId == 0)
                {
                    return true;
                }
            }
        }
        return false;
    }
    private void PlayTone(List<SyllablePlayData> syllabledataList)
    {
        if (curToneInfo != null && syllabledataList != null)
        {
            List<int> syllableIdList = new List<int>();
            syllabledataList.ForEach(x =>
            {
                if (x.SyllId != 0)
                {
                    syllableIdList.Add(x.SyllId);
                }
            });
            PreviewAudioType previewAudioType = isPreview ? PreviewAudioType.TwoD : PreviewAudioType.ThreeD;
            StopTone();
            MusicalInstrumentManager.Inst.PlaySingleSyllable(previewAudioType, curToneInfo, syllableIdList, syllabledataList[0].Length>0?(int)LongShortType.Long:(int)LongShortType.Short, this.gameObject);
        }
    }

    public void StopTone()
    {
        PreviewAudioType previewAudioType = isPreview ? PreviewAudioType.TwoD : PreviewAudioType.ThreeD;
        MusicalInstrumentManager.Inst.StopPreviewSyllable(previewAudioType, curToneInfo, this.gameObject);
    }


    private void PlayMusicSingleAni()
    {
        if (!canPlaySingleMusic)
            return; // 如果在冷却时间内，直接返回不执行

        canPlaySingleMusic = false; // 标记为不可执行

        int count = instrumentSingleAniList.Count;
        int index = UnityEngine.Random.Range(0,count);
        var aniConfig = instrumentSingleAniList[index];

        PlayAnim(aniConfig,() =>
        {
            PlayMusicIdleAni();
        });

        PlayParticle();


        // 启动冷却计时器
        StartCoroutine(Cooldown());
    }

    private IEnumerator Cooldown()
    {
        yield return new WaitForSeconds(cooldownTime);
        canPlaySingleMusic = true; // 冷却时间结束，允许再次执行
    }

    private void PlayAnim(InstrumentAniConfig instrumentAniConfig,Action onComplete = null)
    {
        if(instrumentAniConfig == null) return;

        if (!isInstrumentInited)
        {
            LoggerUtils.Log("乐器动作尚未初始化完");
            onComplete?.Invoke();
            return;
        }

        var aniConfig = instrumentAniConfig.ConvertToAniConfig();
        if (isUseCrossFade)
        {
            animationCtrl.CrossFadeConfigAni(aniConfig, musicCrossFadeTime,onComplete);
            foreach (var eGo in expressionGameObjectList)
            {
                eGo.SetActive(true);
                animationCtrl.CossFadeExpressionAnim(aniConfig,eGo,musicCrossFadeTime);
            }
        }
        else
        {
            animationCtrl.PlayConfigAni(aniConfig, onComplete);
            foreach (var eGo in expressionGameObjectList)
            {
                eGo.SetActive(true);
                animationCtrl.PlayExpressionAnim(aniConfig,eGo);
            }
        }
    }

    private void PlayParticle()
    {
        if (expressionGameObjectList == null) return;
        foreach (var eGo in expressionGameObjectList)
        {
            if (eGo.name.Contains("particle_"))
            {
                var particleList = eGo.GetComponentsInChildren<ParticleSystem>(true);
                if (particleList != null && particleList.Length > 0)
                {
                    foreach (var ps in particleList)
                    {
                        ps.gameObject.SetActive(true);
                        ps.Stop();
                        ps.Play();
                    }
                }
            }
        }
    }
    private void SetAllLongSyllableStop()
    {
        StopTone();
    }
    private void OnDestroy()
    {
        SetAllLongSyllableStop();
    }
}
