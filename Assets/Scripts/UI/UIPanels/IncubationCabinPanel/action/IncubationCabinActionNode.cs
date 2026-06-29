using System.Collections.Generic;
using BUD.AnimPose;
using GameData.PgcData;
using GameData.UGCData;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.LobbyCharacterIdlePanel;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public class IncubationCabinActionNode : MonoBehaviour
    {
        [SerializeField] internal CabinBtnToggleParent mainActionBtnToggleParent;
        [SerializeField] internal CabinBtnToggleParent subActionBtnToggleParent;
        [SerializeField] internal ScrollRect actionScrollRect;
        [SerializeField] internal Text tipTxt;
        [SerializeField] internal Transform poolTransRoot;
        [SerializeField] internal GameObject actionCreateItemPrefab;
        [SerializeField] internal GameObject actionGetMoreItemPrefab;
        [SerializeField] internal GameObject actionRoleItemPrefab;
        [SerializeField] internal GameObject actionRoleItem2Prefab;
        [SerializeField] internal FittingRoomAdapter assetsList;
        [SerializeField] internal IncubationCabinDataLoader dataLoader;

        private IncubationCabinPanel _panel;
        private LobbyCharacterIdlePanel.LobbyCharacterIdlePanel lobbyCharacterIdlePanel;
        private LobbyRoleType curRoleType = LobbyRoleType.Avatar;
        private HallAnimType curAnimType = HallAnimType.NotInteractive;
        private int curMainActionIndex = 0; // 0:循环动画 1:非循环动画
        private int curSubActionIndex = 0; // 0:官方 1:我创作的 2:社区购买的
        private SecondTabs.Tab curThirdTab;

        private List<GameObject> actionCreateItemPrefabList = new List<GameObject>();
        private List<GameObject> actionGetMoreItemPrefabList = new List<GameObject>();
        private List<GameObject> actionRoleItemPrefabList = new List<GameObject>();
        private List<GameObject> actionRoleItem2PrefabList = new List<GameObject>();

        private List<QuickPoseData> officialLoopAllDatas;
        private List<QuickPoseData> officialNonLoopAllDatas;
        private List<QuickPoseData> communityLoopAllDatas;
        private List<QuickPoseData> communityNonLoopAllDatas;

        internal void Init(IncubationCabinPanel panel)
        {
            _panel = panel;
        }

        public void InitActionUI()
        {
            curMainActionIndex = 0;
            curSubActionIndex = 0;
            mainActionBtnToggleParent.Init(0);
            mainActionBtnToggleParent.onSelect += OnMainActionBtnToggleSelect;
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
                        officialLoopAllDatas = new List<QuickPoseData>();
                        dataLoader.GetOfficialLoopDatas(UgcPoseSubType.Single, (datas) =>
                        {
                            officialLoopAllDatas.AddRange(datas);
                        }, this.gameObject);
                    }
                }
                else if (curSubActionIndex == 2)
                {
                    if (communityLoopAllDatas == null)
                    {
                        communityLoopAllDatas = new List<QuickPoseData>();
                        dataLoader.GetCommunityLoopDatas(UgcPoseSubType.Single, (datas) =>
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
                        officialNonLoopAllDatas = new List<QuickPoseData>();
                        dataLoader.GetOfficialNonLoopDatas(UgcPoseSubType.Single, (datas) =>
                        {
                            officialNonLoopAllDatas.AddRange(datas);
                        }, this.gameObject);
                    }
                }
                else if (curSubActionIndex == 2)
                {
                    if (communityNonLoopAllDatas == null)
                    {
                        communityNonLoopAllDatas = new List<QuickPoseData>();
                        dataLoader.GetCommunityNonLoopDatas(UgcPoseSubType.Single, (datas) =>
                        {
                            communityNonLoopAllDatas.AddRange(datas);
                        }, this.gameObject);
                    }
                }
            }
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

        public void RefreshAction()
        {
            curMainActionIndex = 0;
            curSubActionIndex = 0;
            RfreshActionContent();
        }

        private GameObject GetActionItemFromPool(int type)
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
                prefab = Instantiate(tempPrefab, actionScrollRect.content);
            }
            prefab.SetActive(true);
            return prefab;
        }

        private void RfreshActionContent()
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
                    for (int i = 0; i < officialLoopAllDatas.Count; i++)
                    {
                        var go = GetActionItemFromPool(2);
                        go.GetComponent<ActionRoleItem>().Init(officialLoopAllDatas[i].poseInfo);
                    }
                }
                else if (curSubActionIndex == 2)
                {
                    var go = GetActionItemFromPool(1);
                    go.GetComponent<ActionGetMoreItem>().Init();
                    for (int i = 0; i < communityLoopAllDatas.Count; i++)
                    {
                        var go2 = GetActionItemFromPool(2);
                        go2.GetComponent<ActionRoleItem>().Init(communityLoopAllDatas[i].poseInfo);
                    }
                }
            }
            else if (curMainActionIndex == 1)
            {
                if (curSubActionIndex == 0)
                {
                    for (int i = 0; i < officialNonLoopAllDatas.Count; i++)
                    {
                        var go = GetActionItemFromPool(2);
                        go.GetComponent<ActionRoleItem>().Init(officialNonLoopAllDatas[i].poseInfo);
                    }
                }
                else if (curSubActionIndex == 2)
                {
                    var go = GetActionItemFromPool(1);
                    go.GetComponent<ActionGetMoreItem>().Init();
                    for (int i = 0; i < communityNonLoopAllDatas.Count; i++)
                    {
                        var go2 = GetActionItemFromPool(2);
                        go2.GetComponent<ActionRoleItem>().Init(communityNonLoopAllDatas[i].poseInfo);
                    }
                }
            }
        }

        private void OnMainActionBtnToggleSelect(int index)
        {
            curMainActionIndex = index;
            RfreshActionContent();
        }

        private void OnSubActionBtnToggleSelect(int index)
        {
            curSubActionIndex = index;
            RfreshActionContent();
        }
    }
}
