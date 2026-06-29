using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 载具车轮旋转控制 (直接控制 Transform)
/// </summary>
public class PGCWheelRotator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private List<Transform> wheels; // 需要旋转的车轮
    [SerializeField] private Vector3 rotateAxis = Vector3.right; // 旋转轴
    [SerializeField] private float maxRotateSpeed = 360f; // 最大旋转速度 (度/秒)
    [SerializeField] private float accelerationTime = 1.0f; // 加速时间
    [SerializeField] private float decelerationTime = 1.0f; // 减速时间

    private PGCVehicleAnimCtrl pgcVehicleAnimCtrl;
    private float currentSpeedRatio = 0f; // 0~1
    private float targetSpeedRatio = 0f;  // 0 or 1

    private void Start() {
        pgcVehicleAnimCtrl = GetComponentInParent<PGCVehicleAnimCtrl>();
        Init();
    }

    private void Init(){
        if(pgcVehicleAnimCtrl != null){
            pgcVehicleAnimCtrl.OnStateChange += OnStateChange;
        }
    }

    private void OnDestroy() {
        if(pgcVehicleAnimCtrl != null){
            pgcVehicleAnimCtrl.OnStateChange -= OnStateChange;
        }
    }

    private void OnStateChange(PGCVehicleAniState state){
        if(state == PGCVehicleAniState.Idle){
            targetSpeedRatio = 0f;
        }else if(state == PGCVehicleAniState.Run || state == PGCVehicleAniState.Jump || state == PGCVehicleAniState.Fall){
            targetSpeedRatio = 1f;
        }
    }

    private void Update()
    {
        // 1. 计算当前速度比例 (0~1)
        if (Mathf.Abs(currentSpeedRatio - targetSpeedRatio) > 0.001f)
        {
            float duration = (targetSpeedRatio > currentSpeedRatio) ? accelerationTime : decelerationTime;
            if (duration < 0.001f) duration = 0.001f;
            
            currentSpeedRatio = Mathf.MoveTowards(currentSpeedRatio, targetSpeedRatio, Time.deltaTime / duration);
        }
        else
        {
            currentSpeedRatio = targetSpeedRatio;
        }

        // 2. 执行旋转
        if (currentSpeedRatio > 0.001f && wheels != null)
        {
            float angleDelta = currentSpeedRatio * maxRotateSpeed * Time.deltaTime;
            
            for (int i = 0; i < wheels.Count; i++)
            {
                if (wheels[i] != null)
                {
                    wheels[i].Rotate(rotateAxis, angleDelta, Space.Self);
                }
            }
        }
    }
}
/// 简化版，性能消耗小
// public class PGCWheelListener : MonoBehaviour
// {
//     [SerializeField] private Animation anim;
//     private PGCVehicleAnimCtrl pgcVehicleAnimCtrl;

//     private void Start() {
//         pgcVehicleAnimCtrl = GetComponentInParent<PGCVehicleAnimCtrl>();
//         Init();
//     }

//     private void Init(){
//         if(pgcVehicleAnimCtrl != null){
//             pgcVehicleAnimCtrl.OnStateChange += OnStateChange;
//         }
//     }

//     private void OnStateChange(PGCVehicleAniState state){   
//         if(state == PGCVehicleAniState.Idle && anim.isPlaying){
//             anim.Stop();
//         }else if((state == PGCVehicleAniState.Run || state == PGCVehicleAniState.Jump || state == PGCVehicleAniState.Fall) && !anim.isPlaying){
//             anim.Play();
//         }   
//     }   
// }
