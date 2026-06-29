using Com.TheFallenGames.OSA.Util.IO;
using Es;
using GameData.BaseInfo;
using System;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace GameUI
{
    public class PlantInfoPanelAdpterItem : MonoBehaviour
    {
        public GameObject Views;

        public RemoteImageBehaviour Head;
        public Text Name;
        public Text IdName;
        public Image Frame;
        public Transform Trans_BottomEffect;
        public Transform Trans_TopEffect;

        public Button Btn;

        public GameObject Gray;

        public Text FlowerCount;

        [HideInInspector] public PlantUserInfo MapInfo;

        Action<PlantUserInfo> ItemSelected;

        int Idx;
        [SerializeField] private Button headButton;
        private void Awake()
        {
            Btn.onClick.AddListener(OnBtn);

            headButton.onClick.AddListener(() =>
            {
                if (MapInfo == null)
                {
                    return;
                }

                string uid = MapInfo.userInfo.uid;
                if (string.IsNullOrEmpty(uid))
                {
                    return;
                }

                UIManager.Inst.OpenPanel<ProfilePanel>(PanelId.ProfilePanel, uid);
            });

        }

        public void SetData(PlantUserInfo mapInfo, Action<PlantUserInfo> action, int idx)
        {
            if (Views != null) Views?.gameObject.SetActive(true);

            if (mapInfo == null)
            {
                return;
            }

            MapInfo = mapInfo;

            Idx = idx;

            ItemSelected = action;

            FlowerCount.text = MapInfo.treePlantingDayInfo.growthValue.ToString();

            Gray.gameObject.SetActive(MapInfo.treePlantingDayInfo.isVoted);

            Btn.gameObject.SetActive(!MapInfo.treePlantingDayInfo.isVoted);

            Head.Load(mapInfo.userInfo.portraitUrl);
            Name.text = mapInfo.userInfo.nickname;
            IdName.text = "ID: "+ mapInfo.userInfo.username;
            Frame.gameObject.SetActive(false);
            Trans_BottomEffect.ClearChildren();
            Trans_TopEffect.ClearChildren();
            var headCycleData = UserUIWidgetManager.Inst.GetHeadCycleData(mapInfo.userInfo.avatarFrame, this.gameObject);
            if (headCycleData != null)
            {
                Frame.gameObject.SetActive(true);
                Frame.sprite = headCycleData.Sp_HeadCycle;

                if (headCycleData.Effect_Bottom != null)
                {
                    headCycleData.Effect_Bottom.Instantiate(Trans_BottomEffect);
                }

                if (headCycleData.Effect_Top != null)
                {
                    headCycleData.Effect_Top.Instantiate(Trans_TopEffect);
                }
            }
        }

        private void OnBtn()
        {
            PlantTreeSystem.Inst.RequestWater(MapInfo.userInfo.uid, 1, 0, () => {
                FlowerCount.text = (MapInfo.treePlantingDayInfo.growthValue + 10).ToString();

                MapInfo.treePlantingDayInfo.isVoted = true;

                Gray.gameObject.SetActive(MapInfo.treePlantingDayInfo.isVoted);

                Btn.gameObject.SetActive(!MapInfo.treePlantingDayInfo.isVoted);
            });
        }
    }
}