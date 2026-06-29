using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.CommunityGame
{
    public class CommunityGamesItem : BaseSectionInfoItem
    {
        public HeadViewWidget HeadViewWidget;
        public CButton Btn_View;
        public SuperTextMesh Txt_MapName;
        public CText Txt_Desc;
        public int nameLimit = 15;
        public int hintLimit = 40;

        private RecommendItemData _curData;

        private void Awake()
        {
            Btn_View.onClick.AddListener(OnBtnViewClick);
        }

        public override void InitData(RecommendItemData data)
        {
            base.InitData(data);

            if(data == null)
                return;

            this._curData = data;
            Txt_MapName.text = this._curData.ugcInfo?.name;
            Txt_Desc.text = this._curData.ugcInfo?.desc;
            if (this._curData.creatorInfo != null)
            {
                HeadViewWidget.InitHeadCycle(this._curData.creatorInfo);
            }
            if (this._curData.interactInfo != null && data.ugcInfo != null)
            {
                var vistNumStr = GameUtils.ToBudCommonNumString(data.interactInfo.consumeAmount);
                var updateTime = TimestampConverter.ConvertToDateTimeString(data.ugcInfo.updateTime);
                Txt_Desc.SetLocalText("{0} 访问 · {1}", vistNumStr,  updateTime);
            }
        }

        private void OnBtnViewClick()
        {
            if (this._curData?.ugcInfo.gameType == (int)GameType.Normal)
            {
                UIManager.Inst.SwapPanel(PanelId.MapDetailPanel, this._curData?.ugcInfo?.id);

            }
            else if (this._curData?.ugcInfo.gameType== (int)GameType.AIGame)
            {
                UIManager.Inst.SwapPanel(PanelId.AIHospitalUgcMapInfoPanel, this._curData?.ugcInfo?.id);
            }
        }
    }
}
