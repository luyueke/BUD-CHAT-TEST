using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Basic
{
    public class GlobalCameraManager : GlobalInstance<GlobalCameraManager>
    {
        private Camera _uiCamera;

        public Camera UICamera
        {
            get
            {
                if (_uiCamera == null)
                {
                    _uiCamera = GameObject.Find("UICamera").GetComponent<Camera>();
                }

                return _uiCamera;
            }
        }

        #region Galobal Main Camera

        private Camera _globalMainCamera;

        public Camera GlobalMainCamera
        {
            get
            {
                if (_globalMainCamera == null)
                {
                    _globalMainCamera = GameObject.Find("GlobalMainCamera").GetComponent<Camera>();
                }

                return _globalMainCamera;
            }
        }

        private UniversalAdditionalCameraData GlobalMainCameraData =>
            GlobalMainCamera.GetComponent<UniversalAdditionalCameraData>();

        private List<Camera> GlobalMainCameraList => GlobalMainCameraData.cameraStack;

        #endregion

        private void LoadGlobalCameraPrefab()
        {
            var go = Loader.Load<GameObject>("Assets/Arts/Prefabs/GlobalMainCamera.prefab").RetainAsset();
            var camGo = GameObject.Instantiate(go);
            _globalMainCamera = camGo.GetComponent<Camera>();
            _globalMainCamera.gameObject.name = "GlobalMainCamera";
            _globalMainCamera.gameObject.DontDestroy();
            GlobalMainCameraData.renderType = CameraRenderType.Base;
        }

        public void Init()
        {
            LoadGlobalCameraPrefab();
            GlobalMainCamera.gameObject.DontDestroy();
            GlobalMainCameraData.renderType = CameraRenderType.Base;
            GlobalMainCameraList.Clear();
            GlobalMainCameraList.Add(UICamera);
        }

        public override void Release()
        {
            base.Release();
        }

        #region GlobalMainCamera Stack

        private void RefreshMainCamera()
        {
            GlobalMainCamera.enabled = false;
            GlobalMainCamera.enabled = true;
        }

        public void Insert(int index, Camera cam)
        {
            if (!GlobalMainCameraList.Contains(cam))
            {
                GlobalMainCameraList.Insert(index, cam);
            }

            RefreshMainCamera();
        }

        public void InsertFirst(Camera cam)
        {
            if (GlobalMainCameraList.Contains(cam))
            {
                GlobalMainCameraList.Remove(cam);
            }
            GlobalMainCameraList.Insert(0, cam);

            RefreshMainCamera();
        }

        public void InsertLast(Camera cam)
        {
            if (!GlobalMainCameraList.Contains(cam))
            {
                GlobalMainCameraList.Add(cam);
            }
           
            RefreshMainCamera();
        }

        public void Remove(Camera cam)
        {
            if (GlobalMainCameraList.Contains(cam))
            {
                GlobalMainCameraList.Remove(cam);
            }
            RefreshMainCamera();
        }

        /// <summary>
        /// 获取指定摄像机在摄像机堆栈中的索引位置。
        /// 若摄像机不在堆栈中，返回 -1。
        /// </summary>
        /// <param name="cam">要查询的摄像机</param>
        /// <returns>摄像机在堆栈中的索引，不存在则返回 -1</returns>
        public int IndexOf(Camera cam)
        {
            return GlobalMainCameraList.IndexOf(cam);
        }

        #endregion


        #region 相机聚焦

        
        private Vector3 lastPosition;
        private Vector3 lastRotation;
        
        private Vector3 followOffset = new Vector3(4, 10, -10);
        private bool isLookAt = false;

        public void LookAt(Transform target, Bounds bounding, CanvasScaler canvasScaler, float bgWight)
        {
            var cinemachineBrain = GlobalMainCamera.GetComponent<CinemachineBrain>();
            cinemachineBrain.enabled = false;
            var vCam = cinemachineBrain.ActiveVirtualCamera;
            if (vCam != null)
            {
                if (!isLookAt)
                {
                    lastPosition = GlobalMainCamera.transform.position;
                    lastRotation = GlobalMainCamera.transform.eulerAngles;
                }
                int offsetValue = 2;
                if (bounding.size.x > bounding.size.y)
                {
                    followOffset = new Vector3(bounding.size.x / offsetValue, 0, -bounding.size.x * offsetValue);
                }
                else
                {
                    followOffset = new Vector3(bounding.size.y / offsetValue, 0, -bounding.size.y * offsetValue);
                }
                float canvspri = (canvasScaler.referenceResolution.x - bgWight) / bgWight;
                var forwardOffset = target.forward.normalized * followOffset.z;
                var rightOffset = target.right.normalized * canvspri * followOffset.x;
                GlobalMainCamera.transform.DOMove(target.position + forwardOffset + rightOffset, 0.6f).OnUpdate(() =>
                {
                    GlobalMainCamera.transform.LookAt(target.position + rightOffset);
                });

                isLookAt = true;
            }
        }


        public void LookAt(Transform target, CanvasScaler canvasScaler, float bgWight)
        {
            LookAt(target, DataUtil.CalculateBoundingBox(target), canvasScaler, bgWight);

        }

        public void ReFollow()
        {
            isLookAt = false;
   
            GlobalMainCamera.transform.DOMove(lastPosition, 0.6f);
            GlobalMainCamera.transform.DORotate(lastRotation, 0.6f).OnComplete(() =>
            {
                var cinemachineBrain = GlobalMainCamera.GetComponent<CinemachineBrain>();
                cinemachineBrain.enabled = true;
            });
        }

        private AudioListener _cameraAudioListener;
        public void AddCameraAudioListener(){
            if (_cameraAudioListener == null)
            {
                _cameraAudioListener = GlobalMainCamera.gameObject.GetOrAddComponent<AudioListener>();
            }
        }

        public AudioListener GetCameraAudioListener(){
            return _cameraAudioListener;
        }

        public void RemoveCameraAudioListener(){
            if (_cameraAudioListener != null)
            {
                GameObject.Destroy(_cameraAudioListener);
                _cameraAudioListener = null;
            }
        }


        #endregion
        
        
        
    }
}