using System;
using System.Collections.Generic;
using BUD.AnimPose;
using Es;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;
using UnityEngine;
using xasset;
using Random = UnityEngine.Random;

public class UgcNpcIdleBehaviour: MonoBehaviour
{
    private Dictionary<string,AnimFrameData> ugcAnimInfos = new Dictionary<string, AnimFrameData>();
    private AnimIKController ikController;
    private List<AINpcIdleAnim> mData;
    private float time;
    private bool isMainPlaying;
    private bool isNeedSound;
    private const float MinLoopTime = 15;
    private NpcUgcIdleData defaultUgcIdleData;
    private bool isContainSubAnim = true;
    public void Init(AnimIKController controller, bool needSound = false)
    {
        ikController = controller;
        isNeedSound = needSound;
        ikController.SetBgmEnable(needSound);
        InitDefaultIdleData();
    }

    private void InitDefaultIdleData()
    {
        var wrapper = Loader.Load<TextAsset>($"Assets/Arts/Config/CustomAnimConfig/AnimInfo_{(int)UgcAnimSubType.Single}.json");
        if (wrapper == null)
        {
            LoggerUtils.LogError("localOfficialConfig  read Error");
            return;
        }
        var content = wrapper.RetainAsset(this.gameObject).text;
        var animInfo = JsonConvert.DeserializeObject<AnimInfo>(content);
        defaultUgcIdleData = new NpcUgcIdleData()
        {
            id = animInfo.id,
            aniName = LocalizationManager.Inst.GetLocalizedText("默认"),
            metaDataUrl = animInfo.metaDataUrl,
            cover = animInfo.cover,
            propList = animInfo.propList
        };
    }

    private void OnEnable()
    {
        PlayMainAnim();
    }

    public void SetData(List<AINpcIdleAnim> data)
    {
        mData = data;
    }


    public void PlayerAnim(NpcUgcIdleData data,Action complete,bool isLoop = false)
    {
        Action playUgcAnim = () =>
        {
            var frameData = ugcAnimInfos[data.id];
            var animTypeValue = (int) UgcAnimSubType.Single;
            ikController.RemovePropIks();
            ikController.CreatePropIKs((UgcPoseSubType) animTypeValue, data.propList, false,"Default");
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
        
        if (data != null)
        {
            if (!ugcAnimInfos.ContainsKey(data.id))
            {
                DownloadAnim(data.id, data.metaDataUrl, (isSuccess) => 
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
        if (mData != null)
        {
            time += Time.deltaTime;
        }
    }
    
    private List<NpcUgcIdleData> GetUgcListByNpcType(int npcType)
    {
        var idleAnim = mData?.Find(x => x.npcAnimationType == npcType);
        if (idleAnim != null)
        {
            return idleAnim.ugcIdleList;
        }
        return null;
    }
    
    public void PlayMainAnim()
    {
        var ugcList = GetUgcListByNpcType((int)AINpcAnimType.Idle);
        if (ugcList == null)
        {
            ugcList = new List<NpcUgcIdleData>();
        }
        if (ugcList.Count == 0)
        {
            ugcList.Add(defaultUgcIdleData);
        }

        int index = Random.Range(0, ugcList.Count);
        var data = ugcList[index];
        PlayerAnim(data, PlayMainComplete, true);
        isMainPlaying = true;
    }

    public void SetSubAnimState(bool containSubAnim)
    {
        isContainSubAnim = containSubAnim;
    }

    public void RemovePropIKs()
    {
        if (ikController != null)
        {
            ikController.RemovePropIks();
        }
    }

    public void PlayMainAnimByUI(NpcUgcIdleData data)
    {
        PlayerAnim(data, PlayMainComplete,true);
    }  

    
    private void PlayDefaultSub()
    {
        if (isContainSubAnim)
        {
            PlaySubAnimByNpcType((int)AINpcAnimType.Assist);
        }
        else
        {
            time = 0;
            PlayMainAnim();
        }
    }
    
    public void PlaySubAnimByNpcType(int npcType)
    {
        var ugcList = GetUgcListByNpcType(npcType);
        if (ugcList == null || ugcList.Count == 0)
        {
            return;
        }
        ikController.StopAnim();
        int index = Random.Range(0, ugcList.Count);
        var ugcData = ugcList[index];
        PlayerAnim(ugcData, PlaySubComplete);
    }

    
    public void PlaySubAnimByUI(NpcUgcIdleData data)
    {
        ikController.StopAnim();
        PlayerAnim(data, PlaySubComplete);
    }  

    public void ResetData()
    {
        mData = null;
        ikController.StopAnimAndResetJointNode();
    }

    private void PlayMainComplete()
    {
        if (time > MinLoopTime && isContainSubAnim)
        {
            PlayDefaultSub();
        }
    }

    private void PlaySubComplete()
    {
        time = 0;
        ikController.StopAnim();
        PlayMainAnim();
    }    
}