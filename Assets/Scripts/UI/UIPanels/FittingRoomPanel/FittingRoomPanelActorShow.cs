//using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.Avatar;
using Game.COSXML;
using Game.Utils;
using GameData;
using GameData.BaseInfo;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace UI.UIPanels.FittingRoom
{
      
    public partial class FittingRoomPanel
    {
        [Header("前往截图")]
        [SerializeField] internal LoadingButton GoScreenshotBtn;//去截图
        [Header("保存到衣柜")]
        [SerializeField] internal LoadingButton SaveWardrobeBtn;//保存到衣柜

        private bool _hasTryOnUnowned = false;   // 衣服/道具/特效等试穿了未拥有的
        private bool _hasEmoteUnowned = false;   // 动作预览了未拥有的
        private bool _goScreenshotBtnEnabled = false;
        private bool _saveWardrobeBtnEnabled = false;
        private string _pendingClothesName;
        private WardrobeViewWardrobelistItem _pendingWardrobeItem;

        public bool IsActorShowMode => _goScreenshotBtnEnabled || _saveWardrobeBtnEnabled;

        public void RefreshActorShowBtns()
        {
            bool hasUnowned = _hasTryOnUnowned || _hasEmoteUnowned;
            if (GoScreenshotBtn != null)
                GoScreenshotBtn.gameObject.SetActive(_goScreenshotBtnEnabled && !hasUnowned);
            if (SaveWardrobeBtn != null)
                SaveWardrobeBtn.gameObject.SetActive(_saveWardrobeBtnEnabled && !hasUnowned);
        }

        internal void ResetActorShowBtnFlags()
        {
            _hasTryOnUnowned = false;
            _hasEmoteUnowned = false;
            _goScreenshotBtnEnabled = false;
            _saveWardrobeBtnEnabled = false;
            if (GoScreenshotBtn != null) GoScreenshotBtn.gameObject.SetActive(false);
            if (SaveWardrobeBtn != null) SaveWardrobeBtn.gameObject.SetActive(false);
            classList?.SetPermanentHiddenIds(null);
            if (_pendingWardrobeItem != null) _pendingWardrobeItem.HideLoading();
            _pendingWardrobeItem = null;
        }

        // 210002 = ResourceType.Theatre + UgcTheatreSubType.Theatre (UGC tab)
        // 210001 = ResourceType.Theatre + UgcTheatreSubType.AvatarCard (Bag tab)
        // 200001 = ResourceType.AvatarCard + UgcTheatreSubType.AvatarCard (both tabs)
        private static readonly HashSet<int> ActorEditorHiddenClassIds = new() { 210002, 210001, 200001 };

        private void ActorCharacterOpenFittingRoomPanel(CharacterViewExpressionItem expressionItem)
        {
            if (GoScreenshotBtn == null) return; // 防止消息在 Panel 销毁后触发
            saveOcButton?.gameObject.SetActive(false);
            _goScreenshotBtnEnabled = true;
            classList?.SetPermanentHiddenIds(ActorEditorHiddenClassIds);
            RefreshActorShowBtns();
            GoScreenshotBtn.onClick.RemoveAllListeners();
            GoScreenshotBtn.onClick.AddListener((() =>
            {
                UIManager.Inst.OpenPanel<TheatreActorCharacterEditorPanel>(PanelId.TheatreActorCharacterEditorPanel,expressionItem , avatarWrapper);
            }));
            

        }

        private void ActorWardrobeViewOpenFittingRoomPanel(WardrobeViewWardrobelistItem wItem)
        {
            _pendingWardrobeItem = wItem; // 记录当前 item，以便取消时能隐藏 loading
            saveOcButton?.gameObject.SetActive(false);
            _saveWardrobeBtnEnabled = true;
            classList?.SetPermanentHiddenIds(ActorEditorHiddenClassIds);
            RefreshActorShowBtns();

            SaveWardrobeBtn.onClick.RemoveAllListeners();
            SaveWardrobeBtn.onClick.AddListener(() =>
            {
                bool isReplace = wItem != null && wItem.ClothesData != null;
                if (isReplace)
                {
                    wItem.ShowLoading();
                    // 替换：直接用原名保存，无需弹命名框
                    _pendingClothesName = wItem.ClothesData.clothesName;
                    _pendingWardrobeItem = wItem;
                    StartWardrobePhoto();
                }
                else
                {
                    // 新增：弹命名框
                    UIManager.Inst.OpenPanel(PanelId.CommonSetNamePanel, new CommonSetNamePanelData
                    {
                        title = "保存到衣柜",
                        inputTxt = "请输入衣服名称",
                        maxLength = 7,
                        btn_close_action = _ => UIManager.Inst.ClosePanel(PanelId.CommonSetNamePanel),
                        btn_ok_action = txt =>
                        {
                            if (string.IsNullOrEmpty(txt))
                            {
                                TipPanel.ShowToast("名字不能为空！");
                                return;
                            }
                            if (OCTheatreActorEditorDataManager.Inst != null)
                            {
                                var list = OCTheatreActorEditorDataManager.Inst.GetClothesList();
                                if (list.Exists(c => c.clothesName == txt))
                                {
                                    TipPanel.ShowToast("已存在相同名字的衣服！");
                                    return;
                                }
                            }
                            _pendingClothesName = txt;
                            _pendingWardrobeItem = null;
                            UIManager.Inst.ClosePanel(PanelId.CommonSetNamePanel);
                            StartWardrobePhoto();
                        }
                    });
                }
            });
        }

        private void StartWardrobePhoto()
        {
            photoCamera = Loader.Load<GameObject>("Assets/Arts/Prefabs/CharacterUICamera.prefab")
                .Instantiate(avatarWrapper.Avatar.transform).GetComponent<Camera>();
            var camera_pos = new Vector3(0, isCharacterFittingRoom ? 0.5f : 0.35f, 1);
            float scaleX = avatarWrapper.Avatar.transform.localScale.x;
            if (isCharacterFittingRoom && saveAvatarData is CharacterData characterData)
            {
                switch ((CustomBodyTypeController.BodyType)characterData.bodyType)
                {
                    case CustomBodyTypeController.BodyType.Type2:
                        camera_pos.y = 0.6f;
                        scaleX = 1f;
                        break;
                    case CustomBodyTypeController.BodyType.Type3:
                        camera_pos.y = 0.45f;
                        scaleX = 1f;
                        break;
                    case CustomBodyTypeController.BodyType.Type4:
                        camera_pos.y = 0.35f;
                        scaleX = 1f;
                        break;
                    case CustomBodyTypeController.BodyType.Type6:
                        camera_pos.y = 0.75f;
                        scaleX = 1f;
                        break;
                }
            }
            photoCamera.transform.localPosition = camera_pos;
            photoCamera.transform.localEulerAngles = new Vector3(0, 180, 0);
            photoCamera.orthographicSize = photoCamera.orthographicSize * ResolutionAutoFit.CameraScale * scaleX;
            StartCoroutine(WardrobeInfoViewPhoto());
        }

        private IEnumerator WardrobeInfoViewPhoto()
        {
            yield return new WaitForEndOfFrame();

            Rect rect = GetScreenShotRect();
            byte[] imgBytes = ScreenShotUtils.TakeShotGamma(photoCamera, rect);
            Destroy(photoCamera.gameObject);

            string fileName = LocalDataUtils.Inst.SaveImgRes(imgBytes);
            var uri = $"OCTheatre/wardrobeInfo/{AccountDataManager.Inst.Uid}/{Path.GetFileName(fileName)}";
            CosXmlUploadManager.UploadFile(uri, fileName, (url, err) =>
            {
                File.Delete(fileName);
                if (!string.IsNullOrEmpty(err))
                {
                    LoggerUtils.LogError($"Upload Character Image Fail!!! Err : {err}");
                    return;
                }
                SaveClothesToWardrobe(_pendingClothesName, url);
            });
        }

        private void SaveClothesToWardrobe(string clothesName, string url)
        {
            if (OCTheatreActorEditorDataManager.Inst == null) return;
            var currentData = (avatarWrapper as CharacterWrap)?.ChaData ?? saveAvatarData as CharacterData;

            if (_pendingWardrobeItem != null && _pendingWardrobeItem.ClothesData != null)
            {
                // 替换已有衣服，保留 clothesIndex 和 isDef
                var clothes = _pendingWardrobeItem.ClothesData;
                clothes.clothesName = clothesName;
                clothes.clothesJson = CharacterData.SerializeObject(currentData);
                clothes.clothesURL = url;
                OCTheatreActorEditorDataManager.Inst.UpdateClothes(clothes);
            }
            else
            {
                // 新增衣服
                var clothes = new OTCAvatarClothes
                {
                    clothesIndex = OCTheatreActorEditorDataManager.Inst.GetClothesList().Count,
                    clothesName = clothesName,
                    clothesJson = CharacterData.SerializeObject(currentData),
                    clothesURL = url
                };
                OCTheatreActorEditorDataManager.Inst.AddClothes(clothes);
                MessageHelper.Broadcast(MessageName.ActorWardrobeItemAdded, clothes);
            }

            _pendingWardrobeItem = null;
            UIManager.Inst.ClosePanel(PanelId.FittingRoomPanel);
        }
    }

}
