using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BUD.AnimPose;
using GameData.PgcData;
using GameData.UGCData;
using GameSync.Manager;
using GameUI;
using Message;
using UI.Base;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.LobbyCharacterIdlePanel;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    partial class IncubationCabinRolesPanel
    {
        List<GameObject> actionCreateItemPrefabList = new List<GameObject>();
        List<GameObject> actionGetMoreItemPrefabList = new List<GameObject>();
        List<GameObject> actionRoleItemPrefabList = new List<GameObject>();
        List<GameObject> actionRoleItem2PrefabList = new List<GameObject>();

        protected LobbyRoleType curRoleType = LobbyRoleType.Avatar;
        protected HallAnimType curAnimType = HallAnimType.NotInteractive;
        protected int curMainActionIndex = 0;
        protected int curSubActionIndex = 0;
        protected SecondTabs.Tab curThirdTab;

        private List<QuickPoseData> officialLoopAllDatas;
        private List<QuickPoseData> officialNonLoopAllDatas;
        private List<QuickPoseData> communityLoopAllDatas;
        private List<QuickPoseData> communityNonLoopAllDatas;

        public void InitActionUI()
        {
            curMainActionIndex = 0;
            curSubActionIndex = 0;
            mainActionBtnToggleParent.onSelect -= OnMainActionBtnToggleSelect;
            mainActionBtnToggleParent.onSelect += OnMainActionBtnToggleSelect;
            mainActionBtnToggleParent.Init(0);
            subActionBtnToggleParent.onSelect -= OnSubActionBtnToggleSelect;
            subActionBtnToggleParent.onSelect += OnSubActionBtnToggleSelect;
            subActionBtnToggleParent.Init(0);

            actionCreateItemPrefab.SetActive(false);
            actionGetMoreItemPrefab.SetActive(false);
            actionRoleItemPrefab.SetActive(false);
            actionRoleItem2Prefab.SetActive(false);

            GetActionDatas();
        }

        public void GetActionDatas()
        {
            if (curMainActionIndex == 0)
            {
                if (curSubActionIndex == 0)
                {
                    if (officialLoopAllDatas == null)
                    {
                        officialLoopAllDatas = new();
                        DataLoader.GetOfficialLoopDatas(UgcPoseSubType.Single, (datas) =>
                        {
                            officialLoopAllDatas.AddRange(datas);
                        }, this.gameObject);
                    }
                }
                else if (curSubActionIndex == 2)
                {
                    if (communityLoopAllDatas == null)
                    {
                        communityLoopAllDatas = new();
                        DataLoader.GetCommunityLoopDatas(UgcPoseSubType.Single, (datas) =>
                        {
                            communityLoopAllDatas.AddRange(datas);
                        }, this.gameObject);
                    }
                }
            }
            else if (curMainActionIndex == 1)
            {
                if (curSubActionIndex == 0)
                {
                    if (officialNonLoopAllDatas == null)
                    {
                        officialNonLoopAllDatas = new();
                        DataLoader.GetOfficialNonLoopDatas(UgcPoseSubType.Single, (datas) =>
                        {
                            officialNonLoopAllDatas.AddRange(datas);
                        }, this.gameObject);
                    }
                }
                else if (curSubActionIndex == 2)
                {
                    if (communityNonLoopAllDatas == null)
                    {
                        communityNonLoopAllDatas = new();
                        DataLoader.GetCommunityNonLoopDatas(UgcPoseSubType.Single, (datas) =>
                        {
                            communityNonLoopAllDatas.AddRange(datas);
                        }, this.gameObject);
                    }
                }
            }
        }

        GameObject GetActionItemFromPool(int type)
        {
            GameObject prefab = null;
            List<GameObject> prefabList = null;
            GameObject tempPrefab = null;
            switch (type)
            {
                case 0:
                    prefabList = actionCreateItemPrefabList;
                    tempPrefab = actionCreateItemPrefab;
                    break;
                case 1:
                    prefabList = actionGetMoreItemPrefabList;
                    tempPrefab = actionGetMoreItemPrefab;
                    break;
                case 2:
                    prefabList = actionRoleItemPrefabList;
                    tempPrefab = actionRoleItemPrefab;
                    break;
                case 3:
                    prefabList = actionRoleItem2PrefabList;
                    tempPrefab = actionRoleItem2Prefab;
                    break;
            }
            if (prefabList.Count > 0)
            {
                prefab = prefabList[0];
                prefabList.RemoveAt(0);
            }
            else
            {
                prefab = GameObject.Instantiate(tempPrefab, actionScrollRect.content);
            }
            prefab.SetActive(true);
            return prefab;
        }

        public void ReturnActionItemToPool(GameObject prefab, int type)
        {
            switch (type)
            {
                case 0:
                    actionCreateItemPrefabList.Add(prefab);
                    break;
                case 1:
                    actionGetMoreItemPrefabList.Add(prefab);
                    break;
                case 2:
                    actionRoleItemPrefabList.Add(prefab);
                    break;
                case 3:
                    actionRoleItem2PrefabList.Add(prefab);
                    break;
                default:
                    break;
            }
            prefab.SetActive(false);
            prefab.transform.SetParent(poolTransRoot);
            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localRotation = Quaternion.identity;
            prefab.transform.localScale = Vector3.one;
        }

        int GetSelectedClassType()
        {
            EmoteSubType subType;
            switch (curThirdTab)
            {
                case SecondTabs.Tab.Main:
                    if (curAnimType == HallAnimType.NotInteractive)
                    {
                        subType = curRoleType == LobbyRoleType.Avatar
                            ? EmoteSubType.SingleLoop
                            : EmoteSubType.PetSingleLoop;
                    }
                    else
                    {
                        subType = EmoteSubType.PetWithPlayerLoop;
                    }
                    return UniqueType.Get(ResourceType.Emote, (int)subType);
                case SecondTabs.Tab.Sub:
                    if (curAnimType == HallAnimType.NotInteractive)
                    {
                        subType = curRoleType == LobbyRoleType.Avatar ? EmoteSubType.Single : EmoteSubType.PetSingle;
                    }
                    else
                    {
                        subType = EmoteSubType.PetWithPlayer;
                    }
                    return UniqueType.Get(ResourceType.Emote, (int)subType);
            }
            return 0;
        }

        void OnMainActionBtnToggleSelect(int index)
        {
            curMainActionIndex = index;
            RfreshActionContent();
        }

        void OnSelectMainActionItem()
        {
        }

        void OnSelectOfficialActionItem()
        {
        }

        void OnSubActionBtnToggleSelect(int index)
        {
            curSubActionIndex = index;
            RfreshActionContent();
        }

        void OnSelectSubActionItem()
        {
        }

        void OnSelectCommunityActionItem()
        {
        }

        void RfreshActionContent()
        {
            GetActionDatas();
            int childCount = actionScrollRect.content.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Destroy(actionScrollRect.content.GetChild(i).gameObject);
            }
            if (curMainActionIndex == 0)
            {
                if (curSubActionIndex == 0)
                {
                    if (officialLoopAllDatas == null) return;
                    for (int i = 0; i < officialLoopAllDatas.Count; i++)
                    {
                        if (officialLoopAllDatas[i]?.poseInfo == null) continue;
                        var go = GetActionItemFromPool(2);
                        go.GetComponent<ActionRoleItem>().Init(officialLoopAllDatas[i].poseInfo);
                    }
                }
                else if (curSubActionIndex == 2)
                {
                    if (communityLoopAllDatas == null) return;
                    var go = GetActionItemFromPool(1);
                    go.GetComponent<ActionGetMoreItem>().Init();
                    for (int i = 0; i < communityLoopAllDatas.Count; i++)
                    {
                        if (communityLoopAllDatas[i]?.poseInfo == null) continue;
                        var go2 = GetActionItemFromPool(2);
                        go2.GetComponent<ActionRoleItem>().Init(communityLoopAllDatas[i].poseInfo);
                    }
                }
            }
            else if (curMainActionIndex == 1)
            {
                if (curSubActionIndex == 0)
                {
                    if (officialNonLoopAllDatas == null) return;
                    for (int i = 0; i < officialNonLoopAllDatas.Count; i++)
                    {
                        if (officialNonLoopAllDatas[i]?.poseInfo == null) continue;
                        var go = GetActionItemFromPool(2);
                        go.GetComponent<ActionRoleItem>().Init(officialNonLoopAllDatas[i].poseInfo);
                    }
                }
                else if (curSubActionIndex == 2)
                {
                    if (communityNonLoopAllDatas == null) return;
                    var go = GetActionItemFromPool(1);
                    go.GetComponent<ActionGetMoreItem>().Init();
                    for (int i = 0; i < communityNonLoopAllDatas.Count; i++)
                    {
                        if (communityNonLoopAllDatas[i]?.poseInfo == null) continue;
                        var go2 = GetActionItemFromPool(2);
                        go2.GetComponent<ActionRoleItem>().Init(communityNonLoopAllDatas[i].poseInfo);
                    }
                }
            }
        }

        public void RefreshAction()
        {
            curMainActionIndex = 0;
            curSubActionIndex = 0;
            RfreshActionContent();
        }
    }
}