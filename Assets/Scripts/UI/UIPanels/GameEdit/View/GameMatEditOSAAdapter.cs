using System;
using Basic.Utils;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Game.Base;
using Game.CommunityGame;
using GameData;
using GameData.UGCData;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.U2D;

namespace UI.UIPanels.GameEdit
{
    public class GameMatEditOSAAdapter : GridAdapter<GridParams, GameMatEditOSAItemHolder>
    {
        SimpleDataHelper<GameMatUIData> _Data;
        public SimpleDataHelper<GameMatUIData> Data {
            get{
                if (_Data == null)
                    _Data = new SimpleDataHelper<GameMatUIData>(this);
                return _Data;
            }
        }
        public Action<GameMatUIData> OnMatChange;
        SpriteAtlas matSpriteAtlas;

        protected override void Awake()
        {
            base.Awake();
            var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.MatIconSprite);
            matSpriteAtlas = XAssetLoaderMgr.Inst.LoadResource<SpriteAtlas>(atlasPath, gameObject);

            Init();
        }

        protected override void Start()
        {
            base.Start();

            Data.NotifyListChangedExternally();
        }

        protected override void OnCellViewsHolderCreated(GameMatEditOSAItemHolder cellVH, CellGroupViewsHolder<GameMatEditOSAItemHolder> cellGroup)
        {
            base.OnCellViewsHolderCreated(cellVH, cellGroup);

            cellVH.Init(matSpriteAtlas);
            cellVH.AddOnSelectListener(OnCellSelect);
        }

        protected override void UpdateCellViewsHolder(GameMatEditOSAItemHolder newOrRecycled)
        {
            GameMatUIData model = Data[newOrRecycled.ItemIndex];
            newOrRecycled.UpdateByItemIndex(model);
        }

        void OnCellSelect(int index)
        {
            OnMatChange?.Invoke(Data[index]);
        }
    }

    public class GameMatEditOSAItemHolder : CellViewsHolder
    {
        GameIconLoadItem iconLoadItem;
        SpriteAtlas matSpriteAtlas;
        Action<int> onSelectAction;

        CButton goStoreBtn;
        GameObject matUIStyle;
        GameObject goStoreStyle;
        private Transform outline;
        public override void CollectViews()
        {
            base.CollectViews();

            iconLoadItem = root.GetComponentInParent<GameIconLoadItem>(true);
            iconLoadItem.AddOnSelectListener(OnSelected);
            matUIStyle = root.Find("Views/Layout").gameObject;
            goStoreStyle = root.Find("Views/MatStore").gameObject;
            outline = root.Find("Views/Layout/Outline");
            goStoreBtn = goStoreStyle.GetComponentInChildren<CButton>(true);
            goStoreBtn.onClick.AddListener(OnGoStore);
        }

        public void Init(SpriteAtlas atlas)
        {
            matSpriteAtlas = atlas;
        }

        public void UpdateByItemIndex(GameMatUIData data)
        {
            if (!data.IsStoreGoStyle)
            {
                iconLoadItem.SetSelectWithNoNotify(data.IsSelected);

                if (GameUtils.IsNetUrl(data.Url))
                {
                    iconLoadItem.SetIcon(data.Url, (success) =>
                    {
                        if (!success)
                        {
                            outline.gameObject.SetActive(false);
                        }
                    });
                    iconLoadItem.SetIconSize(95, 95);

                } else {
                    iconLoadItem.SetIcon(matSpriteAtlas.GetSprite(data.Url));
                    iconLoadItem.SetIconSize(63, 72);
                }
            }

            if (outline != null)
            {
                var rectTransform = outline.GetComponent<RectTransform>();
                if (GameUtils.IsNetUrl(data.Url))
                {
                    rectTransform.sizeDelta = new Vector2(52, 60);
                }
                else
                {
                    rectTransform.sizeDelta = new Vector2(66, 78);
                }

                outline.gameObject.SetActive(data.MatGroupType == MatGroupTypeEnum.Anime || data.UgcStyle == (int) UgcShaderStyle.Anime);
            }
            matUIStyle.SetActive(!data.IsStoreGoStyle);
            goStoreStyle.SetActive(data.IsStoreGoStyle);
        }

        void OnSelected()
        {
            onSelectAction?.Invoke(ItemIndex);
        }

        void OnGoStore()
        {
            var storePanel = UIManager.Inst.OpenPanel<MaterialStorePanel>(PanelId.MaterialStorePanel);
            storePanel.AddRefreshMaterialList(GameUgcMatManager.Inst.ForceRefreshInteractList);
        }

        public void AddOnSelectListener(Action<int> action)
        {
            onSelectAction = action;
        }
    }
}
