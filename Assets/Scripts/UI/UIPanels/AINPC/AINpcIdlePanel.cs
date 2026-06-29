using System;
using System.Collections;
using System.Collections.Generic;
using BUD.AnimPose;
using Com.TheFallenGames.OSA.DataHelpers;
using Game.Avatar;
using Game.Config;
using Game.Store;
using GameData.BaseInfo;
using GameData.PgcData;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.LobbyCharacterIdlePanel;
using UnityEngine;
using UnityEngine.UI;

public class AINpcIdlePanel : BasePanel<AINpcIdlePanel>
{
    [SerializeField] private CButton returnBtn;
    [SerializeField] private CButton resetButton;
    [SerializeField] internal Transform transBg;
    [SerializeField] private Transform characterRoot;
    [SerializeField] private AvatarCameraController avatarCameraController;
    [SerializeField] internal SecondTabs secondTabsUI;
    [SerializeField] private Transform secondTransform;
    [SerializeField] private Toggle secondTogglePrefab;
    [SerializeField] private AINpcPGCLobbyIdleView pgcView;
    [SerializeField] private AINpcUGCLobbyIdleView ugcView;
    private AINpcBaseLobbyIdleView curView;
    private CharacterWrap characterWrap;
    private AINpcInfo npcInfo;
    
    private List<Toggle> tabToggles = new List<Toggle>();
    public Action OnComplete { set; private get; }
    public override void OnCreate()
    {
        base.OnCreate();
        returnBtn.onClick.AddListener(OnReturnClick);
        resetButton.onClick.AddListener(OnResetClick);
        secondTabsUI.SetCallback(OnSecondTabs);
        pgcView.OnCreate(OnResetClick);
        ugcView.OnCreate(OnResetClick);
        CreateAnimTabs();
        InitUI();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        npcInfo = (AINpcInfo) args[0];
        CreateAvatar();
        pgcView.OnShow(npcInfo,characterWrap);
        ugcView.OnShow(npcInfo,characterWrap);
        bool isPgcRes = npcInfo.animResType == (int) AnimResType.PGC;
        curView = isPgcRes ? pgcView : ugcView;
        curView.PlayMainAnim();
        secondTabsUI.DefualtOn(isPgcRes ? SecondTabs.Tab.Main : SecondTabs.Tab.Sub);
    }

    private void OnSecondTabs(SecondTabs.Tab tab)
    {
        bool isPgcRes = tab == SecondTabs.Tab.Main;
        curView = isPgcRes ? pgcView : ugcView;
        pgcView.gameObject.SetActive(isPgcRes);
        ugcView.gameObject.SetActive(!isPgcRes);
        if (tabToggles[0].isOn)
        {
            OnAnimValueChange(AINpcAnimType.Idle, true);
            return;
        }
        tabToggles[0].isOn = true;
    }

    private void CreateAvatar()
    {
        var saveCharacterData =  CharacterData.DeserializeObject(npcInfo.npcAvatarJson);
        if (saveCharacterData != null)
        {
            characterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData,characterRoot);
            var animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarCameraController.RotateTarget = characterRoot;
            var avatarIdleBehaviour = characterWrap.Avatar.AddComponent<PgcNpcIdleBehaviour>();
            avatarIdleBehaviour.Init(animationCtrl, true);
        }
    }
    
    private void InitUI()
    {
        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        string prefabPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab";
        var itemObj = Loader.Load<GameObject>(prefabPath).Instantiate(transBg);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        {
            "AvatarBg_icon1","AvatarBg_icon5" ,"AvatarBg_icon3","AvatarBg_icon4","AvatarBg_icon2"
        });
        item.gameObject.SetActive(true);
    }

    private void CreateAnimTabs()
    {
        for (var i = 0; i < GameConsts.AnimTabs.Length; i++)
        {
            AINpcAnimType npcType = (AINpcAnimType) i;
            var toggle = GameObject.Instantiate(secondTogglePrefab, secondTransform);
            toggle.GetComponentInChildren<Text>().text = GameConsts.AnimTabs[i];
            toggle.onValueChanged.AddListener(isOn =>
            {
                OnAnimValueChange(npcType,isOn);
            });
            tabToggles.Add(toggle);
        }
        secondTogglePrefab.gameObject.SetActive(false);
    }

    private void OnAnimValueChange(AINpcAnimType npcType,bool isOn)
    {
        if (isOn)
        {
            curView.UpdateAssetList(npcType);
        }
    }

  
    private void OnResetClick()
    {
        npcInfo.npcAnimations?.Clear();
        npcInfo.animResType = (int)AnimResType.PGC;
        curView.ResetAssetList();
    }

    public void OnReturnClick()
    {
        if (IsEmptyAnimtions(npcInfo))
        {
            npcInfo.animResType = (int)AnimResType.PGC;
        }
        OnComplete?.Invoke();
        CloseSelf();
    }

    public static bool IsEmptyAnimtions(AINpcInfo info)
    {
        if (info.npcAnimations != null)
        {
            if (info.animResType == (int) AnimResType.PGC)
            {
                List<string> pgcList = new List<string>();
                foreach (var npcAnim in info.npcAnimations)
                {
                    if (npcAnim.pgcIdleList != null)
                    {
                        pgcList.AddRange(npcAnim.pgcIdleList);
                    }
                }
                return pgcList.Count == 0;
            }
            else
            {
                List<UgcIdleData> ugcList = new List<UgcIdleData>();
                foreach (var npcAnim in info.npcAnimations)
                {
                    if (npcAnim.ugcIdleList != null)
                    {
                        ugcList.AddRange(npcAnim.ugcIdleList);
                    }
                }
                return ugcList.Count == 0;
            }
        }
        return true;
    }

}
