using System.Collections;
using System.Collections.Generic;
using Basic.Extensions;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    public class StatisticsCard : BaseCard
    {
        [SerializeField] private StatisticsItem PlayItem;
        [SerializeField] private StatisticsItem HasPropItem;
        [SerializeField] private StatisticsItem HasSkinItem;
        [SerializeField] private StatisticsItem PostItem;
        [SerializeField] private StatisticsItem PublishMapItem;
        [SerializeField] private StatisticsItem PublishPropItem;
        [SerializeField] private StatisticsItem PublishSkinItem;
        [SerializeField] private StatisticsItem PublishMaterialItem;
        [SerializeField] private StatisticsItem PublishMusicScoreItem;
        [SerializeField] private StatisticsItem PublishToneItem;
        [SerializeField] private StatisticsItem PublishAnimAndPose;
        [SerializeField] private StatisticsItem PublishPetAnimAndPose;
        [SerializeField] private StatisticsItem PublishNPC;
        [SerializeField] private StatisticsItem PublishVehicle;
        [SerializeField] private StatisticsItem PublishTheatre;
        [SerializeField] private StatisticsItem PublishActor;
        [SerializeField] private Text RegisterText;

        public override void OnCreate(ProfilePanel profilePanel)
        {
            base.OnCreate(profilePanel);
            cardBgType = ProfileCardBgType.Bg4;
        }

        public override void OnShow(string uid)
        {
            base.OnShow(uid);
        }


        public void SetData(StatisticsData statisticsData)
        {
            if (statisticsData == null) return;
            PublishMapItem.SetNum(statisticsData.pubMapCnt);
            PublishPropItem.SetNum(statisticsData.pubPropCnt);
            PublishSkinItem.SetNum(statisticsData.pubSkinCnt);
            PlayItem.SetNum(statisticsData.experienceMapCnt);
            HasPropItem.SetNum(statisticsData.ownPropCnt);
            HasSkinItem.SetNum(statisticsData.ownSkin);
            PostItem.SetNum(statisticsData.pubPostCnt);
            PublishMaterialItem.SetNum(statisticsData.pubMaterialCnt);
            PublishMusicScoreItem.SetNum(statisticsData.pubMusicScoreCnt);
            PublishToneItem.SetNum(statisticsData.pubMusicToneCnt);
            PublishAnimAndPose.SetNum(statisticsData.pubAnimationCnt);
            PublishPetAnimAndPose.SetNum(statisticsData.pubPoseCnt);
            PublishNPC.SetNum(statisticsData.pubNpcCnt);
            PublishVehicle.SetNum(statisticsData.pubVehicleCnt);
            PublishTheatre.SetNum(statisticsData.pubTheaterCnt);
            PublishActor.SetNum(statisticsData.pubActorCnt);
        }

        public override void OnUpdateTheme(ProfileThemeInfo themeInfo)
        {
            base.OnUpdateTheme(themeInfo);
            if(themeInfo == null && themeInfo.colorInfo == null) return;
            ProfileThemeManager.Inst.ChangeTextColor(transform,themeInfo.colorInfo.textColor);
            //if (bg.childCount > 0)
            //{
            //    Destroy(bg.GetChild(0).gameObject);
            //}
            //if ( !string.IsNullOrEmpty(themeInfo.colorInfo.gamebg))
            //{
            //    var obj = Loader.Load<GameObject>(themeInfo.colorInfo.gamebg).Instantiate(bg);
            //    obj.transform.SetSiblingIndex(1);
            //}
        }


        public void SetTipsText(long stamp)
        {
            string timeStr = DataUtil.GetTimeStrByStampFormat(stamp * 1000, "yyyy/MM/dd");
            RegisterText.SetLocalText("{0}加入BUD", timeStr);
        }
    }
}
