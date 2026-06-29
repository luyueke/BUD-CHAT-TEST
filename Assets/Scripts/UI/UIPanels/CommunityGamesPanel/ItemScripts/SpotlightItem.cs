using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.Base;
using UI.BaseWidgets;
using UnityEngine;


namespace Game.CommunityGame
{
    public class SpotlightItem : MonoBehaviour
    {
        public CButton Btn_Cover;
        public RemoteImageBehaviour Rm_Cover;
        public SuperTextMesh Txt_Title;
        
        private RecommendItemData cur_Data;

        private void BindUI()
        {

        }

        public void SetData(RecommendItemData data)
        {
            this.cur_Data = data;
            
            Btn_Cover.onClick.AddListener(OnBtnViewClick);
            Rm_Cover.Load(this.cur_Data.ugcInfo?.cover);
            Txt_Title.text = this.cur_Data.ugcInfo?.name;
        }

        private void OnBtnViewClick()
        {
            UIManager.Inst.SwapPanel(PanelId.MapDetailPanel, cur_Data.ugcInfo.id);
        }
    }
}
