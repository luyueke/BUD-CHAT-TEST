using Com.TheFallenGames.OSA.Util.IO;
using Es;
using System;
using GameData.BaseInfo;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace GameUI
{
    public class PlantSeedPanelAdpterItem : MonoBehaviour
    {
        public GameObject Views;

        public Image Icon;

        public RemoteImageBehaviour RemoteImage;

        public GameObject On;

        public Button Btn;

        public CButton CreateBtn;

        [HideInInspector] public DraftListItem MapInfo;

        [HideInInspector] public GamePropData GamePropData;

        [HideInInspector] public object Info;

        Action<object> ItemSelected;

        int Idx;


        private void Awake()
        {
            Btn.onClick.AddListener(OnBtn);
            if (CreateBtn != null)
            {
                CreateBtn.onClick.AddListener(OnCreateBtn);
            }
        }

        public void SetData(object mapInfo, Action<object> action, int idx)
        {
            if (Views != null) Views.gameObject.SetActive(true);

            // 默认隐藏创建入口
            if (CreateBtn != null) CreateBtn.gameObject.SetActive(false);
            if (Btn != null) Btn.gameObject.SetActive(true);

            Info = mapInfo;

            // “创建入口”占位：只显示 CreateBtn
            if (ReferenceEquals(mapInfo, PlantSeedPanel.CreateEntryPlaceholder))
            {
                if (Icon != null) Icon.gameObject.SetActive(false);
                if (RemoteImage != null) RemoteImage.gameObject.SetActive(false);
                if (On != null) On.gameObject.SetActive(false);
                if (Btn != null) Btn.gameObject.SetActive(false);
                if (CreateBtn != null) CreateBtn.gameObject.SetActive(true);

                Idx = idx;
                ItemSelected = action;
                return;
            }

            if (mapInfo as DraftListItem != null)
            {
                Icon.gameObject.SetActive(false);
                MapInfo = mapInfo as DraftListItem;
                RemoteImage.gameObject.SetActive(false);
                RemoteImage.Load(GameStudioUtils.GetBaseInfo(MapInfo).cover, true,(from, success) => { RemoteImage.gameObject.SetActive(true); });
            }
            else if (mapInfo as GamePropData != null)
            {
                RemoteImage.gameObject.SetActive(false);
                Icon.gameObject.SetActive(true);
                GamePropData = mapInfo as GamePropData;
                var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.PgcPropSprite);
                var spriteAtlas = XAssetLoaderMgr.Inst.LoadResource<SpriteAtlas>(atlasPath, gameObject);
                Icon.sprite = spriteAtlas.GetSprite(GamePropData.IconName);
            }

            Idx = idx;

            ItemSelected = action;

            On.gameObject.SetActive(false);
        }

        private void OnBtn()
        {
            ItemSelected?.Invoke(Info);
            On.gameObject.SetActive(true);
        }

        private void OnCreateBtn()
        {
            // 跳转到“游戏素材-草稿箱”（道具草稿箱）
            UIManager.Inst.OpenPanel(PanelId.AssetStudioDraftCommonPanel, MainViewType.Prop);
        }
    }
}