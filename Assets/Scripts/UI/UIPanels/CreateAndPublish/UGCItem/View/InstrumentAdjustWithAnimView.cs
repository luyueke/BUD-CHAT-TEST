using System;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.MusicalInstrument;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;
using GameData.UGCData;
using Pb.Game;
using UGCAsset;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class InstrumentAdjustWithAnimView : UGCBaseStateView {
        [SerializeField] private Transform characterRoot;

        [SerializeField] private AdjustView adjustView;

        [SerializeField] private AvatarCameraController cameraController;

        [SerializeField] private RawImage avatarRawImage;
        [SerializeField] private CButton Btn_SetTone;
        [SerializeField] private CButton Btn_SetAnim;
        [SerializeField] private SyllablePreviewPanel syllablePreviewPanel;
        [SerializeField] private CButton btnReset;

        private string _syllableItemPath = "Assets/Loadable/UI/UIPanel/MusicalInstrument/AdjustSyllableItem.prefab";
        private CharacterWrap characterWrap;
        private PlayerHoldBehaviour _playerHoldBehaviour;
        private CharacterData saveCharacterData;
        private InstrumentEditData instrumentEditData;

        private Vector3 rDef;
        private Vector3 pDef;
        private Vector3 sDef;

        private Vector3 Scale_Def = Vector3.one * (1 / 1.7f);
        private Vector3 Pos_Def = Vector3.zero;
        private Vector3 Rot_Def = Vector3.zero;

        private List<Vector3> VE_Limt = new List<Vector3>() { new Vector3(0, 0.5f, 0), new Vector3(0, -0.5f, 0) };
        private List<Vector3> HO_Limt = new List<Vector3>() { new Vector3(1f, 0, 0), new Vector3(-1f, 0, 0) };
        private List<Vector3> F_Limt = new List<Vector3>() {  new Vector3(0, 0, 0.7f), new Vector3(0, 0, -0.7f) };

        private List<Vector3> XR_Limt = new List<Vector3>() { new Vector3(-180, 0, 0), new Vector3(180, 0, 0) };
        private List<Vector3> YR_Limt = new List<Vector3>() { new Vector3(0, -180, 0), new Vector3(0, 180, 0) };
        private List<Vector3> ZR_Limt = new List<Vector3>() { new Vector3(0, 0, -180), new Vector3(0, 0, 180) };

        private List<Vector3> S_Limt = new List<Vector3>() { new Vector3(0.1f, 0.1f, 0.1f), new Vector3(2, 2, 2) };
        private List<Vector3> ROT_Limt = new List<Vector3>() { new Vector3(0, 0, 0), new Vector3(360, 0, 0) };

        private Vec3 Temp_Pos;    // 位置
        private Vec3 Temp_Rot;    // 旋转
        private Vec3 Temp_Sca;    // 缩放


        public override void Awake() {
            base.Awake();
            btnReset.onClick.AddListener(OnReset);
        }

        private void OnReset() {
            var tmpCommonData = AvatarCommonData.From(instrumentEditData.skinActionDraftInfo._skinInfo);
            S_Limt = tmpCommonData.scaLimit;
            LoggerUtils.Log("S_Limt:" + S_Limt[1]);

            var configDef = MusicalInstrumentUtils.GetDefaultInstrumentDetailInfo();
            if (configDef.sDef.magnitude > S_Limt[1].magnitude) {
                configDef.sDef = S_Limt[1];
            }


            pDef = configDef.pDef;
            rDef = configDef.rDef;
            sDef = tmpCommonData.sDef;
            adjustView.ResetAdjust();
        }



        public override void Show() {
            base.Show();

            if(UIManager.Inst.TryFindPanel<MusicalInstrumentChooseAnimPanel>(WindowId.UGCItemEditWindow, PanelId.MusicalInstrumentChooseAnimPanel, out MusicalInstrumentChooseAnimPanel panel))
            {
                Btn_SetTone.gameObject.SetActive(false);
                Btn_SetAnim.gameObject.SetActive(false);
            }

            Btn_SetTone.onClick.AddListener(OnBtnSetToneClick);
            Btn_SetAnim.onClick.AddListener(OnBtnSetAnimClick);
            ShowCharacter();
            var adjustList = AdjustTypeAdjustItems();
            adjustView.SetAdjustItems(adjustList);
            adjustView.ResetAdjust();
            avatarRawImage.rectTransform.sizeDelta = new Vector2(1084.0f/1124 * Screen.currentResolution.height, Screen.currentResolution.height);
            syllablePreviewPanel.InitData(instrumentEditData.GetToneInfo().Clone(), OnItemSelected);
        }

        private void OnBtnSetToneClick()
        {
            ToneStudioPanelData panelData = new ToneStudioPanelData();
            panelData.CurToneInfo = instrumentEditData.GetToneInfo();
            panelData.OnSelectToneItem = OnSelectedToneInfo;
            UIManager.Inst.OpenPanel(PanelId.ToneStudioPanel, panelData);
        }

        private void OnSelectedToneInfo(ToneInfo toneInfo)
        {
            instrumentEditData.SetToneInfo(toneInfo);
            syllablePreviewPanel.InitData(toneInfo.Clone(), OnItemSelected);

            _playerHoldBehaviour?.PreviewUGCInstrument(instrumentEditData.GetInstrumentInfo());
        }

        private void OnBtnSetAnimClick()
        {
            InstrumentChooseAnimPanelData panelData = new InstrumentChooseAnimPanelData();
            panelData.OnSelectUgcAnim = OnSelectedUgcAnimInfo;
            panelData.CurInstrumentInfo = instrumentEditData.GetInstrumentInfo();
            panelData.CurSkinInfo = instrumentEditData.GetSkinInfo().Clone();
            UIManager.Inst.OpenPanel(PanelId.MusicalInstrumentChooseAnimPanel, panelData);
        }

        private void OnSelectedUgcAnimInfo(string id)
        {
            instrumentEditData.SetAnimId(id);

            if (characterWrap != null) {
                saveCharacterData.ChangeSkinData(instrumentEditData.GetSkinInfo());
                characterWrap.ChangeUGCPart(instrumentEditData.GetSkinInfo());
            }

            _playerHoldBehaviour?.PreviewUGCInstrument(instrumentEditData.GetInstrumentInfo());
        }

        protected override void SyncEditData() {
            base.SyncEditData();
            instrumentEditData = editData as InstrumentEditData;
            if (instrumentEditData != null) {
                if (string.IsNullOrEmpty(instrumentEditData.GetInfo().metaDataUrl)) {
                    instrumentEditData.GetInfo().metaDataUrl = Es.DataTables.GetClothesTemplate(instrumentEditData.GetInfo().templateId).MetaDataUrl;
                }

                // 乐器限制大小
                var tmpCommonData = AvatarCommonData.From(instrumentEditData.skinActionDraftInfo._skinInfo);
                S_Limt = tmpCommonData.scaLimit;

                var configDef = instrumentEditData.GetInstrumentDetailInfo();

                if (configDef == null)
                {
                    configDef = MusicalInstrumentUtils.GetDefaultInstrumentDetailInfo();
                    configDef.sDef = tmpCommonData.sDef;
                }

                if (configDef.sDef.magnitude > S_Limt[1].magnitude) {
                    configDef.sDef = S_Limt[1];
                }
                pDef = configDef.pDef;
                rDef = configDef.rDef;
                sDef = configDef.sDef;


                Temp_Pos = configDef.pDef;
                Temp_Rot = configDef.rDef;
                Temp_Sca = configDef.sDef;
            }
        }

        protected override void OnNextBtnClick()
        {
            ApplyData();

            if(UIManager.Inst.TryFindPanel<MusicalInstrumentChooseAnimPanel>(WindowId.UGCItemEditWindow, PanelId.MusicalInstrumentChooseAnimPanel, out MusicalInstrumentChooseAnimPanel panel))
            {
                panel.RefreshPreview();
            }

            base.OnNextBtnClick();
        }

        private void ApplyData()
        {
            instrumentEditData.SetInstrumentDetailInfo(new InstrumentDetailInfo()
            {
                pDef =  this.pDef,
                rDef =  this.rDef,
                sDef =  this.sDef,
            });
        }


        public void ShowCharacter() {
            if (characterWrap != null) {
                saveCharacterData.ChangeSkinData(instrumentEditData.GetSkinInfo());
                characterWrap.ChangeUGCPart(instrumentEditData.GetSkinInfo());
                return;
            }

            saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();
            saveCharacterData.ChangeSkinData(instrumentEditData.GetSkinInfo());
            if (saveCharacterData == null)
                saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1).Clone();
            if (saveCharacterData != null) {
                characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
                characterWrap.SetParent(characterRoot, true);
                cameraController.RotateTarget = characterRoot;
                cameraController.SetCameraZoom(ViewType.ZoomWholeBody);
            }

            _playerHoldBehaviour = characterWrap.Avatar.GetComponentInChildren<PlayerHoldBehaviour>();
            _playerHoldBehaviour.PreviewUGCInstrument(instrumentEditData.GetInstrumentInfo());
        }

        public List<RoleDataAdjust> AdjustTypeAdjustItems()
        {
            var detailInfo = instrumentEditData.GetInstrumentDetailInfo();
            if (detailInfo != null)
            {
                Temp_Pos = detailInfo.pDef;
                Temp_Rot = detailInfo.rDef;
                Temp_Sca = detailInfo.sDef;
            }

            return new List<RoleDataAdjust>() {
                GetRoleDataAdjust(AdjustViewItemType.Size),
                GetRoleDataAdjust(AdjustViewItemType.UpDown),
                GetRoleDataAdjust(AdjustViewItemType.LeftRight),
                GetRoleDataAdjust(AdjustViewItemType.FrontBack),
                GetRoleDataAdjust(AdjustViewItemType.XRotation),
                GetRoleDataAdjust(AdjustViewItemType.YRotation),
                GetRoleDataAdjust(AdjustViewItemType.ZRotation)
            };
        }


        public RoleDataAdjust GetRoleDataAdjust(AdjustViewItemType type) {
            AdjustAxis axis;
            switch (type) {
                case AdjustViewItemType.Size:
                    axis = AdjustAxis.None;
                    return new() {
                        AdjustType = AdjustViewItemType.Size,
                        Getter = () => Temp_Sca ?? Scale_Def,
                        Setter = (v) => Temp_Sca = v,
                        Axis = () => axis,
                        Limit = () => S_Limt,
                        Default = () => sDef,
                        Apply = () => ChangeSize(0, Temp_Sca)
                    };
                case AdjustViewItemType.UpDown:
                    axis = AdjustAxis.Y;
                    return new() {
                        AdjustType = AdjustViewItemType.UpDown,
                        Getter = () => Temp_Pos ?? Pos_Def,
                        Setter = (v) => Temp_Pos = v,
                        Axis = () => axis,
                        Limit = () => VE_Limt,
                        Default = () => pDef,
                        Apply = () => ChangeMove(0, Temp_Pos)
                    };
                case AdjustViewItemType.LeftRight:
                    axis = AdjustAxis.X;
                    return new() {
                        AdjustType = AdjustViewItemType.LeftRight,
                        Getter = () => Temp_Pos ?? Pos_Def,
                        Setter = (v) => Temp_Pos = v,
                        Axis = () => axis,
                        Limit = () => HO_Limt,
                        Default = () => pDef,
                        Apply = () => ChangeMove(0, Temp_Pos)
                    };
                case AdjustViewItemType.FrontBack:
                    axis = AdjustAxis.Z;
                    return new() {
                        AdjustType = AdjustViewItemType.FrontBack,
                        Getter = () => Temp_Pos ?? Pos_Def,
                        Setter = (v) => Temp_Pos = v,
                        Axis = () => axis,
                        Limit = () => F_Limt,
                        Default = () => pDef,
                        Apply = () => ChangeMove(0, Temp_Pos)
                    };
                case AdjustViewItemType.XRotation:
                    axis = AdjustAxis.X;
                    return new() {
                        AdjustType = AdjustViewItemType.XRotation,
                        Getter = () => Temp_Rot ?? Rot_Def,
                        Setter = (v) => Temp_Rot = v,
                        Axis = () => axis,
                        Limit = () => XR_Limt,
                        Default = () => rDef,
                        Apply = () => ChangeRotate(0, Temp_Rot)
                    };
                case AdjustViewItemType.YRotation:
                    axis =AdjustAxis.Y;
                    return new() {
                        AdjustType = AdjustViewItemType.YRotation,
                        Getter = () => Temp_Rot ?? Rot_Def,
                        Setter = (v) => Temp_Rot = v,
                        Axis = () => axis,
                        Limit = () => YR_Limt,
                        Default = () => rDef,
                        Apply = () => ChangeRotate(0, Temp_Rot)
                    };
                case AdjustViewItemType.ZRotation:
                    axis = AdjustAxis.Z;
                    return new() {
                        AdjustType = AdjustViewItemType.ZRotation,
                        Getter = () => Temp_Rot ?? Rot_Def,
                        Setter = (v) => Temp_Rot = v,
                        Axis = () => axis,
                        Limit = () => ZR_Limt,
                        Default = () => rDef,
                        Apply = () => ChangeRotate(0, Temp_Rot)
                    };
                // case AdjustViewItemType.Spacing:
                //     axis = AdjustAxis.Z;
                //     return new()
                //     {
                //         AdjustType = AdjustViewItemType.Spacing,
                //         Getter = () => Temp_Pos != null ? Temp_Pos : pDef,
                //         Setter = (v) => Temp_Pos = v,
                //         Axis = () => axis,
                //         Limit = () => HO_Limt,
                //         Default = () => pDef,
                //         Apply = () => ChangeMove(0, Temp_Pos)
                //     };
                // case AdjustViewItemType.Vertical:
                //     axis = AdjustAxis.None;
                //     return new()
                //     {
                //         AdjustType = AdjustViewItemType.Vertical,
                //         Getter = () => Temp_Pos != null ? Temp_Pos : Pos_Def,
                //         Setter = (v) => Temp_Pos = v,
                //         Axis = () => axis,
                //         Limit = () => VE_Limt,
                //         Default = () => Pos_Def,
                //         Apply = () => ChangeMove(0, Temp_Pos)
                //     };
                // case AdjustViewItemType.Rotation:
                //     axis = AdjustAxis.None;
                //     return new()
                //     {
                //         AdjustType = AdjustViewItemType.Rotation,
                //         Getter = () => Temp_Rot != null ? Temp_Rot : Rot_Def,
                //         Setter = (v) => Temp_Rot = v,
                //         Axis = () => axis,
                //         Limit = () => ROT_Limt,
                //         Default = () => Rot_Def,
                //         Apply = () => ChangeRotate(0, Temp_Rot)
                //     };
            }

            return new RoleDataAdjust();
        }

        void ChangeSize(int resType, Vec3 size) {
            // characterWrap.Scale(resType, size);
            _playerHoldBehaviour.SetInstrumentScl(size);
            sDef = size;

            ApplyData();
        }

        void ChangeMove(int resType, Vec3 pos) {
            // characterWrap.Move(resType, pos);
            _playerHoldBehaviour.SetInstrumentPos(pos);
            pDef = pos;

            ApplyData();
        }

        void ChangeRotate(int resType, Vec3 rot) {
            // characterWrap.Rotate(resType, rot);
            _playerHoldBehaviour.SetInstrumentRot(rot);
            rDef = rot;

            ApplyData();
        }

        private void OnItemSelected(ToneInfo toneInfo, int syllableId)
        {
            SyllablePlayData data = new SyllablePlayData();
            data.SyllId = syllableId;
            _playerHoldBehaviour.PlayMusicSyllable(data);
        }
    }
}
