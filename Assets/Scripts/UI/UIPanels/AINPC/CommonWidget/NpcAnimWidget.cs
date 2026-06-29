using System;
using System.Collections.Generic;
using BUD.AnimPose;
using Game.Avatar;
using Game.Config;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;

public class NpcAnimWidget : MonoBehaviour
{
    [Header("人物预览")] 
    public AvatarCameraController CameraController;
    public CharacterWrap CharacterWrap;
    public Transform CharacterRoot;
    private PgcNpcIdleBehaviour pgcIdleBehaviour;
    private UgcNpcIdleBehaviour ugcIdleBehaviour;
    
    [Header("动画")] 
    public TabView AnimTabView;
    public TabView EmoContentView;
    
    private List<TabItem> allItems;
    private AINpcInfo _curNpcInfo;
    private AINpcAnimType _curNpcType = AINpcAnimType.Idle;

    public void InitData(AINpcInfo npcInfo)
    {
        _curNpcInfo = npcInfo;
        CreateEmoContent();
        ShowCharacter();
        ChangeAnim();
        CreateAnimTabs();
        SetNpcEmoType(_curNpcType);
    }
    
    private void ShowCharacter()
    {
        var chaData = CharacterData.DeserializeObject(_curNpcInfo.npcAvatarJson);
        if (chaData != null)
        {
            CharacterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(chaData, CharacterRoot);
            CharacterWrap.RefreshAvatar(chaData);
            CameraController.RotateTarget = CharacterRoot;
            CameraController.SetCameraZoom(ViewType.ZoomWholeBody);

            var animationCtrl = CharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            pgcIdleBehaviour = CharacterWrap.Avatar.AddComponent<PgcNpcIdleBehaviour>();
            pgcIdleBehaviour.Init(animationCtrl, true);

            ugcIdleBehaviour = CharacterWrap.Avatar.AddComponent<UgcNpcIdleBehaviour>();
            var playerIkController = CharacterWrap.Avatar.GetComponent<AnimIKController>();
            ugcIdleBehaviour.Init(playerIkController);

        }
    }
    
    private void ChangeAnim()
    {
        bool isPgcRes = _curNpcInfo.animResType == (int)AnimResType.PGC;
        var ikController = CharacterWrap.Avatar.GetComponent<AnimIKController>();
        ikController.ChangeAnimResType(isPgcRes ? AnimResType.PGC : AnimResType.UGC);
        ikController.RemovePropIks();
        Action playAnim = isPgcRes ? pgcIdleBehaviour.PlayMainAnim : ugcIdleBehaviour.PlayMainAnim;
        playAnim.Invoke();
        SetNpcEmoType(_curNpcType);
    }

    private void CreateAnimTabs()
    {
        for (var i = 0; i < GameConsts.AnimTabs.Length; i++)
        {
            int index = i;
            var item = AnimTabView.CreateItem(i.ToString(), GameConsts.AnimTabs[i]);
            item.RemoveAllListener();
            item.AddValueChangeCallListener(isOn =>
            {
                if (isOn)
                {
                    AINpcAnimType npcType = (AINpcAnimType)index;
                    SetNpcEmoType(npcType);
                }
            });
        }
    }

    private void SetNpcEmoType(AINpcAnimType npcType)
    {
        _curNpcType = npcType;
        allItems.ForEach(x =>
        {
            x.RemoveAllListener();
            x.SetIsSelectWithoutCallback(false);
            x.gameObject.SetActive(false);
        });

        if (_curNpcInfo.animResType == (int)AnimResType.PGC)
        {
            var data = _curNpcInfo.npcAnimations?.Find(x => x.npcAnimationType == (int)npcType);
            if (data == null || data.pgcIdleList == null || data.pgcIdleList.Count == 0)
            {
                return;
            }

            for (var i = 0; i < data.pgcIdleList.Count; i++)
            {
                string id = data.pgcIdleList[i];
                var uiConfig = Es.DataTables.GetEmoUIConfig(id);
                string aniName = string.Empty;
                if (id.Equals("leisure"))
                {
                    aniName = LocalizationManager.Inst.GetLocalizedText("默认");
                }
                else if (id.Equals("default"))
                {
                    aniName = LocalizationManager.Inst.GetLocalizedText("站立");
                }
                else
                {
                    aniName = uiConfig == null ? LocalizationManager.Inst.GetLocalizedText("默认") : uiConfig.name;
                }

                allItems[i].gameObject.SetActive(true);
                allItems[i].SetShowName(aniName);
                allItems[i].AddValueChangeCallListener(isOn =>
                {
                    if (isOn)
                    {
                        Action<string> playAnim = _curNpcType == AINpcAnimType.Idle
                            ? pgcIdleBehaviour.PlayMainAnimByUI
                            : pgcIdleBehaviour.PlaySubAnimByUI;
                        playAnim?.Invoke(id);
                    }
                });
            }
        }
        else
        {
            var data = _curNpcInfo.npcAnimations?.Find(x => x.npcAnimationType == (int)npcType);
            if (data == null || data.ugcIdleList == null || data.ugcIdleList.Count == 0)
            {
                return;
            }

            for (var i = 0; i < data.ugcIdleList.Count; i++)
            {
                var ugcData = data.ugcIdleList[i];
                allItems[i].gameObject.SetActive(true);
                allItems[i].SetShowName(ugcData.aniName);
                allItems[i].AddValueChangeCallListener(isOn =>
                {
                    if (isOn)
                    {
                        Action<NpcUgcIdleData> playAnim = _curNpcType == AINpcAnimType.Idle
                            ? ugcIdleBehaviour.PlayMainAnimByUI
                            : ugcIdleBehaviour.PlaySubAnimByUI;
                        playAnim?.Invoke(ugcData);
                    }
                });
            }
        }
    }

    private void CreateEmoContent()
    {
        allItems = new List<TabItem>();
        for (int i = 0; i < 5; i++)
        {
            var item = EmoContentView.CreateItem(i.ToString());
            item.Init();
            allItems.Add(item);
        }
    }
    
}
