using Com.TheFallenGames.OSA.DataHelpers;
using Game.Store;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI.UIPanels.CommonConfirm;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class OcList : MonoBehaviour
    {
        [SerializeField] OcAdapter OSA;
        [SerializeField] Button managerButton;

        [SerializeField] GameObject operationRoot;
        [SerializeField] Button selectAllButton;
        [SerializeField] Image selectAllToggle;
        [SerializeField] Text selectedTitle;
        [SerializeField] Button delectButton;
        [SerializeField] Button cancelButton;

        private HttpPageRequestHandle<OcListPageUseData> dataHandle;
        private List<OcServerData> ocDataList = new();
        private List<OcServerData> selectedOc = new();
        private bool operation;
        private OcServerData selected;
        private int curSet;
        private int totalSet;
        private Action<OcServerData> onItemSelected;

        public OcServerData Selected => selected;
        public bool IsInOperation => operation;

        public void SetCallback(Action<OcServerData> action)
        {
            onItemSelected = action;
        }

        private void Awake()
        {
            managerButton.onClick.AddListener(OnManagerClick);
            selectAllButton.onClick.AddListener(OnSelectAllClick);
            delectButton.onClick.AddListener(OnDelectClick);
            cancelButton.onClick.AddListener(OnCancelClick);
            // OSA.Init() 移到 Start()：Awake() 在首次 SetActive(true) 时同步调用，
            // 此时 Canvas 布局尚未刷新，RectTransform 高度为 0 会触发 OSAException。
            // Start() 在下一帧执行，Canvas 已完成布局，高度有效。
            OSA.OnItemSelected = OnItemSelected;
            OSA.OnItemDelete = OnDeleteSingleItem;
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

        private void Start()
        {
            // Canvas 布局在本帧已完成，此时初始化 OSA 高度有效
            OSA.Init();
            OSA.Data = new LazyDataHelper<OcServerData>(OSA, CreateNewModel);
            OSA.Data.ResetItems(ocDataList.Count + 1);
        }

        private bool _isCharacter;
        public void Init(bool isCharacterFittingRoom = true)
        {
            selected = null;
            _isCharacter = isCharacterFittingRoom;

            dataHandle = new HttpPageRequestHandle<OcListPageUseData>(HttpUrlDefine.ocList,paramStr:JsonConvert.SerializeObject(new JObject() { ["skinType"] = _isCharacter?0:1}), reqMethod: HttpMethod.GET);
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
            // 重置OC计数器，确保每次显示OC列表时都能正确触发引导
            // 如果不想重置OC计数器，可以将参数改为false：OcItem.ResetOcCounter(false);
            OcItem.ResetOcCounter();
        }

        private void OnDisable()
        {
            selected = null;
            CloseOperation();
        }

        public void OnDeleteSingleItem(OcServerData data)
        {
            CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            commonConfirmPanel.SetLocalText($"删除{(_isCharacter ? "设子" : "宠物")}", $"你确定删除选中的{(_isCharacter ? "设子" : "宠物")}吗？", "删除", "取消");
            commonConfirmPanel.SetOnClickAction(() =>
            {
                List<OcServerData> tempOcList = new();
                tempOcList.Add(data);
                DeleteOcInfo(tempOcList.ToList(), (success) =>
                {
                    if (success)
                    {
                        TipPanel.ShowToast("删除成功");
                        dataHandle?.Reset();
                        dataHandle?.Start();
                        MessageHelper.Broadcast(MessageName.RefreshOcList);
                    }
                });

                CloseOperation();
            }, () =>
            {

            });
        }

        public void OnItemSelected(OcServerData data, bool isOn)
        {
            if (data == null)
                return;

            if (data.add)
            {
                var panel = UIManager.Inst.OpenPanel<BuyOcPanel>(PanelId.BuyOcPanel,_isCharacter);
                panel.SetOnBuySuccessAct(() =>
                {
                    dataHandle?.Reset();
                    dataHandle?.Start();
                    MessageHelper.Broadcast(MessageName.RefreshOcList);
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
                
                if (_isCharacter)
                {
                    selectedTitle.SetLocalText("选择了{0}个设子",selectedOc.Count);
                }
                else
                {
                    selectedTitle.SetLocalText("选择了{0}个宠物",selectedOc.Count);
                }

                OSA.Data.ResetItems(ocDataList.Count + 1);
                selectAllToggle.gameObject.SetActive(selectedOc.Count == ocDataList.Count);
                return;
            }

            selected = data;
            OSA.Data.ResetItems(ocDataList.Count + 1);
            onItemSelected?.Invoke(selected);
        }

        private void OnPullData(OcListPageUseData data)
        {
            OSA.PullToRefreshBehaviour.HideGizmo();
            if (data.isFirst)
            {
                ocDataList.Clear();
                curSet = data.totalCount;
                totalSet = data.totalSlotCount;
            }

            if (data.list != null) {
                for (int i = 0; i < data.list.Count; i++)
                {
                    data.list[i].skinType = _isCharacter ? 0 : 1;
                    if (i<data.limitedSlotCount)
                    {
                        data.list[i].leftTime = data.leftTime;
                    }
                }
            }
            MarketReviewManager.Inst.CheckSaveOcListIsShowMarketPointPanel(data.totalCount);
            ocDataList.AddRange(data.list);
            OSA.Data?.ResetItems(ocDataList.Count + 1);
        }

        private OcServerData CreateNewModel(int index)
        {
            if (index == 0)
            {
                return new OcServerData()
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
                        var selectedData = selectedOc.Find(x => x.ocInfo?.ocId == ocData.ocInfo?.ocId);
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
            selectAllButton.gameObject.SetActive(false);
            operationRoot.gameObject.SetActive(false);
            selectedTitle.SetLocalText($"已保存的{(_isCharacter ? "设子" : "宠物")}");
            if (ocDataList != null && OSA != null && OSA.Data != null) OSA.Data.ResetItems(ocDataList.Count + 1);
        }

        private void OnManagerClick()
        {
            operation = true;
            managerButton.gameObject.SetActive(false);
            // 屏蔽全选
            //selectAllButton.gameObject.SetActive(true);
            operationRoot.gameObject.SetActive(true);
            if (_isCharacter)
            {
                selectedTitle.SetLocalText("选择了{0}个设子",selectedOc.Count);
            }
            else
            {
                selectedTitle.SetLocalText("选择了{0}个宠物",selectedOc.Count);
            }

            OSA.Data.ResetItems(ocDataList.Count + 1);
        }

        private void OnSelectAllClick()
        {
            if (selectedOc.Count == ocDataList.Count)
            {
                selectedOc.Clear();
                OSA.Data.ResetItems(ocDataList.Count + 1);
                selectAllToggle.gameObject.SetActive(false);
            }
            else
            {
                selectedOc.Clear();
                ocDataList.ForEach(o => selectedOc.Add(o));
                if (_isCharacter)
                {
                    selectedTitle.SetLocalText("选择了{0}个设子",ocDataList.Count);
                }
                else
                {
                    selectedTitle.SetLocalText("选择了{0}个宠物",ocDataList.Count);
                }

                OSA.Data.ResetItems(ocDataList.Count + 1);
                selectAllToggle.gameObject.SetActive(true);
            }
        }

        private void OnDelectClick()
        {
            if (selectedOc.Count == 0)
            {
                CloseOperation();
                return;
            }

            CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            commonConfirmPanel.SetLocalText($"删除{(_isCharacter ? "设子" : "宠物")}", $"你确定删除选中的{(_isCharacter ? "设子" : "宠物")}吗？", "删除", "取消");
            commonConfirmPanel.SetOnClickAction(() =>
            {
                DeleteOcInfo(selectedOc.ToList(), (success) =>
                {
                    if (success)
                    {
                        TipPanel.ShowToast("删除成功");
                        dataHandle?.Reset();
                        dataHandle?.Start();
                        MessageHelper.Broadcast(MessageName.RefreshOcList);
                    }
                });

                CloseOperation();
            }, () =>
            {

            });
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

        public void SetOcInfo(string ocCover, string avatarJson, Action<bool> resultHandler = null, bool refreshList = true)
        {
            OcInfo info = new OcInfo()
            {
                ocCover = ocCover,
                avatarJson = avatarJson,
                skinType = _isCharacter ? 0 : 1,
            };

            OcSetReq req = new OcSetReq
            {
                setType = 0,
                ocInfo = info
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setOc, HttpMethod.POST, JsonConvert.SerializeObject(req),
                onReceive: arg0 =>
                {
                    if (refreshList)
                    {
                        dataHandle?.Reset();
                        dataHandle?.Start();
                        MessageHelper.Broadcast(MessageName.RefreshOcList);
                    }
                    resultHandler?.Invoke(true);
                },
                onFail: arg0 =>
                {
                    resultHandler?.Invoke(false);
                }
            );
        }

        public void DeleteOcInfo(List<OcServerData> ocServerDatas, Action<bool> resultHandler = null)
        {
            List<string> ocIds = new();
            foreach (var data in ocServerDatas)
            {
                ocIds.Add(data.ocInfo.ocId);
            }

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.batchDelOc, HttpMethod.POST, JsonConvert.SerializeObject(new DeleteOc() { ocIds = ocIds }),
                onReceive: arg0 =>
                {
                    // 删除选中的社子，把选中信息清空
                    if (selected != null && selected.ocInfo != null && ocIds.Contains(selected.ocInfo.ocId)) selected = null;
                    resultHandler?.Invoke(true);
                },
                onFail: arg0 =>
                {
                    resultHandler?.Invoke(false);
                }
            );
        }

        public void RefreshList()
        {
            dataHandle?.Reset();
            dataHandle?.Start();
        }
    }

    public class DeleteOc
    {
        public List<string> ocIds;
    }

    public class OcListPageUseData : HttpPageUseData
    {
        public List<OcServerData> list;
        public int totalCount;
        public int totalSlotCount;
        public int limitedSlotCount;
        public string leftTime;
    }

    public class OcServerData
    {
        public bool add;
        public int cur;
        public int total;
        public bool operation;
        public bool selected;
        public OcInfo ocInfo;
        public int isSlot; //1表示空槽位，没有设子


        /// <summary>
        /// 服务器不返回，由客户端给予请求类型 赋值
        /// </summary>
        public int skinType; // 1 表示宠物， 0 表示角色
        public string leftTime;
    }

    public class OcSetReq
    {
        public OcInfo ocInfo;

        // 0： 设置； 1: 删除
        public int setType;
    }
}
