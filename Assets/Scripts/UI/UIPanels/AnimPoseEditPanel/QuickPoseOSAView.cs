using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Com.TheFallenGames.OSA.DataHelpers;
using Game.Store;
using GameData.BaseInfo;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.UIPanels.CommonConfirm;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    public class QuickPoseOSAView : BasePoseOSAView
    {
        [SerializeField] QuickPoseAdapter OSA;
        [SerializeField] Button managerButton;
        [SerializeField] Button downButton;
        [SerializeField] GameObject operationRoot;
        [SerializeField] Button delectButton;
        [SerializeField] Button cancelButton;

        private HttpPageRequestHandle<PoseOcListPageUseData> dataHandle;
        private List<PoseOcServerData> ocDataList = new();
        private List<PoseOcServerData> selectedOc = new();
        private bool operation;
        private PoseOcServerData selected;
        private int curSet;
        private int totalSet;
        private Action<PoseOcServerData> onItemSelected;

        public PoseOcServerData Selected => selected;
        public Action<int,int> OnRefreshSlot;
        private UgcPoseSubType curPoseType;
        public override void OnStart(UgcPoseSubType subType)
        {
            curPoseType = subType;
            managerButton.onClick.AddListener(OnManagerClick);
            delectButton.onClick.AddListener(OnDelectClick);
            cancelButton.onClick.AddListener(OnCancelClick);
            OSA.Init();
            OSA.Data = new LazyDataHelper<PoseOcServerData>(OSA, CreateNewModel);
            OSA.OnItemSelected = OnItemSelected;
            OSA.OnItemDelete = OnDeleteSingleItem;
            Init(subType);
        }
        
        private void Start()
        {
            OSA.Data.ResetItems(ocDataList.Count + 1);
        }

        public void SetCallback(Action<PoseOcServerData> action)
        {
            onItemSelected = action;
        }

        private void AddListener()
        {
            Message.MessageHelper.AddListener<int>(Message.MessageName.BuySlotResult, OnBuySlotResult);
        }

        public void RemoveListener()
        {
            Message.MessageHelper.RemoveListener<int>(Message.MessageName.BuySlotResult, OnBuySlotResult);
        }

        private void OnBuySlotResult(int num)
        {
            totalSet = num;

            if (this == null || object.ReferenceEquals(this, null))
            {
                return;
            }

            if (gameObject.activeInHierarchy)
            {
                if (ocDataList != null)
                    OSA.Data.ResetItems(ocDataList.Count + 1);
            }
        }

      
        private bool _isCharacter;

        public void Init(UgcPoseSubType subType)
        {
            selected = null;
            if (subType == UgcPoseSubType.Single || subType == UgcPoseSubType.Double)
            {
                _isCharacter = true;
            }
            else
            {
                _isCharacter = false;
            }
            dataHandle = new HttpPageRequestHandle<PoseOcListPageUseData>(HttpUrlDefine.quickPoseList,
                paramStr: JsonConvert.SerializeObject(new JObject() {["skinType"] = _isCharacter ? 0 : 1,["poseType"] = (int)subType}),
                reqMethod: HttpMethod.GET);
            OSA.PullToRefreshBehaviour.OnRefreshWithSlideUp.RemoveAllListeners();
            OSA.PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(dataHandle.Next);
            dataHandle.AddSuccessAction(OnPullData);
            dataHandle.AddFailAction((_) => { OSA.PullToRefreshBehaviour.HideGizmo(); });
            dataHandle.Reset();
            dataHandle.Start();

            AddListener();
        }

        private void OnEnable()
        {
            selected = null;
            CloseOperation();
        }

        private void OnDisable()
        {
            selected = null;
            CloseOperation();
        }

        public void OnDeleteSingleItem(PoseOcServerData data)
        {
            CommonConfirmPanel commonConfirmPanel =
                UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            commonConfirmPanel.SetText($"删除快捷姿势", $"你确定删除选中的快捷姿势吗？",
                "删除", "取消");
            commonConfirmPanel.SetOnClickAction(() =>
            {
                List<PoseOcServerData> tempOcList = new();
                tempOcList.Add(data);
                DeleteOcInfo(tempOcList.ToList(), (success) =>
                {
                    if (success)
                    {
                        TipPanel.ShowToast("删除成功");
                        dataHandle?.Reset();
                        dataHandle?.Start();
                    }
                });

                CloseOperation();
            }, () => { });
        }
        
        

        public void OnItemSelected(PoseOcServerData data, bool isOn)
        {
            if (data == null)
                return;

            if (data.add)
            {
                var panel = UIManager.Inst.OpenPanel<BuyQuickPosePanel>(PanelId.BuyQuickPosePanel, curPoseType);
                panel.SetOnBuySuccessAct(() =>
                {
                    dataHandle.Reset();
                    dataHandle.Start();
                });
                // 购买卡槽
                return;
            }

            if (operation)
            {
                data.operation = true;
                data.selected = isOn;
                if (data.selected == false)
                    selectedOc.Remove(data);
                else
                    selectedOc.Add(data);

                OSA.Data.ResetItems(ocDataList.Count + 1);
                return;
            }
            OnSelectPoseClick?.Invoke(data.poseInfo.poseData);
            selected = data;
            OSA.Data.ResetItems(ocDataList.Count + 1);
            onItemSelected?.Invoke(selected);
        }

        private void OnPullData(PoseOcListPageUseData data)
        {
            OSA.PullToRefreshBehaviour.HideGizmo();
            if (data.isFirst)
            {
                ocDataList.Clear();
                curSet = data.totalCount;
                totalSet = data.totalSlotCount;
                OnRefreshSlot?.Invoke(curSet,totalSet);
            }

            if (data.list != null)
            {
                foreach (var tmpOcData in data.list)
                {
                    tmpOcData.skinType = _isCharacter ? 0 : 1;
                }
                ocDataList.AddRange(data.list);
                ocDataList.ForEach(x=>x.poseType = (int)curPoseType);
            }

            OSA.Data?.ResetItems(ocDataList.Count + 1);
        }

        private PoseOcServerData CreateNewModel(int index)
        {
            if (index == 0)
            {
                return new PoseOcServerData()
                {
                    add = true,
                    cur = curSet,
                    total = totalSet,
                    skinType = _isCharacter ? 0 : 1,
                };
            }
            else
            {
                var ocData = ocDataList[index - 1];

                //占位空槽
                if (ocData != null)
                {
                    ocData.operation = operation;
                    if (operation)
                    {
                        var selectedData = selectedOc.Find(x => x.poseInfo?.id == ocData.poseInfo?.id);
                        ocData.selected = selectedData != null;
                    }
                    else
                    {
                        ocData.selected = ocData == selected;
                    }
                }

                return ocData;
            }
        }

        private void CloseOperation()
        {
            operation = false;
            selectedOc.Clear();
            managerButton.gameObject.SetActive(true);
            operationRoot.gameObject.SetActive(false);
            downButton.gameObject.SetActive(true);
            if (OSA.Data != null && ocDataList != null && ocDataList.Count != 0)
            {
                OSA.Data.ResetItems(ocDataList.Count + 1);
            }
        }

        private void OnManagerClick()
        {
            operation = true;
            managerButton.gameObject.SetActive(false);
            operationRoot.gameObject.SetActive(true);
            downButton.gameObject.SetActive(false);
            OSA.Data.ResetItems(ocDataList.Count + 1);
        }

        private void OnDelectClick()
        {
            if (selectedOc.Count == 0)
            {
                CloseOperation();
                return;
            }

            CommonConfirmPanel commonConfirmPanel =
                UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            commonConfirmPanel.SetText($"删除快捷姿势", $"你确定删除选中的快捷姿势吗？",
                "删除", "取消");
            commonConfirmPanel.SetOnClickAction(() =>
            {
                DeleteOcInfo(selectedOc.ToList(), (success) =>
                {
                    if (success)
                    {
                        TipPanel.ShowToast("删除成功");
                        dataHandle?.Reset();
                        dataHandle?.Start();
                    }
                });

                CloseOperation();
            }, () => { });
        }

        private void OnCancelClick()
        {
            operation = false;
            CloseOperation();
        }

        public bool CanSave()
        {
            if (totalSet == 0)
            {
                dataHandle.Reset();
                dataHandle.Start();
                return true;
            }

            return curSet < totalSet;
        }

        // public void SetOcInfo(string ocCover, string avatarJson, Action<bool> resultHandler = null)
        // {
        //     OcInfo info = new OcInfo()
        //     {
        //         ocCover = ocCover,
        //         avatarJson = avatarJson,
        //         skinType = _isCharacter ? 0 : 1,
        //     };
        //
        //     OcSetReq req = new OcSetReq
        //     {
        //         setType = 0,
        //         ocInfo = info
        //     };
        //
        //     NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setOc, HttpMethod.POST, JsonConvert.SerializeObject(req),
        //         onReceive: arg0 =>
        //         {
        //             dataHandle?.Reset();
        //             dataHandle?.Start();
        //             resultHandler?.Invoke(true);
        //         },
        //         onFail: arg0 => { resultHandler?.Invoke(false); }
        //     );
        // }

        public void DeleteOcInfo(List<PoseOcServerData> ocServerDatas, Action<bool> resultHandler = null)
        {
            List<string> ids = new();
            foreach (var data in ocServerDatas)
            {
                ids.Add(data.poseInfo.id);
            }

            if (ids.Count == 0)
            {
                OnCancelClick();
                return;
            }

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.delQuickPose, HttpMethod.POST,
                JsonConvert.SerializeObject(new DeletePoseIDs() {ids = ids}),
                onReceive: arg0 =>
                {
                    // 删除选中的社子，把选中信息清空
                    if (selected != null && selected.poseInfo != null && ids.Contains(selected.poseInfo.id))
                        selected = null;
                    resultHandler?.Invoke(true);
                },
                onFail: arg0 => { resultHandler?.Invoke(false); }
            );
        }

        public void RefreshList()
        {
            dataHandle?.Reset();
            dataHandle?.Start();
        }
    }

    public class DeletePoseIDs
    {
        public List<string> ids;
    }

    public class PoseOcListPageUseData : HttpPageUseData
    {
        public List<PoseOcServerData> list;
        public int totalCount;
        public int totalSlotCount;
    }

    public class PoseOcServerData
    {
        public bool add;
        public int cur;
        public int total;
        public bool operation;
        public bool selected;
        public PoseInfo poseInfo;
        public int isSlot; //1表示空槽位，没有设子
        public int poseType;

        /// <summary>
        /// 服务器不返回，由客户端给予请求类型 赋值
        /// </summary>
        public int skinType; // 1 表示宠物， 0 表示角色
    }

    public class OcSetReq
    {
        public OcInfo ocInfo;

        // 0： 设置； 1: 删除
        public int setType;
    }
}
