/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-05-22 18:28:03
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-06-20 11:26:22
 * @ Description: 领奖台控制器
 */

using System;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;
using Game.Avatar;
using Network;
using Network.Http;
using Newtonsoft.Json.Linq;

public class PlayerPodiumController : MonoBehaviour
{
    public Transform[] playersGo;
    public GameObject effectGo;
    public Camera viewCamera;

    private List<PodiumRoleController> podiumRoleCtrList = new List<PodiumRoleController>();
    private List<string> currentLoadingUid = new List<string>(); // 当前正在加载的Uid列表
    private Dictionary<string, CharacterData> userRoleDataCache = new Dictionary<string, CharacterData>();

    class PodiumRoleController
    {
        public int Index;
        public CharacterWrap CharacterWrap;
        public bool IsLoaded = false;
        public bool IsUsed = false;
        public bool IsDate = false;
    }

    List<List<string>> AwradAnimConfig = new List<List<string>>()
    {
        new List<string>(){StateName.PodiumAwrad_FirstStart, StateName.PodiumAwrad_FirstCentre},
        new List<string>(){StateName.PodiumAwrad_SecondStart, StateName.PodiumAwrad_SecondCentre},
        new List<string>(){StateName.PodiumAwrad_ThirdStart, StateName.PodiumAwrad_ThirdCentre},
    };

    public void ShowPlayers(List<string> uids)
    {
        Reset();

        var count = uids.Count > 3 ? 3 : uids.Count;// 目前只显示前三名
        for (int i = 0; i < count; i++)
        {
            var index = i;
            currentLoadingUid.Add(uids[i]);
            if (userRoleDataCache.TryGetValue(uids[i], out var roleData))
            {
                LoadPlayerByIndex(i, roleData);
            }
            else
            {
                GetUserInfo(uids[i], (userInfo) =>
                {
                    OnSuccess(index,userInfo);
                },OnFail);
            }
            if (podiumRoleCtrList.Count > i) podiumRoleCtrList[i].IsUsed = true;
        }
        
        //StopAllCoroutines();
        //StartCoroutine(LoadOver());
    }

    private void GetUserInfo(string uid, Action<AccountUserInfo> successAction ,Action<string> fallAction = null)
    {
        var jb = new JObject
        {
            ["targetUid"] = uid
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.getUserImage,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            onReceive: arg0 =>
            {
                GetImageRes resData = JsonConvert.DeserializeObject<GetImageRes>(arg0);
                successAction?.Invoke(resData.userInfo);
            },
            onFail: arg0 =>
            {
                fallAction?.Invoke(arg0);
            },
            retryCount:1);
    }

    private void Awake()
    {
        // effectGo?.SetActive(false);

        for (int i = 0; i < playersGo.Length; i++)
        {
            var characterWrap = AvatarController.Inst.CreateUIAvatar(AvatarDataManager.Inst.GetDefaultDataByGender(1));
            characterWrap.SetParent(playersGo[i], true);
            characterWrap.Avatar.SetActive(false);
            podiumRoleCtrList.Add(new PodiumRoleController() { CharacterWrap = characterWrap, Index = i });
        }
    }
    
    private void OnSuccess(int index, AccountUserInfo userInfo)
    {
        var uid = userInfo.uid;
        CharacterData roleData = userInfo.avatarInfo;
        userRoleDataCache.TryAdd(uid, roleData);
        
        if (currentLoadingUid.Count > index && uid == currentLoadingUid[index]) // 判断是否是同一批次的请求
        {
            LoadPlayerByIndex(index, roleData);
        }
    }

    private void OnFail(string error)
    {

    }

    private void LoadPlayerByIndex(int index, CharacterData roleData)
    {
        if (index < podiumRoleCtrList.Count)
        {
            var controller = podiumRoleCtrList[index];
            if (!controller.IsLoaded)
            {
                // controller.CharacterWrap.Avatar?.SetActive(false);
                controller.IsLoaded = true;
            }
            controller.IsDate = true;
            controller.CharacterWrap.Avatar.SetActive(false);
            controller.CharacterWrap.RefreshAvatar(roleData, ()=>
            {
                controller.CharacterWrap.Avatar.SetActive(true);
                var animList = AwradAnimConfig[controller.Index];
                var animator = controller.CharacterWrap.Avatar.GetComponent<Animator>();

                animator.Play(animList[0]);
            });
        }
    }

    private void OnEnable()
    {
        foreach(var controller in podiumRoleCtrList)
        {
            if (controller.CharacterWrap.Avatar.activeSelf)
            {
                var animList = AwradAnimConfig[controller.Index];
                var animator = controller.CharacterWrap.Avatar.GetComponent<Animator>();

                animator.Play(animList[0]);
            }
        }
    }

    private void Reset()
    {
        // effectGo?.SetActive(false);
        LoggerUtils.Log("PlayerPodiumController.Reset");
        foreach (var item in podiumRoleCtrList)
        {
            item.IsUsed = false;
            item.IsDate = false;
            // item.CharacterWrap.Avatar?.SetActive(false);
        }

        currentLoadingUid.Clear();
    }
}