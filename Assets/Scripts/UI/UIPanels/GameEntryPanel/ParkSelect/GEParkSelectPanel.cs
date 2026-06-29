using AIGame.Base;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Avatar;
using GameData.Base;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class GEParkSelectPanel : BasePanel<GEParkSelectPanel>
    {
        public CButton CloseBtn;

        public Toggle UgcTog;
        public Toggle PgcTog;
        public CButton SelectBtn;

        public GEParkSelectItem PgcInfo;

        public GameObject UgcInfo;
        public Toggle ToggleCollect;
        public Toggle ToggleRecord;
        public Toggle ToggleSelected;
        public CButton MoreBtn;
        public GEParkSelectAdpter SelectAdpter;
        public PullToRefreshBehaviour PullToRefreshBehaviour;
        public Text NoneText;

        public GameObject ModelRoot;
        public RawImage RawImage;
        public AvatarCameraController AvatarCameraController;

        [HideInInspector] private UgcBaseInfo MapInfo;

        [HideInInspector] private Dictionary<string, CharacterWrap> WrapDic = new Dictionary<string, CharacterWrap>();
        private CharacterWrap SelfWrap;

        private RenderTexture RenderTexture;

        GameEntrySystemData data => GameEntrySystem.Inst.data;
        public override void OnCreate()
        {
            base.OnCreate();

            PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(OnPullRefresh);
            SelectAdpter.OnItemSelected = OnItemSelected;
            SelectAdpter.Data = new LazyDataHelper<UgcBaseInfo>(SelectAdpter, GetMapInfo);
            SelectAdpter.Init();

            CloseBtn.onClick.AddListener(OnClose);
            UgcTog.onValueChanged.AddListener(OnUgcTog);
            PgcTog.onValueChanged.AddListener(OnPgcTog);
            SelectBtn.onClick.AddListener(OnSelectBtn);

            MoreBtn.onClick.AddListener(OnMoreBtn);
            ToggleCollect.onValueChanged.AddListener(OnToggleCollect);
            ToggleRecord.onValueChanged.AddListener(OnToggleRecord);
            ToggleSelected.onValueChanged.AddListener(OnToggleSelected);

            RenderTexture = new RenderTexture(2000, 800, 24);
            RawImage.texture = RenderTexture;
            AvatarCameraController.roleCamera.targetTexture = RenderTexture;
        }

        protected override void OnDestroy()
        {
            if (RenderTexture != null)
            {
                RenderTexture.Release();
                GameObject.Destroy(RenderTexture);
                RenderTexture = null;
            }
            base.OnDestroy();
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            if (BootDataManager.Inst.GetParkEntry() == 0)
            {
                PgcTog.isOn = true;
                UIManager.Inst.OpenPanel(PanelId.BootPanel, 141);
            }else{
                if(args.Length > 0){
                    UgcBaseInfo mapInfo = args[0] as UgcBaseInfo;
                    if(mapInfo != null){
                        if(AIParkUtils.Inst.isOffical(mapInfo.id)){
                            PgcTog.isOn = true;

                        }else{
                            UgcTog.isOn = true;
                            RefreshView(mapInfo);
                            reqRfreshSelected();
                        }
                    }else{
                        PgcTog.isOn = true;
                    }
                }else{
                    PgcTog.isOn = true;
                }
            }
        }

        void reqRfreshSelected(){
            GameEntrySystem.Inst.SelectedReq(() => {
                if(ToggleSelected.isOn){
                    SelectAdpter.Data.ResetItems(data.SelectedMapInfos.Count);
                    SelectAdpter.Refresh();
                }
            });
        }

        public void RefreshView(UgcBaseInfo mapInfo)
        {
            MapInfo = mapInfo;

            var npcs = MapInfo.gameSetting.AICommonGameConfig.npcData;
            if (AIParkUtils.Inst.isOffical(MapInfo.id))
            {
                npcs = AIParkUtils.Inst.OfficalNpcData();
            }

            var pos = GetPos(0);
            var scal = GetScale(0);
            if (SelfWrap == null)
            {
                SelfWrap = AvatarController.Inst.CreateUIAvatar(AccountDataManager.Inst.UserInfo.avatarInfo.Clone());
                SelfWrap.SetParent(ModelRoot.transform, true);
                SelfWrap.Avatar.transform.localPosition = pos;
                SelfWrap.Avatar.transform.localScale = scal;
                AvatarCameraController.RotateTarget = SelfWrap.Avatar.gameObject.transform;
            }

            foreach (var item in WrapDic)
            {
                item.Value.Avatar.gameObject.SetActive(false);
            }

            for (int i = 0; i < npcs.Count; i++)
            {
                pos = GetPos(i + 1);
                scal = GetScale(i + 1);
                var npc = npcs[i];
                if (!WrapDic.ContainsKey(npc.id))
                {
                    var _data = CharacterData.DeserializeObject(npc.npcAvatarJson);
                    var _wrapper = AvatarController.Inst.CreateUIAvatar(_data);
                    _wrapper.SetParent(ModelRoot.transform, true);

                    WrapDic.Add(npc.id, _wrapper);
                }
                var wrap = WrapDic[npc.id];
                wrap.Avatar.transform.localPosition = pos;
                wrap.Avatar.transform.localScale = scal;
                wrap.Avatar.gameObject.SetActive(true);
            }
        }

        public Vector3 GetPos(int idx)
        {
            var off = Mathf.Lerp(0.4f, 0.3f, idx / 20f);
            var x = (idx % 2 == 0 ? 1 : -1) * ((idx + 1) / 2) * off;
            if (idx == 0)
            {
                x = 0;
            }
            return new Vector3(x, 0, -1 - (idx + 1) / 2 * 0.4f);
        }
        public Vector3 GetScale(int idx)
        {
            var scale = 1 - (idx + 1) / 2 * 0.1f;
            if (idx == 0)
            {
                scale = 1;
            }
            return Vector3.one * scale;
        }

        private UgcBaseInfo GetMapInfo(int index)
        {
            if (ToggleCollect.isOn)
            {
                return data.CollectMapInfos[index].ugcInfo;
            }
            else if (ToggleSelected.isOn)
            {
                return data.SelectedMapInfos[index].ugcInfo;
            }
            else
            {
                return data.MapInfos[index];
            }
        }

        private void OnItemSelected(UgcBaseInfo mapInfo)
        {
            foreach (var item in SelectAdpter._VisibleItems)
            {
                item.ContainingCellViewsHolders[0].item.On.gameObject.SetActive(false);
            }
            RefreshView(mapInfo);
        }

        private void OnPullRefresh()
        {
            if (ToggleCollect.isOn)
            {
                GameEntrySystem.Inst.CollectReq((items) =>
                {
                    PullToRefreshBehaviour.HideGizmo();
                    SelectAdpter.Data.ResetItems(data.CollectMapInfos.Count);
                    SelectAdpter.Refresh();
                });
            }
        }

        #region 按钮
        void OnClose()
        {
            var panel = UIManager.Inst.FindPanel<GameEntryParkPanel>(PanelId.GameEntryParkPanel);
            if (panel != null)
            {
                panel.RefreshView(panel.MapInfo);
            }
            CloseSelf();
        }

        void OnUgcTog(bool bo)
        {
            if (bo)
            {
                PgcInfo.gameObject.SetActive(false);
                UgcInfo.gameObject.SetActive(true);
                ToggleSelected.isOn = true;
            }
        }
        void OnPgcTog(bool bo)
        {
            if (bo)
            {
                PgcInfo.gameObject.SetActive(true);
                UgcInfo.gameObject.SetActive(false);
                RefreshView(AIParkUtils.Inst.GetOfficalMapInfo());
                PgcInfo.SetData(AIParkUtils.Inst.GetOfficalMapInfo(), null, -1);
            }
        }
        void OnSelectBtn()
        {
            GameEntrySystem.Inst.SetLastPlayMapInfo(MapInfo);
            var p = UIManager.Inst.FindPanel<GameEntryParkPanel>(PanelId.GameEntryParkPanel);
            p.SetMapInfo(MapInfo);
            OnClose();
        }
        void OnMoreBtn()
        {
            GameEntrySystem.Inst.OpenParkGroupPanel();
        }
        void OnToggleCollect(bool bo)
        {
            if (bo)
            {
                //收藏
                NoneText.text = "";
                data.CollectResInfoList = null;
                data.CollectMapInfos.Clear();
                GameEntrySystem.Inst.CollectReq((list) =>
                {
                    PullToRefreshBehaviour.HideGizmo();
                    SelectAdpter.Data.ResetItems(data.CollectMapInfos.Count);
                    SelectAdpter.Refresh();
                });
            }
        }
        void OnToggleRecord(bool bo)
        {
            if (bo)
            {
                NoneText.text = "";
                if (data.MapInfos.Count <= 0) 
                {
                    NoneText.text = "无历史游玩记录";
                }
                else
                {
                    NoneText.text = "";
                }
                SelectAdpter.Data.ResetItems(data.MapInfos.Count);
            }
        }
        void OnToggleSelected(bool bo)
        {
            if (bo)
            {
                //精选
                NoneText.text = "";
                SelectAdpter.Data.ResetItems(data.SelectedMapInfos.Count);
            }
        }
        #endregion








    }
}