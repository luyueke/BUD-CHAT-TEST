using System;
using Game.Store;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.CommonConfirm
{
    public class BuyOcPanel : BasePanel<BuyOcPanel>
    {
        public static OcPayData ocPayData;

        [SerializeField] internal List<BuyOcItem> buyOcItems;
        [SerializeField] internal GameObject listRoot;
        [SerializeField] internal GameObject buyRoot;
        [SerializeField] internal Text usedTip;
        [SerializeField] internal Text ocNum;
        [SerializeField] internal Text ocNumTitle;
        [SerializeField] internal Text gemNum;
        [SerializeField] internal GameObject discountRoot;
        [SerializeField] internal Text discountNum;
        [SerializeField] internal Button buyButton;

        [SerializeField] internal Button closeButtonl;
        [SerializeField] internal Button closeButton2;

        [SerializeField] internal Image iconImage;
        [SerializeField] internal Sprite characterIconSprite;
        [SerializeField] internal Sprite petIconSprite;

        private Slot curSlot;
        private Action onBuySlotSuccess;
        private bool isCharacter = true;
        private SlotType _slotType = SlotType.None;


        public override void OnCreate()
        {
            base.OnCreate();
            closeButtonl.onClick.AddListener(CloseSelf);
            closeButton2.onClick.AddListener(CloseSelf);
            buyButton.onClick.AddListener(OnConfirmBuyOc);
            usedTip.SetLocalText("当前使用的卡位：{0}/{1}",3,3);
        }

        public override void OnShow(params object[] args)
        {
            if (args.Length>0)
            {
                isCharacter = (bool)args[0];
                if (args.Length > 1)
                {
                    _slotType = (SlotType)args[1];
                }
            }
            GetPay();
            if (ocPayData != null) UpdateView(ocPayData);
        }

        public void SetOnBuySuccessAct(Action act)
        {
            onBuySlotSuccess = act;
        }

        private int GetSlotType()
        {
            int slot = isCharacter ? 1 : 2;
            if (_slotType != SlotType.None)
            {
                slot = (int)_slotType;
            }

            return slot;
        }
        

        private void GetPay()
        {
            int slot = GetSlotType();
            if (_slotType != SlotType.None)
            {
                slot = (int)_slotType;
            }

            var param = new JObject()
            {
                ["slotType"] = slot
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.payOcList, HttpMethod.GET, JsonConvert.SerializeObject(param),
                onReceive: arg0 =>
                {
                    var data = JsonConvert.DeserializeObject<OcPayData>(arg0);
                    ocPayData = data;
                    UpdateView(data);
                },
                onFail: arg0 =>
                {

                }
            );
        }

        public void UpdateView(OcPayData ocPayData)
        {
            for (int i = 0, C = buyOcItems.Count; i < C; i++)
            {
                var item = buyOcItems[i];
                if (ocPayData.slotList.Count <= i)
                {
                    item.gameObject.SetActive(false);
                    continue;
                }
                var data = ocPayData.slotList[i];
                item.gameObject.SetActive(true);
                item.SetData(data, isCharacter);
                item.OnBuyOcAction = () =>
                {
                    BuyOc(data);
                };
            }
            usedTip.SetLocalText("当前使用的卡位：{0}/{1}",ocPayData.usedSlot,ocPayData.totalSlot);
            iconImage.sprite = isCharacter ? characterIconSprite : petIconSprite;
        }

        private void BuyOc(Slot slot)
        {
            curSlot = slot;
            listRoot.gameObject.SetActive(false);
            buyRoot.gameObject.SetActive(true);
            ocNum.text = $"x {slot.slotAmount}";
            ocNumTitle.SetLocalText("{0}个设子卡位",slot.slotAmount);
            gemNum.text = $"{slot.gem}";

            if (slot.discount == 0)
            {
                discountRoot.gameObject.SetActive(false);
            }
            else
            {
                discountRoot.gameObject.SetActive(true);
                discountNum.SetLocalText("{0}折",100 - slot.discount);
            }
        }

        private void OnConfirmBuyOc()
        {
            int slot = GetSlotType();
            AssetsDataManager.BuyOc(curSlot.productId, slot, CurrencyType.Gem, curSlot.gem, (success, msg, needCount) =>
            {
                if (success)
                {
                    var result = JsonConvert.DeserializeObject<BuySlotResult>(msg);
                    Message.MessageHelper.Broadcast(Message.MessageName.BuySlotResult, result.curSlotCnt);
                    CloseSelf();

                    var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                    panel?.InitData(curSlot);
                    onBuySlotSuccess?.Invoke();
                }
                else
                {
                    if (msg.Equals("余额不足"))
                    {
                        CloseSelf();
                        UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needCount);
                        return;
                    }
                }
            });
        }
    }

    public class OcPayData
    {
        public List<Slot> slotList;
        public int usedSlot;
        public int totalSlot;
    }

    public class Slot
    {
        public string productId;
        public int slotAmount;
        public int discount;
        public int gem;
    }

    public class BuySlotResult
    {
        public int curSlotCnt;
    }
}
