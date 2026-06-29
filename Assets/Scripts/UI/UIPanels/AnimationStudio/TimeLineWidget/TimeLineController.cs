using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BUD.AnimPose;
using GameData.BaseInfo;
using GameData.Manager;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 处理时间轴相关操作的Controller
/// </summary>
public class TimeLineController : MonoBehaviour, IDragHandler , IEndDragHandler
{
    [Header("时间轴相关")] 
    public RectTransform ParentPanel;
    public HorizontalLayoutGroup horizontalLayoutGroup;
    public Transform Trans_CenterLine;
    public ScrollRect KeyFrameScv;
    public RectTransform KeyFrameContent;
    public RectTransform ViewportRT;
    public HorizontalLayoutGroup KFHorLayout;
    
    private bool canPlayAnim = false;
    private bool _isLoop = false;
    private float currentAnimRuntime = 0;
    private float currentAnimEndTime = 0;
    private int currentAnimEndFrame = 0;
    protected List<KeyFrameItem> _curKeyFrameItems = new List<KeyFrameItem>();
    protected string _keyFrameItemPath = "Assets/Loadable/UI/UIPanel/AnimationStudio/Prefabs/KeyFrameItem.prefab";

    //当前选中的关键帧
    protected KeyFrameItem _curSelectKeyFrameItem;
    private Action<KeyFrameItem> onSnapItemAct;
    private Action<KeyFrameItem> onItemSelectAct;
    private Action<float> onSetAnimationAct;
    private Action onStopAct;
    private Action onLoopEndAct;
    //帧率
    private int _frameFrequency = 10;
    //帧数
    private int _timeLineFrameCount = 201;
    private int _spacing = 10;

    #region DefaultData
    private List<RoleKeyframeData> _defRoleKFData;
    private string _defRoleKFPath = "Assets/Loadable/UI/UIPanel/AnimationStudio/Prefabs/UgcAnimDefaultData.json";
    #endregion
    
    #region Drag相关
    public void OnDrag(PointerEventData eventData)
    {
        SetCurAnimStepLogic();
    }

    // 当用户结束拖动时触发
    public void OnEndDrag(PointerEventData eventData)
    {
        SnapToNearestItem();
        SetCurAnimStepLogic();
    }
    #endregion 
    
    
    public void SetAction(Action<KeyFrameItem> onSnapItem = null, Action<KeyFrameItem> onItemSelect = null, Action<float> onSetAnimation = null, Action onStop = null,  Action onLoopEnd = null)
    {
        this.onSnapItemAct = onSnapItem; 
        this.onItemSelectAct = onItemSelect;
        this.onSetAnimationAct = onSetAnimation;
        this.onStopAct = onStop;
        this.onLoopEndAct = onLoopEnd;
    }

    public List<KeyFrameItem> CreateTimeLine(AnimFrameData data)
    {
        //1.创建时间轴 - 200个帧
        for (int i = 0; i < _timeLineFrameCount; i++)
        {
            var keyFrameItemObj = Loader.Load<GameObject>(_keyFrameItemPath).Instantiate(KeyFrameContent);
            var keyFrameItemComp = keyFrameItemObj.GetComponent<KeyFrameItem>();
            keyFrameItemComp.InitKeyFrameItem(i, OnKeyFrameItemSelect);
            _curKeyFrameItems.Add(keyFrameItemComp);
        }
        
        //2.绑定数据
        BindTimeLineData(data);

        ForceSetPadding();
        return _curKeyFrameItems;
    }

