using System;
using System.Collections.Generic;
using Game.Avatar;
using Game.Base;
using Game.Props.PropsManagers;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.MapData;
using GameData.UGCData;
using Google.Protobuf;
using Pb.Game;
using Pb.Map;
using UGCAsset;
using UI;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.MusicalInstrument
{
    public class InstrumentChooseAnimPanelData
    {
        public SkinInfo CurSkinInfo;
        public InstrumentInfo CurInstrumentInfo;
        public Action<string> OnSelectUgcAnim;
    }
    public class MusicalInstrumentChooseAnimPanel : BasePanel<MusicalInstrumentChooseAnimPanel>
    {
        public Transform _trans_Bg;
        public CButton Btn_Return;
        public Transform SelectedItemContent;
        public CButton Btn_Adjust;
        
        [Header("预览")]
        public Transform characterRoot;
        public CharacterWrap characterWrap;
        public UIDragUtil dragUtil;
        public SyllablePreviewPanel SyllablePreview;
        public MusicalInstrumentChooseAnimLeftItem _leftItemPrefab;
        
        private PlayerHoldBehaviour _playerHoldBehaviour;
        private InstrumentChooseAnimPanelData _curPanelData;
        private List<MusicalInstrumentChooseAnimLeftItem> _animLeftItems = new List<MusicalInstrumentChooseAnimLeftItem>();
        private PreviewLightRecordData _srcLightRecordData;

        private List<string> _animSortList = new List<string>() { "1008", "1003", "1009", "1007", "1006", "1001", "1004", "1005", "1002" };
        public override void OnCreate()
        {
            base.OnCreate();
            
            InitBG();
            AddListener();
            _srcLightRecordData = AmbientLightManager.Inst.OpenPreviewDirLight();
        }
        
        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            _curPanelData = (InstrumentChooseAnimPanelData)args[0];
            SaveToCache();
            StartPreview();
            InitAnimItems();
            SyllablePreview.InitData(_curPanelData.CurInstrumentInfo.toneInfo.Clone(), OnItemSelected);

            if (args.Length == 2)
            {
                Btn_Adjust.gameObject.SetActive(true);
                Btn_Adjust.onClick.RemoveAllListeners();
                Btn_Adjust.onClick.AddListener(OnBtnAdjustClick);
            }
        }

        public override void OnHidden()
        {
            base.OnHidden();
            AmbientLightManager.Inst.ClosePreviewDirLight(_srcLightRecordData);
        }

        public void StartPreview()
        {
            this.gameObject.SetActive(true);

            var avatarJson = AccountDataManager.Inst.UserInfo.avatarJson;
            CharacterData tempAvatarInfo = CharacterData.DeserializeObject(avatarJson).Clone();
            tempAvatarInfo.ChangeSkinData(_curPanelData.CurSkinInfo);
            
            if (characterWrap == null)
            {
                characterWrap = AvatarController.Inst.CreateUIAvatar(tempAvatarInfo);
                characterWrap.SetParent(characterRoot, true);
            }
            else
            {
                characterWrap.RefreshAvatar(tempAvatarInfo);
            }
            
            _playerHoldBehaviour = characterWrap.Avatar.GetComponentInChildren<PlayerHoldBehaviour>();

            dragUtil.RotateTarget = characterWrap.Avatar.transform;
        }
        
        private void InitBG()
        {
            if (_trans_Bg == null)
            {
                return;
            }

            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(_trans_Bg);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
            {
                "music_icon_1", "music_icon_2", "music_icon_3"
            });
            item.gameObject.SetActive(true);
        }

        private void AddListener()
        {
            Btn_Return.onClick.AddListener(CloseSelf);
        }

        private void InitAnimItems()
        {
            if (_curPanelData?.CurInstrumentInfo == null)
            {
                LoggerUtils.LogError("_curPanelData?.CurInstrumentInfo == null");
                return;
            }

            
            var curEmoId = _curPanelData.CurInstrumentInfo.moveId;
            var curSelectedEmoId = curEmoId;

            for (int i = 0; i < _animSortList.Count; i++)
            {
                var item = GameObject.Instantiate(_leftItemPrefab, SelectedItemContent);
                item.Init(_animSortList[i], OnItemSelected);
                _animLeftItems.Add(item);
            }

            if (_animLeftItems.Count != 0)
            {
                var curSelectedItem = _animLeftItems.Find(x => x.GetCurEmoId() == curSelectedEmoId);
                curSelectedItem.OnBtnClick();
            }
        }

        private void OnItemSelected(string emoId)
        {
            SyllablePreview.DisableAllSelected();
            _curPanelData?.OnSelectUgcAnim?.Invoke(emoId);
            
            _playerHoldBehaviour.PreviewUGCInstrument(_curPanelData.CurInstrumentInfo);
            
            _animLeftItems.ForEach(x=>x.SetSelectedState(false));
        }
        
        private void OnItemSelected(ToneInfo toneInfo, int syllableId)
        {
            SyllablePlayData data = new SyllablePlayData();
            data.SyllId = syllableId;
            _playerHoldBehaviour.PlayMusicSyllable(data);
        }

        private void SaveToCache()
        {
            if(GameController.IsInHallScene())
                return;
            
            var itemData = GamePropNodeManager.Inst.SaveUgcItemData();
            var ugcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<SkinInfo>();
            var skinActionInfo = GameDataManager.Inst.mapGlobalData.skinActionInfo;
            var propManager = GlobalNodeManager.Inst.Get<PropManager>();
            propManager.RemovePropData(ugcInfo.id);
            if (ugcInfo.skinDetailInfo == null) {
                ugcInfo.skinDetailInfo = SkinDetailInfo.FromDetailInfo(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
            }
            ugcInfo.skinDetailInfo.size = itemData.Size.ToVector3();
            var metaDataBytes = itemData.ToByteArray();
            var itemPb = MapPbDataTool.ParsePropPb(metaDataBytes);
            if (itemPb != null) {
                if (itemPb.UgcmatData != null) {
                    GameUgcMatManager.Inst.AddUGCMatData(itemPb.UgcmatData);
                    var pNodeData = AssetPropNodeManager.Inst.GetEmptyNodeData(ugcInfo.id);
                    pNodeData.Prims.AddRange(itemPb.NodeData.Prims);
                    propManager?.AddPropData(ugcInfo.id, pNodeData);
                }
            }
        }

        private void OnBtnAdjustClick()
        {
            var itemData = GamePropNodeManager.Inst.SaveUgcItemData();
            var ugcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<SkinInfo>();
            var skinActionInfo = GameDataManager.Inst.mapGlobalData.skinActionInfo;
            var instrumentDraftInfo = InstrumentAssetManager.Inst.GetOrCreateDraftInfo(ugcInfo, skinActionInfo);
            if (instrumentDraftInfo == null)
            {
                return;
            }
            
            var publishStateMachine = new UGCPublishStateMachine();
            publishStateMachine.SetStates(new List<UGCPublishStateBase>() {
                new UGCPublishStateBase(UGCPublishState.SetInstrumentAdjustWithAnim),
            });
            publishStateMachine.SetEditData(new InstrumentEditData() {
                skinActionDraftInfo = instrumentDraftInfo,
                metaDataBytes = itemData.ToByteArray(),
            });

            publishStateMachine.Start();
        }

        public void RefreshPreview()
        {
            if (_playerHoldBehaviour != null)
            {
                _playerHoldBehaviour.PreviewUGCInstrument(_curPanelData.CurInstrumentInfo);
            }
        }
    }
}