using AIGame.Base;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using GameData.UGCData;
using GameUI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GmaeUI
{
    public class AIGameLoadingParkGroup : MonoBehaviour
    {
        public RemoteImageBehaviour RemoteImage;

        public Text NameTxt;

        public Text PlotTxt;

        public Text CreatName;

        public List<GEParkItemLabel> LabelList;

        public List<GEParkNpcHead> NpcList;

        UgcInfoRsp ResInfo;
        public void SetData(UgcInfoRsp resInfo) {
            ResInfo = resInfo;
            RefreshView();
        }

        public void RefreshView() 
        {
            gameObject.SetActive(true);

            var map = ResInfo.mapInfo;

            RemoteImage.Load(map.cover);

            NameTxt.text = map.name;

            PlotTxt.text = map.gameSetting.AICommonGameConfig.plot;

            LayoutRebuilder.ForceRebuildLayoutImmediate(PlotTxt.transform.parent as RectTransform);

            if (AIParkUtils.Inst.isOffical(map.id))
            {
                CreatName.text = "BUD官妈";
            }
            else
            {
                CreatName.text = ResInfo.creator.nickname;
            }

            var ls = map.gameSetting.AICommonGameConfig.npcData;
            for (int i = 0; i < NpcList.Count; i++)
            {
                if (i < ls.Count)
                {
                    NpcList[i].gameObject.SetActive(true);
                    NpcList[i].SetDate(ls[i].id, ls[i].cover);
                }
                else
                {
                    NpcList[i].gameObject.SetActive(false);
                }
            }

            var ls2 = map.sectionInfo;
            for (int i = 0; i < LabelList.Count; i++)
            {
                if (ls2 != null && i < ls2.Count)
                {
                    LabelList[i].Parent.gameObject.SetActive(true);
                    LabelList[i].Text.text = ls2[i].name;
                    LabelList[i].Text.GetComponent<ContentSizeFitter>().SetLayoutHorizontal();
                }
                else
                {
                    LabelList[i].Parent.gameObject.SetActive(false);
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(LabelList[0].Parent.parent as RectTransform);
        }
    }
}