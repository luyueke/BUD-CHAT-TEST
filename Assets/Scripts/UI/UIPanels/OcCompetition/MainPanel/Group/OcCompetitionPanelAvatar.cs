using BUD.AnimPose;
using Game.Avatar;
using Game.COSXML;
using Game.Utils;
using GameData;
using GameData.BaseInfo;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class OcCompetitionPanelAvatar : MonoBehaviour
    {
        public PictureSelectLoad PicUpLoad;
        public List<GameObject> ShotHide;

        public AvatarCameraController AvatarCameraController;
        public RawImage RawImage;
        public GameObject ModelRoot; // 用于放置角色模型的根节点

        public Text NoneTxt;

        private RenderTexture RenderTexture;
        private CharacterWrap SelfWrap;
        private AnimIKController AnimationCtrlIK;

        private CharacterWrap OtherWrap;
        private AnimIKController OtherAnimationCtrlIK;

        OcCompetitionSystemData data => OcCompetitionSystem.Inst.data;
        [HideInInspector] public OcCompetitionPanelMy Root;

        [HideInInspector] public PoseInfo PoseInfo;
        [HideInInspector] public OcInfo OcInfo;
        [HideInInspector] public string CoverUrl;

        [HideInInspector] public int setType; //1：为更新，0：为参赛
        [HideInInspector] public string oldCreation;
        public void Init(OcCompetitionPanelMy root)
        {
            Root = root;

            RenderTexture = new RenderTexture(500, 500, 24);
            RawImage.texture = RenderTexture;
            AvatarCameraController.roleCamera.targetTexture = RenderTexture;
        }

        private void OnEnable()
        {
            if (OcInfo == null && PoseInfo == null)
            {
                NoneTxt.gameObject.SetActiveValid(true);
                PicUpLoad.SetData("");
            }
        }

        private void OnDestroy()
        {
            if (RenderTexture != null)
            {
                RenderTexture.Release();
                GameObject.Destroy(RenderTexture);
                RenderTexture = null;
            }
        }

        public void ShowAvatar(bool bo) {
            if (SelfWrap != null && SelfWrap.Avatar != null)
            {
                SelfWrap.Avatar.gameObject.SetActive(bo);
            }
        }

        public void Clear() {
            setType = 0;
            PoseInfo = null;
            OcInfo = null;
            NoneTxt.gameObject.SetActiveValid(true);
            if (SelfWrap != null && SelfWrap.Avatar != null)
            {
                GameObject.Destroy(SelfWrap.Avatar.gameObject);
            }
            SelfWrap = null;
        }

        public void RefreshAvatar(OcServerData item)
        {
            RefreshAvatar(item.ocInfo);
            NoneTxt.gameObject.SetActive(false);
        }
        public void RefreshAvatar(OcInfo ocInfo)
        {
            NoneTxt.gameObject.SetActive(false);
            OcInfo = ocInfo;    
            var _data = CharacterData.DeserializeObject(ocInfo.avatarJson);
            if (SelfWrap != null && SelfWrap.Avatar != null)
            {
                GameObject.Destroy(SelfWrap.Avatar.gameObject);
            }
            SelfWrap = AvatarController.Inst.CreateUIAvatarWithIKController(_data, ModelRoot.transform);
            //SelfWrap.SetParent(ModelRoot.transform, true);
            //SelfWrap.Avatar.transform.localPosition = new Vector3(0, -50, -400);
            //SelfWrap.Avatar.transform.localScale = Vector3.one * 100;
            AnimationCtrlIK = SelfWrap.Avatar.GetComponent<AnimIKController>();
            AvatarCameraController.RotateTarget = SelfWrap.Avatar.gameObject.transform;

            if (OtherWrap == null)
            {
                OtherWrap = AvatarController.Inst.CreateUIAvatarWithIKController(AccountDataManager.Inst.UserInfo.otherAvatarInfo, ModelRoot.transform);
                OtherAnimationCtrlIK = OtherWrap.Avatar.GetComponent<AnimIKController>();
                OtherWrap.Avatar.gameObject.SetActive(false);
            }
        }

        public void RefreshAvatar(PoseInfo poseInfo)
        {
            NoneTxt.gameObject.SetActive(false);
            PoseInfo = poseInfo;
            AnimationCtrlIK.Pose(poseInfo, OtherAnimationCtrlIK);
            //avatarCameraController.SetEmoteView((UgcAnimSubType)poseInfo.UgcInfo.poseInfo.poseType);
            //var tempFrameData = JsonConvert.DeserializeObject<KeyFrameData>(poseData);
            //Creater.SetKeyFrameData(curKeyFrameData);
            //GizmoManager.Inst.CurGizmoCtrl?.DisableGizmo();
            //handleTool.gameObject.SetActive(false);
        }

        public void Sure() {
            if (OcInfo == null || PoseInfo == null) 
            {
                TipPanel.ShowToast("请完成设子和姿势的选择");
                return;
            }
            Shot((url) =>
            {
                OcCompetitionSystem.Inst.Join(OcInfo,
                                              PoseInfo,
                                              url,
                                              () => { Root.SetStep(OcCompetitionStep.All); },
                                              setType,
                                              oldCreation);
            });

        }

        [Button("截图")]
        public void Shot(Action<string> ac = null) {
            foreach (var item in ShotHide)
            {
                item.gameObject.SetActive(false);
            }
            StartCoroutine(CaptureScreenArea(ac));
        }

        public IEnumerator CaptureScreenArea(Action<string> ac = null)
        {
            RectTransform uiTransform = RawImage.transform as RectTransform;
            Vector3[] corners = new Vector3[4];
            uiTransform.GetWorldCorners(corners);

            // 转换为屏幕坐标
            Camera canvasCamera = UIManager.Inst.Canvas.worldCamera;
            Vector3 screenPos0 = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[0]);
            Vector3 screenPos2 = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[2]);

            Rect screenRect = new Rect(
                Mathf.Min(screenPos0.x, screenPos2.x),
                Mathf.Min(screenPos0.y, screenPos2.y),
                Mathf.Abs(screenPos2.x - screenPos0.x),
                Mathf.Abs(screenPos2.y - screenPos0.y)
            );

            yield return new WaitForEndOfFrame();

            Texture2D screenShot = new Texture2D((int)screenRect.width, (int)screenRect.height, TextureFormat.RGB24, false);
            screenShot.ReadPixels(screenRect, 0, 0);
            screenShot.Apply();

            byte[] imageBytes = screenShot.EncodeToPNG();

            Destroy(screenShot);

            string fileName = LocalDataUtils.Inst.SaveImgRes(imageBytes);

            //string filePath = System.IO.Path.Combine(Application.persistentDataPath, "screenshot.png");
            //System.IO.File.WriteAllBytes(filePath, imageBytes);

            var uri = $"OcCompetition/Cover/{AccountDataManager.Inst.Uid}/{Path.GetFileName(fileName)}";
            CosXmlUploadManager.UploadFile(uri, fileName, (url, err) =>
            {
                File.Delete(fileName);

                if (!string.IsNullOrEmpty(err))
                {
                    LoggerUtils.Log($"Upload Character Image Fail!!! Err : {err}");
                    return;
                }
                else
                {
                    LoggerUtils.Log("Upload Img Success url: " + url);

                    ac?.Invoke(url);
                }
            });
        }
    }
}