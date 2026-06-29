using System;
using System.Collections.Generic;
using Game.AINPCStudio;
using Game.Audio;
using Game.Base;
using Game.Config;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers;
using Game.PropStore;
using Game.Utils;
using GameData;
using Message;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UndoSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc:
/// Date:24-06-21 21:13:33
/// </summary>
namespace Game.AssetToolBox
{
    public class PublishAssetBoxPanel : MonoBehaviour
    {
        private CButton Btn_ShowToolBox;
        private CButton Btn_CloseToolBox;

        private PublishAssetOSAView BagOsaView;
        private GameObject Go_ToolBox;

        private int propCurrentIndex = 0;


        public void Init()
        {

            Btn_ShowToolBox = GameObjectEx.FindChildByName(this.transform, "Btn_ShowToolBox").GetComponent<CButton>();
            Btn_CloseToolBox = GameObjectEx.FindChildByName(this.transform, "Btn_CloseToolBoxPanel").GetComponent<CButton>();
            Go_ToolBox = GameObjectEx.FindChildByName(this.transform, "ToolBoxPanel").gameObject;
     
            BagOsaView = GameObjectEx.FindChildByName(this.transform, "BagOsaView").GetComponent<PublishAssetOSAView>();

            BagOsaView.SetBottomBtnAct(() =>
            {
              //  Debug.LogError($"init data.count={BagOsaView.AllDatas.Count},adapte={BagOsaView.Adapter.Data.Count}");
                UIManager.Inst.OpenPanel<InventoryPublishBagPanel>(PanelId.InventoryPublishBagPanel,BagOsaView.Adapter.Data.List);
            });

            BagOsaView.SetOSAItemClickAct(OnOwnedItemClick);

            Btn_ShowToolBox.onClick.AddListener(() => { ShowToolBox(true); });
            Btn_CloseToolBox.onClick.AddListener(() => { ShowToolBox(false); });
        }

        public void Reset()
        {
            ShowToolBox(false);
           
        }
        private void ShowToolBox(bool show)
        {
            Btn_ShowToolBox.gameObject.SetActive(!show);
            Go_ToolBox.gameObject.SetActive(show);
        }

        private void OnOwnedItemClick(PropResInfo itemData)
        {
            if (CanUseVip())
            {
                CreateUgcAsset(itemData);
            }
        }

        private void CreateUgcAsset(PropResInfo itemData)
        {
            if (itemData != null && itemData.propInfo != null)
            {
                if(UIManager.Inst.TryFindPanel( WindowId.UGCItemEditWindow, PanelId.UGCVehicleEditPanel, out _))
                {
                    int v = GameProfilerManager.Inst.mapStatisticInfo.vertices;
                    int p_v = itemData.propInfo.detailInfo.vertexs;
                    if(v + p_v > GameProfilerManager.GetLimitVerticesCount(LimitType.Prop))
                    {
                        TipPanel.ShowToast($"顶点数超过限制, 总计顶点数不能超过{GameProfilerManager.GetLimitVerticesCount(LimitType.Prop)}");
                        return;
                    }
                }

                GlobalNodeManager.Inst.Get<PropManager>().Create(itemData.propInfo, behaviour =>
                {
                    if (behaviour != null)
                    {
                        UI.Manager.InputHandlerManager.Inst.SelectEntity(behaviour.entity);
                    }
                });
            }
            else
            {
                LoggerUtils.LogError("itemData 类型不对或为null:");
            }
        }

        public bool CanUseVip()
        {
            if (VipDataManager.Inst.isVip)
                return true;

            var joinVipType = new List<JoinVipType>() { JoinVipType.VIP_Multi_Prop };
            var joinVipTitle = "您正在使用的VIP功能：" + "添加已发布的素材";
            if (joinVipType.Count > 0)
            {
                UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, joinVipTitle, joinVipType);
            }

            return false;
        }






        private void OnBuyUgcItemSuccess(string ugcId)
        {
            if(BagOsaView.IsInit)
                BagOsaView.GetFirstPageDatas();
            

        }
    }
}
