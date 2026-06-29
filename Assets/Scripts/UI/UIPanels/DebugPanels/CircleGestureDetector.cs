using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 画圈手势检测器
/// 用于在手机屏幕上检测画圈手势来唤起GM面板
/// </summary>
public class CircleGestureDetector : MonoBehaviour
{
    private List<Vector2> touchPoints = new List<Vector2>();
    private const float MIN_CIRCLE_RADIUS = 50f; // 最小圆圈半径（像素）
    private const float MAX_CIRCLE_RADIUS = 500f; // 最大圆圈半径（像素）
    private const int MIN_POINTS_FOR_CIRCLE = 20; // 形成圆圈所需的最少点数
    private const float CIRCLE_COMPLETION_THRESHOLD = 0.7f; // 圆圈完成度阈值（0-1）
    private const float MAX_TIME_FOR_CIRCLE = 2f; // 画圈的最大时间（秒）
    
    private float gestureStartTime;
    private bool isDetecting = false;
    private int currentTouchId = -1;

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        // 编辑器模式下使用鼠标模拟
        if (Input.GetMouseButtonDown(0))
        {
            StartGesture(Input.mousePosition, -1);
        }
        else if (Input.GetMouseButton(0) && isDetecting)
        {
            UpdateGesture(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isDetecting)
        {
            EndGesture();
        }
#else
        // 移动端使用触摸
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                StartGesture(touch.position, touch.fingerId);
            }
            else if (touch.phase == TouchPhase.Moved && isDetecting && touch.fingerId == currentTouchId)
            {
                UpdateGesture(touch.position);
            }
            else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && isDetecting && touch.fingerId == currentTouchId)
            {
                EndGesture();
            }
        }
        else if (Input.touchCount == 0 && isDetecting)
        {
            EndGesture();
        }
#endif
    }

    private void StartGesture(Vector2 position, int touchId)
    {
        touchPoints.Clear();
        touchPoints.Add(position);
        gestureStartTime = Time.time;
        isDetecting = true;
        currentTouchId = touchId;
    }

    private void UpdateGesture(Vector2 position)
    {
        if (!isDetecting)
            return;

        // 检查是否超时
        if (Time.time - gestureStartTime > MAX_TIME_FOR_CIRCLE)
        {
            ResetGesture();
            return;
        }

        // 只添加与上一个点距离足够的点，避免点太密集
        if (touchPoints.Count > 0)
        {
            float distance = Vector2.Distance(touchPoints[touchPoints.Count - 1], position);
            if (distance < 5f) // 最小移动距离
                return;
        }

        touchPoints.Add(position);

        // 如果点数足够，检查是否是圆圈
        if (touchPoints.Count >= MIN_POINTS_FOR_CIRCLE)
        {
            if (DetectCircle())
            {
                OnCircleDetected();
                ResetGesture();
            }
        }
    }

    private void EndGesture()
    {
        if (touchPoints.Count >= MIN_POINTS_FOR_CIRCLE)
        {
            if (DetectCircle())
            {
                OnCircleDetected();
            }
        }
        ResetGesture();
    }

    private void ResetGesture()
    {
        touchPoints.Clear();
        isDetecting = false;
        currentTouchId = -1;
    }

    /// <summary>
    /// 检测触摸点是否形成圆圈
    /// </summary>
    private bool DetectCircle()
    {
        if (touchPoints.Count < MIN_POINTS_FOR_CIRCLE)
            return false;

        // 计算中心点
        Vector2 center = Vector2.zero;
        foreach (Vector2 point in touchPoints)
        {
            center += point;
        }
        center /= touchPoints.Count;

        // 计算平均半径
        float totalRadius = 0f;
        foreach (Vector2 point in touchPoints)
        {
            totalRadius += Vector2.Distance(point, center);
        }
        float avgRadius = totalRadius / touchPoints.Count;

        // 检查半径是否在合理范围内
        if (avgRadius < MIN_CIRCLE_RADIUS || avgRadius > MAX_CIRCLE_RADIUS)
            return false;

        // 检查点是否围绕中心形成圆圈
        // 通过计算角度变化来判断
        int angleChanges = 0;
        float totalAngleChange = 0f;
        
        for (int i = 1; i < touchPoints.Count; i++)
        {
            Vector2 prevDir = (touchPoints[i - 1] - center).normalized;
            Vector2 currDir = (touchPoints[i] - center).normalized;
            
            float angle = Vector2.SignedAngle(prevDir, currDir);
            if (Mathf.Abs(angle) > 5f) // 忽略微小角度变化
            {
                angleChanges++;
                totalAngleChange += angle;
            }
        }

        // 检查总角度变化是否接近360度（允许一定误差）
        float angleChangeDegrees = Mathf.Abs(totalAngleChange);
        float circleCompletion = angleChangeDegrees / 360f;

        // 检查半径变化是否在合理范围内（圆圈应该相对均匀）
        float radiusVariance = 0f;
        foreach (Vector2 point in touchPoints)
        {
            float radius = Vector2.Distance(point, center);
            radiusVariance += Mathf.Abs(radius - avgRadius);
        }
        radiusVariance /= touchPoints.Count;
        float radiusVarianceRatio = radiusVariance / avgRadius;

        // 如果角度变化接近360度且半径变化不大，认为是圆圈
        return circleCompletion >= CIRCLE_COMPLETION_THRESHOLD && radiusVarianceRatio < 0.3f;
    }

    private void OnCircleDetected()
    {
        LoggerUtils.Log("Circle gesture detected! Opening KCC Debug Panel...");
        // 打开KCC调试面板（使用GUI方式）
        KCCDebugPanel.Instance.Show();
    }
}

