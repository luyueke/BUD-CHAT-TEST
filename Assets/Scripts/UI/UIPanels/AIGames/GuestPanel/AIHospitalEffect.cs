using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIHospitalEffect : MonoBehaviour
{
    public Transform targetBone; // 要跟随的骨骼
    public float offsetY = 0.05f;
    private Vector3 offset;
    private Vector3 _tempPosition;

    void Start()
    {
        // 记录初始偏移
        //offset = transform.position - targetBone.position;
    }

    void LateUpdate()
    {
        // 跟随骨骼位置
        if (targetBone != null)
        {
            transform.position = targetBone.position + offset;
            //todo 想要固定position 的 Y坐标为0.05f
            _tempPosition = transform.position;
            _tempPosition.y = offsetY;
            transform.position = _tempPosition;
        }
    }
}