    /// <summary>
    /// 时间轴适配
    /// </summary>
    protected virtual void ForceSetPadding()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(ParentPanel);
        float curContentWidth = ParentPanel.sizeDelta.x;
        var padding = curContentWidth / 2 - _curKeyFrameItems[0].RectTrans.sizeDelta.x / 2;
        horizontalLayoutGroup.padding.left = (int)padding;
        horizontalLayoutGroup.padding.right = (int)padding;
    }

    public void OnKeyFrameItemSelect(KeyFrameItem item)
    {
        this._curSelectKeyFrameItem = item;
        this.onItemSelectAct?.Invoke(item);
    }

    //绑定时间轴数据
    public void BindTimeLineData(AnimFrameData data)
    {
        if (data == null || data.animFrames == null || data.animFrames.Count == 0)
            return;
        
        if(_curKeyFrameItems == null || _curKeyFrameItems.Count == 0)
            return;
        
        //RestData
        _curKeyFrameItems.ForEach(x=>x.SetData(null));
        var lastFrame = data.animFrames.Last().frame;
        data.animFrames.ForEach(x =>
        {
            var curFrameIndex = x.frame;
            var kFItem = _curKeyFrameItems.Find(x => x.GetIndex() == curFrameIndex);
            //数据注入
            if (kFItem != null)
            {
                kFItem.SetData(x, x.frame == lastFrame);
            }
        });
    }

    //根据当前的时间轴Data刷新时间轴状态
    public void RefreshKeyFrameItemStateByData(AnimFrameData data)
    {
        var lastFrameData = data.animFrames.Last();
        var lastFrameIndex = lastFrameData.frame;
        
        _curKeyFrameItems.ForEach(x =>
        {
            var curKFItemMode = FrameSelectMode.EmptyFrameData;
            
            if (x.GetData() != null)
            {
                if (x.GetData().isEmptySlot == 1)
                {
                    curKFItemMode = FrameSelectMode.EmptySlot;
                }
                else
                {
                    curKFItemMode = FrameSelectMode.AddedFrameData;
                }
            }
            x.RefreshItemBg(curKFItemMode);
        });
    }

    //将距离中线最近的Item吸附到中线
    public void SnapToNearestItem()
    {
        float minDistance = Mathf.Infinity;
        KeyFrameItem curNearestKFItem = null;
        foreach (KeyFrameItem item in _curKeyFrameItems)
        {
            // 计算Item中心点与中线的距离
            var itemTrans = item.transform;
            float distance = Mathf.Abs(Trans_CenterLine.position.x - itemTrans.position.x);
            if (distance < minDistance)
            {
                minDistance = distance;
                curNearestKFItem = item;
            }
        }

        if (curNearestKFItem != null)
        {
            SnapToItem(curNearestKFItem);
        }
    }

    public void SnapToItem(KeyFrameItem item)
    {
        if(item == null)
            return;
        
        // 平滑移动ScrollView，使最近的Item吸附到中线位置
        float targetPositionX = -item.GetContentWidth() * item.GetIndex();
        // 吸附完成
        KeyFrameScv.content.localPosition = new Vector3(targetPositionX, KeyFrameScv.content.localPosition.y, KeyFrameScv.content.localPosition.z);
        //速度衰减到0
        KeyFrameScv.velocity = Vector2.zero;
        this._curSelectKeyFrameItem = item;
        
        this.onSnapItemAct?.Invoke(item);
    }

    public virtual void ZoomTimeLine(bool IsZoomIn)
    {
        if (IsZoomIn)
        {
            _curKeyFrameItems.ForEach(x => x.ZoomIn());
        }
        else
        {
            _curKeyFrameItems.ForEach(x => x.ZoomOut());
        }
        
        var newOffset = ViewportRT.rect.width / 2 - _curKeyFrameItems[0].GetContentWidth() / 2 + _spacing / 2;
        KFHorLayout.padding = new RectOffset((int)newOffset, (int)newOffset, 0, 0);
    }

    public void StartPlay(int startIndex, int endIndex, bool isLoop = false)
    {
        canPlayAnim = true;
        this._isLoop = isLoop;
        currentAnimEndFrame = endIndex;
        currentAnimRuntime = startIndex / (_frameFrequency * 1.00f);
        currentAnimEndTime = endIndex / (_frameFrequency * 1.00f);
    }

    // 设置当前Line指向的Frame,并且表现当前动画
    public void SetCurAnimStepLogic()
    {
        onStopAct?.Invoke();
        canPlayAnim = false;
        SetStepAnimation();
    }

    //播放结束，播到最后一帧
    private void OnPlayOver()
    {
        var endContentX = -currentAnimEndTime * GetPreSecondContentWidth();
        KeyFrameContent.anchoredPosition = new Vector2(endContentX, KeyFrameContent.anchoredPosition.y);
        //1.吸附到最后的那一帧
        var lastItem = _curKeyFrameItems.Find(x => x.GetIndex() == currentAnimEndFrame);
        if (lastItem != null)
        {
            SnapToItem(lastItem);
        }
        //2.停止播放
        SetCurAnimStepLogic();
    }
    
    private void OnPlayOverLoop()
    {
        currentAnimRuntime = 0; 
        //1.吸附到第一帧
        KeyFrameScv.content.localPosition = new Vector3(0, KeyFrameScv.content.localPosition.y, KeyFrameScv.content.localPosition.z);
        onLoopEndAct?.Invoke();
    }
    
    private void Update()
    {
        if (canPlayAnim)
        {
            if (currentAnimRuntime >= currentAnimEndTime)
            {
                if (this._isLoop)
                {
                    OnPlayOverLoop();
                }
                else
                {
                    OnPlayOver();
                }
            }
            
            currentAnimRuntime += Time.deltaTime;
            
            if (currentAnimRuntime >= currentAnimEndTime)
            {
                currentAnimRuntime = currentAnimEndTime;
            }
            SetTimeStamp(currentAnimRuntime);
            
            var newContentX = -currentAnimRuntime * GetPreSecondContentWidth();
            KeyFrameContent.anchoredPosition = new Vector2(newContentX, KeyFrameContent.anchoredPosition.y);
        }
    }

    private void SetTimeStamp(float time)
    {
        onSetAnimationAct?.Invoke(time);
    }

    public void SetStepAnimation()
    {
        var curW = -KeyFrameContent.anchoredPosition.x;
        var curTime = curW  / GetPreSecondContentWidth();
        SetTimeStamp(curTime);
    }

    private float GetPreSecondContentWidth()
    {
        return this._frameFrequency * _curKeyFrameItems[0].GetContentWidth() * 1.00f;
    }

    #region 增删操作
    public void AddKeyFrameData(AnimFrameData oriData, int targetIndex)
    {
        // 检查是否可以添加更多关键帧
        if (!UgcAnimVipChecker.Inst.CanAddMoreKeyFrames(targetIndex))
        {
            return;
        }
        
        // 检查是否已经存在相同 frame 的 KeyFrameData
        foreach (var keyFrame in oriData.animFrames)
        {
            if (keyFrame.frame == targetIndex)
            {
                // 已经存在相同 frame 的 KeyFrameData，直接返回不做处理
                return;
            }
        }

        var lastData = oriData.animFrames.Last();
        var lastIndex = lastData.frame;

        for (int i = lastIndex + 1; i <= targetIndex; i++)
        {
            // 创建新的 KeyFrameData
            KeyFrameData newKeyFrame = new KeyFrameData
            {
                frame = i,
                isEmptySlot = 1
            };
            oriData.animFrames.Add(newKeyFrame);
        }
    }

    public void DeleteKeyFrameData(AnimFrameData oriData, int frameIndex)
    {
        if (oriData.animFrames == null || oriData.animFrames.Count == 0)
        {
            LoggerUtils.LogError("DeleteKeyFrameData oriData.animFrames == null");
            return;
        }
        
        // 查找指定 frameIndex 的 KeyFrameData
        int removeIndex = -1;
    
        for (int i = 0; i < oriData.animFrames.Count; i++)
        {
            if (oriData.animFrames[i].frame == frameIndex)
            {
                removeIndex = i;
                break;
            }
        }

        // 如果找到对应 frameIndex 的 KeyFrameData，则删除并更新右侧元素的 frame
        // 删除前的检查，确保不能删除最后一个元素
        if (oriData.animFrames.Count != 1 && removeIndex != -1)
        {
            // 删除指定元素
            oriData.animFrames.RemoveAt(removeIndex);

            // 将右侧的所有元素的 frame - 1
            for (int i = removeIndex; i < oriData.animFrames.Count; i++)
            {
                oriData.animFrames[i].frame -= 1;
            }
        }
        
        // 边界检查，确保不能删除唯一的元素,并且唯一的这一帧需要有值
        if (oriData.animFrames[0].isEmptySlot == 1)
        {
            oriData.animFrames[0].keyFrame = GetDefaultKeyFrame();
        }

        if (removeIndex == 0)
        {
            oriData.animFrames[0].keyFrame = GetDefaultKeyFrame();
        }
    }
    
    public void ClearKeyFrameData(AnimFrameData oriData, int frameIndex)
    {
        // 查找 frame 等于 frameIndex 的元素
        foreach (var keyFrame in oriData.animFrames)
        {
            if (keyFrame.frame == frameIndex)
            {
                keyFrame.isEmptySlot = 1;
                keyFrame.keyFrame = new List<RoleKeyframeData>();
                return;
            }
        }
    }

    public KeyFrameData GetNearestAnimData(AnimFrameData oriData, int frameIndex)
    {
        KeyFrameData nearestKeyFrame = null;
        int smallestDistance = int.MaxValue;

        // 遍历 animFrames，找到 isEmptySlot == 0 且 frame 小于 frameIndex 的元素
        foreach (var keyFrame in oriData.animFrames)
        {
            // 只考虑 isEmptySlot == 0 且 frame 小于 frameIndex 的元素
            if (keyFrame.isEmptySlot == 0 && keyFrame.frame < frameIndex)
            {
                int distance = Math.Abs(keyFrame.frame - frameIndex);

                // 如果当前元素的距离比之前找到的更近，更新最近的 KeyFrameData
                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                    nearestKeyFrame = keyFrame;
                }
            }
        }

        // 返回找到的最近的 KeyFrameData，如果没有找到，返回 null
        return nearestKeyFrame;
    }
    #endregion

    public List<RoleKeyframeData> GetDefaultKeyFrame()
    {
        if (_defRoleKFData == null)
        {
            var animType = GameDataManager.Inst.mapGlobalData.GetCurInfo<AnimInfo>().animType;
            var textAsset = Loader.Load<TextAsset>(_defRoleKFPath, this.gameObject);
            var animDefDatas = JsonConvert.DeserializeObject<List<AnimDefData>>(textAsset.text);
            if (animDefDatas != null)
            {
                var animDefData = animDefDatas.Find(x => x.animType == animType);
                _defRoleKFData = animDefData?.keyFrame;
            }
        }
        return _defRoleKFData;
    }
}

public class AnimDefData
{
    public int animType;
    public List<RoleKeyframeData> keyFrame;
}
