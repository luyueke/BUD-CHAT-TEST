using System;
using System.Collections.Generic;
using GameData.PgcData;
using UnityEngine;

namespace BUD.AnimPose
{
    public class BasePoseOSAView: MonoBehaviour
    {
        public Action<string> OnSelectPoseClick;

        public string CurSelectId;
        public virtual void OnStart(UgcPoseSubType subType)
        {
            
        }

        public virtual void OnUpdate()
        {
            
        }

        public bool CanUseVip(string vipStr)
        {
            if (VipDataManager.Inst.isVip)
                return true;

            var joinVipType = new List<JoinVipType>() { JoinVipType.VIP_Multi_Prop };
            var joinVipTitle = "您正在使用的VIP功能：" + vipStr;
            if (joinVipType.Count > 0)
            {
                UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, joinVipTitle, joinVipType);
            }

            return false;
        }
    }
}