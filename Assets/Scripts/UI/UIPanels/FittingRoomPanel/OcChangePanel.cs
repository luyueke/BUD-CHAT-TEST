using System;
using System.Collections;
using System.Collections.Generic;
using Game.Audio;
using Game.Avatar;
using Game.Store;
using GameData.BaseInfo;
using UI.Base;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public enum OcChangeScene
    {
        Lobby,
        Play,
        DoubleEmote,
        EditNpc,
    }

    public class OcChangePanel : BasePanel<OcChangePanel>
    {
        [Header("调整界面")] 
        [SerializeField] internal Text txt_title;
        [SerializeField] internal OcList ocList;
        [SerializeField] internal Button closeButton;
        [SerializeField] internal Button sureButton;
        [SerializeField] internal Button jumpButton;
        [SerializeField] internal Toggle avatarTog;
        [SerializeField] internal Toggle petTog;

        private SkinType _curSkinType = SkinType.Avatar;
        private OcChangeScene scene;
        public Action<BaseAvatarData> OnCloseAction;
        public Action<BaseAvatarData> OnChooseAvatarAction;

        private Vector3 ocAnchorPos = Vector3.zero;
        public override void OnCreate()
        {
            base.OnCreate();
            ocList.SetCallback(OnOcSelect);
            closeButton.onClick.AddListener(OnCloseClick);
            sureButton.onClick.AddListener(OnSureClick);
            jumpButton.onClick.AddListener(OnJumpClick);
            avatarTog.onValueChanged.AddListener(OnAvatarToggleClick);
            petTog.onValueChanged.AddListener(OnPetToggleClick);
            avatarTog.isOn = true;
            ocAnchorPos = ocList.GetComponent<RectTransform>().anchoredPosition;
            OnAvatarToggleClick(true);
            VipDataManager.Inst.UpdateVipStatus();
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            if (args.Length > 0)
            {
                scene = (OcChangeScene)args[0];
                jumpButton.gameObject.SetActive(scene == OcChangeScene.Lobby || scene == OcChangeScene.Play);
            }
            else
            {
                scene = OcChangeScene.Lobby;
            }
            ocList.gameObject.SetActive(true);

            if (scene == OcChangeScene.EditNpc)
            {
                txt_title.SetLocalText("选择NPC形象");
            }
        }

        public void SetAvatarChangeOc()
        {
            avatarTog.transform.parent.gameObject.SetActive(false);
            ocList.GetComponent<RectTransform>().anchoredPosition = ocAnchorPos + new Vector3(0, 57, 0);
            ocList.GetComponent<RectTransform>().sizeDelta += new Vector2(0, 114);
        }

        private void OnAvatarToggleClick(bool isOn)
        {
            if (isOn)
            {
                ocList.Init(true);
                _curSkinType = SkinType.Avatar;
                AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
            }
        }
        
        private void OnPetToggleClick(bool isOn)
        {
            if (isOn)
            {
                ocList.Init(false);
                _curSkinType = SkinType.Pet;
                AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
            }
        }

        public void OnOcSelect(OcServerData ocInfo)
        {

        }

        private void LateUpdate()
        {
            sureButton.interactable = ocList.Selected != null;
        }

        public void OnSureClick()
        {
            if (ocList.Selected == null) 
                return;

            BaseAvatarData avatarData = null;
            var curSkinType = (SkinType)ocList.Selected.ocInfo.skinType;
            switch (curSkinType)
            {
                case SkinType.Pet:
                    avatarData = PetData.DeserializeObject(ocList.Selected.ocInfo.avatarJson);
                    if (scene == OcChangeScene.Lobby || scene == OcChangeScene.Play)
                    {
                        AccountDataManager.Inst.SyncPetAvatarData((PetData)avatarData, isSuc =>
                        {
                            if (isSuc)
                            {
                                AvatarDataManager.Inst.SelfPetData = (PetData)avatarData;
                            }
                        });
                    }
                    break;

                case SkinType.Avatar:
                default:
                    avatarData = CharacterData.DeserializeObject(ocList.Selected.ocInfo.avatarJson);
                    CharacterData dataInfo = (CharacterData)avatarData;
                    var shapeData = ShapeDataMgr.Inst.GetShapeData(dataInfo.bodyType);
                    if(shapeData?.SaleType == ShapeSaleType.Vip && !VipDataManager.Inst.isVip)
                    {
                        UIAgentManager.Inst.OpenPanel(PanelId.CommonSingleConfirmPanel_Style2, new CommonSingleConfirmPanel_Style2Data()
                        {
                            CanClose = true,
                            ConfirmString = "确定",
                            ContextString = "正在使用vip体型，请开通vip后再试！",
                            TopTitleString = "提示",
                        });
                        return;
                    }
                    if (scene == OcChangeScene.Lobby || scene == OcChangeScene.Play)
                    {
                        AccountDataManager.Inst.SyncAvatarData(dataInfo, isSuc =>
                        {
                            if (isSuc)
                            {
                                AvatarDataManager.Inst.SelfCharacterData = dataInfo;
                            }
                        });
                    }
                    break;
            }


            if (scene == OcChangeScene.Lobby)
            {
                var gameHallPanel = UIManager.Inst.FindPanel<GameHallPanel>(WindowId.GameHallWindow, PanelId.GameHallPanel);
                switch (curSkinType)
                {
                    case SkinType.Pet:
                            gameHallPanel.PlayPetChangeOcAni();
                        break;
                    
                    case SkinType.Avatar:
                        default:
                            gameHallPanel.PlayChangeOcAni();
                        break;
                }
            }
            else
            {
                if (scene == OcChangeScene.EditNpc)
                {
                    OnChooseAvatarAction?.Invoke(avatarData);
                }
            }
            CloseSelf();
            OnCloseAction?.Invoke(avatarData);
        }

        public void OnJumpClick()
        {
            FittingRoomPanel fittingRoom;
            switch (_curSkinType)
            {
                case SkinType.Pet:
                    fittingRoom = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel, true) as FittingRoomPanel;
                    break;
                
                default:
                case SkinType.Avatar:
                    fittingRoom = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
                    break;
            }
            
            fittingRoom?.JumpTo(MainTabs.Tab.Bag, (int)OtherClass.Oc);
            fittingRoom.OnCloseAction = RefreshList;
        }

        public void OnCloseClick()
        {
            CloseSelf();
            BaseAvatarData data = _curSkinType == SkinType.Avatar
                ? AccountDataManager.Inst.UserInfo.avatarInfo
                : AccountDataManager.Inst.PetInfo.avatarInfo;
            OnCloseAction?.Invoke(data);
        }

        public void RefreshList(CharacterData data)
        {
            ocList?.RefreshList();
        }
    }
}
