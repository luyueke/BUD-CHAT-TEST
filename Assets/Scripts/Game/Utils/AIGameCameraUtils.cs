using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

namespace AIGame.Base
{
    /// <summary>
    /// 协程管理器，用于处理相机过渡动画
    /// </summary>
    public class CameraCoroutineManager : MonoBehaviour
    {
        private static CameraCoroutineManager _instance;
        public static CameraCoroutineManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("CameraCoroutineManager");
                    _instance = go.AddComponent<CameraCoroutineManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public void StartSmoothCameraTransition(Vector3 targetPosition, Vector3 targetForward, float duration, float distance, System.Action onComplete = null)
        {
            StartCoroutine(SmoothCameraTransition(targetPosition, targetForward, duration, distance, onComplete));
        }

        public void StartSmoothCameraTransitionWithOffset(Vector3 targetPosition, Vector3 offset, float duration, System.Action onComplete = null)
        {
            StartCoroutine(SmoothCameraTransitionWithOffset(targetPosition, offset, duration, onComplete));
        }

        public void StartOrbitAnimation(Vector3 centerPosition, float radius, float startAngle, float endAngle, float duration, float height = 0, System.Action onComplete = null)
        {
            StartCoroutine(OrbitAnimation(centerPosition, radius, startAngle, endAngle, duration, height, onComplete));
        }

        private IEnumerator SmoothCameraTransition(Vector3 targetPosition, Vector3 targetForward, float duration, float distance, System.Action onComplete)
        {
            CinemachineVirtualCamera moveCamera = AIGameCameraUtils.Inst.MoveCamera;
            
            // 计算目标相机位置和旋转
            Vector3 targetCameraPosition = targetPosition - targetForward.normalized * distance;
            Vector3 direction = (targetPosition - targetCameraPosition).normalized;
            Quaternion targetRotation = direction != Vector3.zero ? Quaternion.LookRotation(direction) : Quaternion.identity;
            
            // 记录起始位置和旋转
            Vector3 startPosition = moveCamera.transform.position;
            Quaternion startRotation = moveCamera.transform.rotation;
            
            // 清除LookAt和Follow，让相机保持在固定位置
            moveCamera.LookAt = null;
            moveCamera.Follow = null;
            
            float elapsedTime = 0f;
            
            // 使用缓动函数进行平滑过渡
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                
                // 使用缓动函数（EaseInOut）
                float easedT = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                
                // 平滑插值位置和旋转
                moveCamera.transform.position = Vector3.Lerp(startPosition, targetCameraPosition, easedT);
                moveCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, easedT);
                
                yield return null;
            }
            
            // 确保最终位置和旋转精确
            moveCamera.transform.position = targetCameraPosition;
            moveCamera.transform.rotation = targetRotation;
            
