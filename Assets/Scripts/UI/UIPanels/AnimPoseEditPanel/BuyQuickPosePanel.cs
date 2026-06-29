using System;
using Game.Store;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using GameData.PgcData;
using UI.Base;
using UI.UIPanels.CommonConfirm;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    public class BuyQuickPosePanel : BasePanel<BuyQuickPosePanel>
    {
        public static OcPayData ocPayData;

        [SerializeField] internal List<BuyQuickPoseItem> buyOcItems;
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
        [SerializeField] internal Sprite[] iconSprites;

        private Slot curSlot;
        private Action onBuySlotSuccess;
        private UgcPoseSubType curPoseType;
        
        private Dictionary<UgcPoseSubType, string> allPoseTips = new()
        {
            {UgcPoseSubType.Single,"单人"},
            {UgcPoseSubType.Double,"双人"},
            {UgcPoseSubType.PetSingle,"宠物单人"},
            {UgcPoseSubType.PetWithPlayer,"宠物双人"}
        };

        public override void OnCreate()
        {
            base.OnCreate();
            closeButtonl.onClick.AddListener(CloseSelf);
            closeButton2.onClick.AddListener(CloseSelf);
            buyButton.onClick.AddListener(OnConfirmBuyOc);
        }

        public override void OnShow(params object[] args)
        {
            curPoseType = (UgcPoseSubType)args[0];
            GetPay();
            if (ocPayData != null) UpdateView(ocPayData);
        }

        public void SetOnBuySuccessAct(Action act)
        {
            onBuySlotSuccess = act;
        }

        private void GetPay()
        {
            int slotType = GetSlotType();
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.payOcList, HttpMethod.GET, JsonConvert.SerializeObject(new JObject() { ["slotType"] = slotType }),
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

        private int GetSlotType()
        {
            int slotType = 3;
            switch (curPoseType)
            {
                case UgcPoseSubType.Single:
                    slotType = 3;
                    break;
                case UgcPoseSubType.Double:
                    slotType = 4;
                    break;
                case UgcPoseSubType.PetSingle:
                    slotType = 5;
                    break;
                case UgcPoseSubType.PetWithPlayer:
                    slotType = 6;
                    break;
            }

            return slotType;
        }

        public void UpdateView(OcPayData ocPayData)
        {
            usedTip.SetLocalText("当前使用的卡位：{0}/{1}", ocPayData.usedSlot, ocPayData.totalSlot);
            switch (curPoseType)
            {
                case UgcPoseSubType.Single:
                    iconImage.sprite = iconSprites[0];
                    break;
                case UgcPoseSubType.Double:
                    iconImage.sprite = iconSprites[1];
                    break;
                case UgcPoseSubType.PetSingle:
                    iconImage.sprite = iconSprites[2];
                    break;
                case UgcPoseSubType.PetWithPlayer:
                    iconImage.sprite = iconSprites[3];
                    break;
            }
            iconImage.SetNativeSize();
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
                string tip = allPoseTips[curPoseType];
                item.SetData(data, tip, iconImage.sprite);
                item.OnBuyOcAction = () =>
                {
                    BuyOc(data);
                };
            }

            
        }

        private void BuyOc(Slot slot)
        {
            curSlot = slot;
            listRoot.gameObject.SetActive(false);
            buyRoot.gameObject.SetActive(true);
            ocNum.text = $"x {slot.slotAmount}";
            string tip = allPoseTips[curPoseType];
            ocNumTitle.SetLocalText("{0}个{1}姿势卡位", slot.slotAmount, tip);
            gemNum.text = $"{slot.gem}";

            if (slot.discount == 0)
            {
                discountRoot.gameObject.SetActive(false);
            }
            else
            {
                discountRoot.gameObject.SetActive(true);
                discountNum.text = $"{100 - slot.discount}折";
            }
        }

        private void OnConfirmBuyOc()
        {
            int slotType = GetSlotType();
            AssetsDataManager.BuyOc(curSlot.productId, slotType, CurrencyType.Gem, curSlot.gem, (success, msg, needCount) =>
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
}
