using AIGame.Base;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Avatar;
using GameData.Base;
using GameData.BaseInfo;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class GameEntryParkPanel : BasePanel<GameEntryParkPanel>
    {
        public CButton CloseBtn;
        public CButton CreatMapBtn;
        public Button ChangeMapBtn;
        public CButton PlayBtn;

        public CButton PetBtn;
        public CButton EditBtn;
        public CButton SheziBtn;
        public CButton PictureBtn;

        public GameObject ModelRoot;
        public RawImage RawImage;
        public AvatarCameraController AvatarCameraController;

        public RemoteImageBehaviour MapCover;

        public GameObject Info;

        [HideInInspector] public UgcBaseInfo MapInfo;

        [HideInInspector] private Dictionary<string, CharacterWrap> WrapDic = new Dictionary<string, CharacterWrap>();
        private CharacterWrap SelfWrap;

        private RenderTexture RenderTexture;
        public GameObject FirstGuideGo;
        public CButton FirstGuideBtn;
        public Image guideHeadImg;

        public override void OnCreate()
        {
            base.OnCreate();

            CloseBtn.onClick.AddListener(CloseSelf);
            CreatMapBtn.onClick.AddListener(OnCreatMapBtn);
            ChangeMapBtn.onClick.AddListener(OnChangeMapBtn);
            PlayBtn.onClick.AddListener(OnPlayBtn);

            PetBtn.onClick.AddListener(OnPetBtn);
            EditBtn.onClick.AddListener(OnEditBtn);
            SheziBtn.onClick.AddListener(OnSheziBtn);
            PictureBtn.onClick.AddListener(OnPictureBtn);

            FirstGuideBtn.onClick.AddListener(OnFirstGuideBtn);
            FirstGuideGo.SetActive(false);


            RenderTexture = new RenderTexture(2000,800,24);
            RawImage.texture = RenderTexture;
            AvatarCameraController.roleCamera.targetTexture = RenderTexture;

            if(GameEntrySystem.Inst.data.LastPlayMapInfo != null)
            {
                RefreshView(GameEntrySystem.Inst.data.LastPlayMapInfo);
            }
            else
            {
                RefreshView(AIParkUtils.Inst.GetOfficalMapInfo());
            }

            AccountDataManager.Inst.AddAvatarChangeListener(RefreshSelfAvatar);
            AIParkUtils.Inst.ReportOncePerDay_AILARPGameHall();

            try
            {
                string spriteatlasPath = "Assets/Loadable/UI/SpriteAltas/NpcHead.spriteatlas";
                Sprite spl = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "1011", gameObject);
                guideHeadImg.sprite = spl;
            }
            catch (System.Exception e)
            {
                LoggerUtils.LogError(e.Message);
            }
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            if (BootDataManager.Inst.GetParkEntry() == 0)
            {
                FirstGuideGo.SetActive(true);
            }
            //else
            //{
            //    if (BootDataManager.Inst.GetParkStart() == 0)
            //    {
            //        UIManager.Inst.OpenPanel(PanelId.BootPanel, 125);
            //    }
            //}
        }

        protected override void OnDestroy()
        {
            AccountDataManager.Inst.RemoveAvatarChangeListener(RefreshSelfAvatar);
            if (RenderTexture != null) 
            {
                RenderTexture.Release();
                GameObject.Destroy(RenderTexture);
                RenderTexture = null;
            }
            base.OnDestroy();
        }

        public void RefreshSelfAvatar(AccountUserInfo userInfo) {
            var pos = GetPos(0);
            var scal = GetScale(0);
            if (SelfWrap != null)
            {
                GameObject.Destroy(SelfWrap.Avatar.gameObject);
                SelfWrap = null;
            }
            SelfWrap = AvatarController.Inst.CreateUIAvatar(userInfo.avatarInfo.Clone());
            SelfWrap.SetParent(ModelRoot.transform, true);
            SelfWrap.Avatar.transform.localPosition = pos;
            SelfWrap.Avatar.transform.localScale = scal;
            AvatarCameraController.RotateTarget = SelfWrap.Avatar.gameObject.transform;
            switch ((CustomBodyTypeController.BodyType)userInfo.avatarInfo.bodyType)
            {
                
                case CustomBodyTypeController.BodyType.Type4:
                    SelfWrap.Avatar.transform.localPosition = pos + new Vector3(0, 0.18f, 0);
                    break;
                case CustomBodyTypeController.BodyType.Type1:
                case CustomBodyTypeController.BodyType.Type2:
                    SelfWrap.Avatar.transform.localPosition = pos + new Vector3(0, -0.08f, 0);
                    break;
                case CustomBodyTypeController.BodyType.Type6:
                    SelfWrap.Avatar.transform.localPosition = pos + new Vector3(0, -0.08f, 0);
                    SelfWrap.Avatar.transform.localScale = new Vector3(0.66f, 0.66f, 0.66f);
                    break;
                default:
                    SelfWrap.Avatar.transform.localPosition = pos + Vector3.zero;
                    break;
            }
        }

        public void SetMapInfo(UgcBaseInfo mapInfo) {
            MapInfo = mapInfo;
        }

        public void RefreshView(UgcBaseInfo mapInfo) {
            MapInfo = mapInfo;

            RefreshSelfAvatar(AccountDataManager.Inst.UserInfo);

            var npcs = MapInfo.gameSetting.AICommonGameConfig.npcData;
            if (AIParkUtils.Inst.isOffical(MapInfo.id))
            {
                npcs = AIParkUtils.Inst.OfficalNpcData();
                MapCover.Load(AIParkUtils.Inst.OfficalCover());
            }
            else
            {
                MapCover.Load(MapInfo.cover);
            }
            var pos = GetPos(0);
            var scal = GetScale(0);

            foreach (var item in WrapDic)
            {
                item.Value.Avatar.gameObject.SetActive(false);
            }

            StartCoroutine(RefreshOtherAvatar(npcs));
            //for (int i = 0; i < npcs.Count; i++)
            //{
            //    pos = GetPos(i + 1);
            //    scal = GetScale(i + 1);
            //    var npc = npcs[i];
            //    if (!WrapDic.ContainsKey(npc.id))
            //    {
            //        var _data = CharacterData.DeserializeObject(npc.npcAvatarJson);
            //        var _wrapper = AvatarController.Inst.CreateUIAvatar(_data);
            //        _wrapper.SetParent(ModelRoot.transform, true);
            //
            //        WrapDic.Add(npc.id, _wrapper);
            //    }
            //    var wrap = WrapDic[npc.id];
            //    wrap.Avatar.transform.localPosition = pos;
            //    wrap.Avatar.transform.localScale = scal;
            //    wrap.Avatar.gameObject.SetActive(true);
            //}
        }

        IEnumerator RefreshOtherAvatar(List<AICommonGameConfig_NPC> npcs) { 
            yield return null;

            var pos = GetPos(0);
            var scal = GetScale(0);

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
                switch ((CustomBodyTypeController.BodyType)wrap.ChaData.bodyType)
                {
                    case CustomBodyTypeController.BodyType.Type4:
                        wrap.Avatar.transform.localPosition = pos + new Vector3(0, 0.18f, 0);
                        break;
                    case CustomBodyTypeController.BodyType.Type1:
                    case CustomBodyTypeController.BodyType.Type2:
                        SelfWrap.Avatar.transform.localPosition = pos + new Vector3(0, -0.08f, 0);
                        break;
                    case CustomBodyTypeController.BodyType.Type6:
                        wrap.Avatar.transform.localPosition = pos + new Vector3(0, -0.08f, 0);
                        wrap.Avatar.transform.localScale = new Vector3(0.66f, 0.66f, 0.66f);
                        break;
                    default:
                        wrap.Avatar.transform.localPosition = pos + Vector3.zero;
                        break;
                }
                yield return null;
            }

        }

        public Vector3 GetPos(int idx) 
        {
            var off = Mathf.Lerp(0.4f,0.3f,idx / 20f);
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
            if (idx == 0) {
                scale = 1;
            }
            return Vector3.one * scale;
        }

        #region 按钮

        void OnCreatMapBtn() {
            GameEntrySystem.Inst.OpenParkWorkPanel();
        }
        void OnChangeMapBtn()
        {
            foreach (var item in WrapDic)
            {
                item.Value.Avatar.gameObject.SetActive(false);
            }
            if (SelfWrap != null)
            {
                SelfWrap.Avatar.gameObject.SetActive(false);
            }
            GameEntrySystem.Inst.OpenParkSelectPanel(MapInfo);
            foreach (var item in WrapDic)
            {
                item.Value.Avatar.gameObject.SetActive(false);
            }
        }
        void OnPlayBtn()
        {
            BootDataManager.Inst.SetParkStart(1);
            if (AIParkUtils.Inst.isOffical(MapInfo.id))
            {
                AIParkUtils.Inst.EnterOfficalParkGame();
            }
            else
            {
                GameEntrySystem.Inst.SetLastPlayMapInfo(MapInfo);
                AIParkUtils.Inst.EnterUgcParkGame(MapInfo.id);
            }

        }
        void OnPetBtn()
        {
            UIManager.Inst.OpenPanel(PanelId.FittingRoomPanel, true);
        }
        void OnEditBtn()
        {
            UIManager.Inst.OpenPanel(PanelId.FittingRoomPanel);
        }
        void OnSheziBtn()
        {
            UIManager.Inst.OpenPanel(PanelId.OcChangePanel, UI.UIPanels.FittingRoom.OcChangeScene.Lobby);
        }
        void OnPictureBtn()
        {
            Info.gameObject.SetActive(false);

            var p = UIManager.Inst.OpenPanel<ScreenShotPanel>(PanelId.ScreenShotPanel);
            p.CallBack = () => { Info.gameObject.SetActive(true); };
        }
        #endregion

        void OnFirstGuideBtn(){
            FirstGuideGo.SetActive(false);
            UIManager.Inst.OpenPanel(PanelId.BootPanel, 126);
        }
    }
}