            // 调用完成回调
            onComplete?.Invoke();
        }

        private IEnumerator SmoothCameraTransitionWithOffset(Vector3 targetPosition, Vector3 offset, float duration, System.Action onComplete)
        {
            CinemachineVirtualCamera moveCamera = AIGameCameraUtils.Inst.MoveCamera;
            
            // 计算目标相机位置和旋转
            Vector3 targetCameraPosition = targetPosition + offset;
            Vector3 direction = (targetPosition - targetCameraPosition).normalized;
            Quaternion targetRotation = direction != Vector3.zero ? Quaternion.LookRotation(direction) : Quaternion.identity;
            
            // 记录起始位置和旋转
            Vector3 startPosition = moveCamera.transform.position;
            Quaternion startRotation = moveCamera.transform.rotation;
            
            // 清除LookAt和Follow，让相机保持在固定位置
            moveCamera.LookAt = null;
            moveCamera.Follow = null;
            
            float elapsedTime = 0f;
            
            // 使用缓动函数进行平滑过渡
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                
                // 使用缓动函数（EaseInOut）
                float easedT = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                
                // 平滑插值位置和旋转
                moveCamera.transform.position = Vector3.Lerp(startPosition, targetCameraPosition, easedT);
                moveCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, easedT);
                
                yield return null;
            }
            
            // 确保最终位置和旋转精确
            moveCamera.transform.position = targetCameraPosition;
            moveCamera.transform.rotation = targetRotation;
            
            // 调用完成回调
            onComplete?.Invoke();
        }

        private IEnumerator OrbitAnimation(Vector3 centerPosition, float radius, float startAngle, float endAngle, float duration, float height = 0, System.Action onComplete = null)
        {
            float startRad = startAngle * Mathf.Deg2Rad;
            float endRad = endAngle * Mathf.Deg2Rad;

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                
                float angle = Mathf.Lerp(startRad, endRad, t);
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;
                Vector3 cameraPosition = centerPosition + new Vector3(x, height, z);
                
                AIGameCameraUtils.Inst.MoveCamera.transform.position = cameraPosition;
                AIGameCameraUtils.Inst.MoveCamera.transform.LookAt(centerPosition);
                
                yield return null;
            }

            AIGameCameraUtils.Inst.MoveCamera.transform.position = centerPosition + new Vector3(Mathf.Sin(endRad) * radius, height, Mathf.Cos(endRad) * radius);
            AIGameCameraUtils.Inst.MoveCamera.transform.LookAt(centerPosition);
            
            onComplete?.Invoke();
        }
    }

    public class AIGameCameraUtils : GlobalInstance<AIGameCameraUtils>
    {
        private Camera _mainCamera;
        public Camera MainCamera
        {
            get
            {
                if (_mainCamera == null)
                {
                    _mainCamera = GameObject.Find("GlobalMainCamera").GetComponent<Camera>();
                }
                return _mainCamera;
            }
        }

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

        private CinemachineVirtualCamera _virCamera;
        public CinemachineVirtualCamera VirtualCamera
        {
            get
            {
                if (_virCamera == null)
                {
                    _virCamera = GameObject.Find("PlayVirtualCamera").GetComponent<CinemachineVirtualCamera>();
                }
                return _virCamera;
            }
        }
        private CinemachineVirtualCamera _moveCamera;
        public CinemachineVirtualCamera MoveCamera
        {
            get
            {
                if (_moveCamera == null)
                {
                    _moveCamera = new GameObject("MoveCam").AddComponent<CinemachineVirtualCamera>();
                    //z增加运镜用的虚拟相机比重
                    _moveCamera.Priority = 20;
                    CinemachineTransposer transposer = MoveCamera.AddCinemachineComponent<CinemachineTransposer>();
                    transposer.m_BindingMode = CinemachineTransposer.BindingMode.LockToTarget;
                    MoveCamera.AddCinemachineComponent<CinemachineSameAsFollowTarget>();
                }
                return _moveCamera;
            }
        }
        private CinemachineBrain _mianBrain;
        public CinemachineBrain MianBrain
        {
            get
            {
                if (_mianBrain == null)
                {
                    _mianBrain = MainCamera.GetComponent<CinemachineBrain>();
                }
                return _mianBrain;
            }
        }

        private CinemachineBrain _moveBrain;
        public CinemachineBrain MoveBrain
        {
            get
            {
                if (_moveBrain == null)
                {
                    _moveBrain = MoveCamera.GetComponent<CinemachineBrain>();
                }
                return _moveBrain;
            }
        }

        public void SetCamToPos(Transform targetPos,int time = 0,float offset = 0)
        {
            SetBrainTime(time);
            MoveCamera.gameObject.SetActive(true);
            MoveCamera.LookAt = targetPos;
            MoveCamera.Follow = targetPos;
            CinemachineTransposer transposer = MoveCamera.GetCinemachineComponent<CinemachineTransposer>();
            transposer.m_FollowOffset = new Vector3(0, 0, -offset);
        }

        public void SetCamToPos(Transform targetTrans, int time = 0, Vector3 offset = default) {
            SetBrainTime(time);
            MoveCamera.gameObject.SetActive(true);
            MoveCamera.LookAt = targetTrans;
            MoveCamera.Follow = targetTrans;
            CinemachineTransposer transposer = MoveCamera.GetCinemachineComponent<CinemachineTransposer>();
            transposer.m_FollowOffset = offset;
        }

        // public void SetCamToPos(Vector3 position, int time = 0, Vector3 offset = default) {
        //     SetBrainTime(time);
        //     MoveCamera.gameObject.SetActive(true);
            
        //     // 创建一个临时的目标Transform来指向指定位置
        //     GameObject tempTarget = new GameObject("TempCameraTarget");
        //     tempTarget.transform.position = position;
            
        //     // 设置相机朝向和跟随目标
        //     MoveCamera.LookAt = tempTarget.transform;
        //     MoveCamera.Follow = tempTarget.transform;
            
        //     // 设置偏移
        //     CinemachineTransposer transposer = MoveCamera.GetCinemachineComponent<CinemachineTransposer>();
        //     transposer.m_FollowOffset = offset;
            
        //     // 如果有过渡时间，使用协程管理器来延迟销毁
        //     if (time > 0)
        //     {
        //         // 使用MonoBehaviour来管理协程
        //         GameObject coroutineManager = new GameObject("CoroutineManager");
        //         CoroutineManager manager = coroutineManager.AddComponent<CoroutineManager>();
        //         manager.StartDestroyCoroutine(tempTarget, time);
        //     }
        //     else
        //     {
        //         // 立即销毁临时目标
        //         Object.DestroyImmediate(tempTarget);
        //     }
        // }
        
        /// <summary>
        /// 设置相机朝向指定位置（顺滑过渡版本）
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <param name="forward">相机朝向方向</param>
        /// <param name="time">过渡时间</param>
        /// <param name="distance">相机距离目标的距离</param>
        public void SetCamLookAt(Vector3 position, Vector3 forward, float time = 0, float distance = 2,Action onComplete = null)
        {
            // 如果时间为0，使用硬切
            if (time <= 0)
            {
                SetCamLookAtInstant(position, forward, distance);
                onComplete?.Invoke();
                return; 
            }

            // 使用协程管理器进行顺滑过渡
            MoveCamera.gameObject.SetActive(true);
            CameraCoroutineManager.Instance.StartSmoothCameraTransition(position, forward, time, distance,onComplete);
        }

        /// <summary>
        /// 设置相机朝向指定位置（硬切版本）
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <param name="forward">相机朝向方向</param>
        /// <param name="distance">相机距离目标的距离</param>
        private void SetCamLookAtInstant(Vector3 position, Vector3 forward, float distance = 2)
        {
            SetBrainTime(0);
            MoveCamera.gameObject.SetActive(true);
            
            // 计算相机位置：目标位置 + 朝向方向的反方向 * 距离
            Vector3 cameraPosition = position - forward.normalized * distance;
            
            // 设置相机位置
            MoveCamera.transform.position = cameraPosition;
            
            // 设置相机朝向目标位置
            Vector3 direction = (position - cameraPosition).normalized;
            if (direction != Vector3.zero)
            {
                MoveCamera.transform.rotation = Quaternion.LookRotation(direction);
            }
            
            // 清除LookAt和Follow，让相机保持在固定位置
            MoveCamera.LookAt = null;
            MoveCamera.Follow = null;
        }

        public void SetMoveCameraPosAndRotation(Vector3 position, Vector3 eulerAngles){
            SetBrainTime(0);
            MoveCamera.gameObject.SetActive(true);

            MoveCamera.transform.position = position;
            MoveCamera.transform.eulerAngles = eulerAngles;
            MoveCamera.LookAt = null;
            MoveCamera.Follow = null;
        }

  

        /// <summary>
        /// 设置相机朝向指定位置（带偏移）
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <param name="offset">相机偏移</param>
        /// <param name="time">过渡时间</param>
        public void SetCamLookAt(Vector3 position, Vector3 offset, int time = 0)
        {
            if (time <= 0)
            {
                // 硬切版本
                SetBrainTime(0);
                MoveCamera.gameObject.SetActive(true);
                
                // 计算相机位置：目标位置 + 偏移
                Vector3 cameraPosition = position + offset;
                
                // 设置相机位置
                MoveCamera.transform.position = cameraPosition;
                
                // 设置相机朝向目标位置
                Vector3 direction = (position - cameraPosition).normalized;
                if (direction != Vector3.zero)
                {
                    MoveCamera.transform.rotation = Quaternion.LookRotation(direction);
                }
                
                // 清除LookAt和Follow，让相机保持在固定位置
                MoveCamera.LookAt = null;
                MoveCamera.Follow = null;
            }
            else
            {
                // 使用协程管理器进行顺滑过渡
                CameraCoroutineManager.Instance.StartSmoothCameraTransitionWithOffset(position, offset, time);
            }
        }


        /// <summary>
        /// 设置相机朝向指定位置（带偏移和完成回调）
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <param name="offset">相机偏移</param>
        /// <param name="time">过渡时间</param>
        /// <param name="onComplete">完成回调</param>
        public void SetCamLookAt2(Vector3 position, Vector3 offset, int time, System.Action onComplete)
        {
            if (time <= 0)
            {
                // 硬切版本
                SetBrainTime(0);
                MoveCamera.gameObject.SetActive(true);
                
                // 计算相机位置：目标位置 + 偏移
                Vector3 cameraPosition = position + offset;
                
                // 设置相机位置
                MoveCamera.transform.position = cameraPosition;
                
                // 设置相机朝向目标位置
                Vector3 direction = (position - cameraPosition).normalized;
                if (direction != Vector3.zero)
                {
                    MoveCamera.transform.rotation = Quaternion.LookRotation(direction);
                }
                
                // 清除LookAt和Follow，让相机保持在固定位置
                MoveCamera.LookAt = null;
                MoveCamera.Follow = null;
                
                onComplete?.Invoke();
            }
            else
            {
                // 使用协程管理器进行顺滑过渡
                CameraCoroutineManager.Instance.StartSmoothCameraTransitionWithOffset(position, offset, time, onComplete);
            }
        }

        /// <summary>
        /// 相机环绕目标位置
        /// </summary>
        /// <param name="centerPosition">环绕中心位置</param>
        /// <param name="radius">环绕半径</param>
        /// <param name="startAngle">起始角度（度）</param>
        /// <param name="endAngle">结束角度（度）</param>
        /// <param name="time">过渡时间</param>
        /// <param name="height">相机高度偏移</param>
        public void SetCamOrbit(Vector3 centerPosition, float radius, float startAngle, float endAngle, int time, float height = 0)
        {
            if (time <= 0)
            {
                // 硬切到结束位置
                float endRad = endAngle * Mathf.Deg2Rad;
                Vector3 cameraPosition = centerPosition + new Vector3(
                    Mathf.Sin(endRad) * radius,
                    height,
                    Mathf.Cos(endRad) * radius
                );
                
                SetCamLookAtInstant(centerPosition, (centerPosition - cameraPosition).normalized, radius);
            }
            else
            {
                // 使用协程管理器进行环绕动画
                CameraCoroutineManager.Instance.StartOrbitAnimation(centerPosition, radius, startAngle, endAngle, time, height);
            }
        }

        /// <summary>
        /// 相机环绕目标位置（带完成回调）
        /// </summary>
        /// <param name="centerPosition">环绕中心位置</param>
        /// <param name="radius">环绕半径</param>
        /// <param name="startAngle">起始角度（度）</param>
        /// <param name="endAngle">结束角度（度）</param>
        /// <param name="time">过渡时间</param>
        /// <param name="height">相机高度偏移</param>
        /// <param name="onComplete">完成回调</param>
        public void SetCamOrbit(Vector3 centerPosition, float radius, float startAngle, float endAngle, int time, float height, System.Action onComplete)
        {
            if (time <= 0)
            {
                // 硬切到结束位置
                float endRad = endAngle * Mathf.Deg2Rad;
                Vector3 cameraPosition = centerPosition + new Vector3(
                    Mathf.Sin(endRad) * radius,
                    height,
                    Mathf.Cos(endRad) * radius
                );
                
                SetCamLookAtInstant(centerPosition, (centerPosition - cameraPosition).normalized, radius);
                onComplete?.Invoke();
            }
            else
            {
                // 使用协程管理器进行环绕动画
                CameraCoroutineManager.Instance.StartOrbitAnimation(centerPosition, radius, startAngle, endAngle, time, height, onComplete);
            }
        }

        public void AddLayer(params LayerMask[] masks) {
            if (masks == null || masks.Length == 0) return;
            foreach (var mask in masks)
            {
                MainCamera.cullingMask |= mask;
            }
        }

        public void RemoveLayer(params LayerMask[] masks) {
            if (masks == null || masks.Length == 0) return;
            foreach (var mask in masks)
            {
                MainCamera.cullingMask &= ~mask;
            }
        }


        public void SetCamFOV(float value)
        {
            MoveCamera.m_Lens.FieldOfView = value;
        }

        public void BackToPlayer(float time = 0)
        {
            AddLayer(LayerMask.GetMask("Player"), LayerMask.GetMask("Head"));
            if (_moveCamera == null)
            {
                return;
            }
            SetBrainTime(time);
            MoveCamera.gameObject.SetActive(false);
        }

        public void BackToPlayerFromMoveCam(float time = 0)
        {
            VirtualCamera.gameObject.transform.position = MoveCamera.transform.position;
            VirtualCamera.gameObject.transform.rotation = MoveCamera.transform.rotation;
            BackToPlayer(0);
        }

        private void SetBrainTime(float time)
        {
            if (time <=0)
            {
                MianBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut;
            }
            else
            {
                MianBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.EaseIn;
                MianBrain.m_DefaultBlend.m_Time = time;
            }
        }

        /// <summary>
        /// 使用示例：
        /// 
        /// // 1. 相机朝向指定位置，默认距离2米，硬切
        /// AIGameCameraUtils.Inst.SetCamLookAt(new Vector3(10, 0, 5));
        /// 
        /// // 2. 相机朝向指定位置，距离5米，2秒顺滑过渡
        /// AIGameCameraUtils.Inst.SetCamLookAt(new Vector3(10, 0, 5), 2, 5f);
        /// 
        /// // 3. 相机朝向指定位置，指定朝向方向，1秒过渡
        /// AIGameCameraUtils.Inst.SetCamLookAt(new Vector3(10, 0, 5), Vector3.up, 1, 3f);
        /// 
        /// // 4. 相机朝向指定位置，带偏移，1秒过渡
        /// AIGameCameraUtils.Inst.SetCamLookAt(new Vector3(10, 0, 5), new Vector3(0, 2, -3), 1);
        /// 
        /// // 5. 相机环绕目标位置，从0度到180度，3秒过渡
        /// AIGameCameraUtils.Inst.SetCamOrbit(new Vector3(0, 0, 0), 5f, 0f, 180f, 3, 2f);
        /// 
        /// // 6. 带完成回调的相机移动
        /// AIGameCameraUtils.Inst.SetCamLookAt(new Vector3(10, 0, 5), Vector3.forward, 2, 3f, () => {
        ///     Debug.Log("相机移动完成！");
        /// });
        /// 
        /// // 7. 返回玩家视角
        /// AIGameCameraUtils.Inst.BackToPlayer(1);
        /// </summary>
    }
}
