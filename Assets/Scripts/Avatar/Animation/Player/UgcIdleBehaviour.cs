using System;
using System.Collections.Generic;
using BUD.AnimPose;
using Es;
using GameData.BaseInfo;
using GameData.PgcData;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using xasset;
using Random = UnityEngine.Random;

public class UgcIdleBehaviour : MonoBehaviour
{
    private Dictionary<string,AnimFrameData> ugcAnimInfos = new Dictionary<string, AnimFrameData>();
    private AnimIKController ikController;
    private IdleData mData;
    private float time;
    private bool isMainPlaying;
    private bool isNeedSound;
    private const float MinLoopTime = 15;
    private UgcAnimSubType ugcAnimType;

    public void Init(AnimIKController controller, bool needSound = false)
    {
        ikController = controller;
        isNeedSound = needSound;
        ikController.SetBgmEnable(needSound);
    }

    private void OnEnable()
    {
        PlayMain();
    }

    public void SetData(UgcAnimSubType animType,IdleData data)
    {
        ugcAnimType = animType;
        mData = data;
    }


    public void PlayerAnim(string id,Action complete,bool isLoop = false)
    {
        UgcIdleData idleData = mData.ugcIdleList?.Find(x => x.id.Equals(id));

        Action playUgcAnim = () =>
        {
            var frameData = ugcAnimInfos[id];
            var animTypeValue = (int) ugcAnimType;
            ikController.RemovePropIks();
            ikController.CreatePropIKs((UgcPoseSubType) animTypeValue, idleData.propList, false,"Default");
            ikController.SetAnimFrameData(frameData);
            if (isLoop)
            {
                ikController.PlayLoopAnim(complete);
            }
            else
            {
                ikController.PlayOnceAnim(complete);
            }

        };
        
        if (idleData != null)
        {
            // 播 UGC 表情时隐藏特殊皮肤特效避免穿模（仅 UI 预览 + 特殊皮肤生效，其余 no-op）；恢复由 ResetEmoteForUICharacter 处理
            GetComponentInChildren<PlayerAnimationCtrl>()?.NotifyUIEmoteBegin();
            if (!ugcAnimInfos.ContainsKey(id))
            {
                DownloadAnim(id, idleData.metaDataUrl, (isSuccess) => 
                {
                    if (isSuccess)
                    {
                        playUgcAnim();
                    }
                });
            }
            else
            {
                playUgcAnim();
            }
        }
    }
    
    
    
    private void DownloadAnim(string id,string url,Action<bool> callback)
    {
        if (string.IsNullOrEmpty(url))
        {
            LoggerUtils.LogError("url is null "+url);
            return;
        }

        var localUrl = url.StartsWith("https://Assets/") ? url.Substring(8) : url;
        if (localUrl.Contains("Assets/"))
        {
            var wrapper = Loader.Load<TextAsset>(localUrl);
            if (wrapper == null)
            {
                LoggerUtils.LogError("localOfficialConfig  read Error");
                return;
            }
            var content = wrapper.RetainAsset(this.gameObject).text;
            var frameData = JsonConvert.DeserializeObject<AnimFrameData>(content);
            ugcAnimInfos[id] = frameData;
            callback?.Invoke(true);
        }
        else
        {
            var assetRequest = Asset.LoadRemoteAssetAsync(url);
            if (assetRequest != null)
            {
                assetRequest.completed += (_) =>
                {
                    if (assetRequest.result == Request.Result.Success)
                    {
                        var content = System.Text.Encoding.UTF8.GetString(assetRequest.asset);
                        var frameData = JsonConvert.DeserializeObject<AnimFrameData>(content);
                        ugcAnimInfos[id] = frameData;
                    }
                    callback?.Invoke(assetRequest.result == Request.Result.Success);
                };
            }
        }


    }

    
    private void Update()
    {
        if (mData != null && !string.IsNullOrEmpty(mData.mainIdle))
        {
            time += Time.deltaTime;
        }
    }
    public void PlayMain()
    {
        if (mData != null)
        {
            isMainPlaying = true;
            PlayerAnim(mData.mainIdle, PlayMainComplete, true);
        }
    }

    public void RemovePropIKs()
    {
        if (ikController != null)
        {
            ikController.RemovePropIks();
        }
    }

    public void PlayMainAnimByUI(string id)
    {
        PlayerAnim(id, PlayMainComplete,true);
    }  

    public void PlaySub()
    {
        if (mData != null && mData.subIdle != null && mData.subIdle.Count != 0)
        {
            ikController.StopAnim();
            string emoteID = mData.subIdle[Random.Range(0, mData.subIdle.Count)];
            PlayerAnim(emoteID, PlaySubComplete);
        }
    }

    public void PlaySubAnimByUI(string id)
    {
        ikController.StopAnim();
        PlayerAnim(id, PlaySubComplete);
    }  

    public void ResetData()
    {
        mData = null;
        ikController.StopAnimAndResetJointNode();
    }

    private void PlayMainComplete()
    {
        if (time > MinLoopTime)
        {
            PlaySub();
        }
    }

    private void PlaySubComplete()
    {
        time = 0;
        ikController.StopAnim();
        PlayMain();
    }
}