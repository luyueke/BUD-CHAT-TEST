using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BUD.AnimPose;
using GameData.BaseInfo;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 处理时间轴相关操作的Controller
/// </summary>
public class MusicTimeLineController : TimeLineController
{
    [Header("BGM操作")]
    public CButton Btn_AddBgmDefault;
    public CButton Btn_AddBgm_1;
    public CButton Btn_AddBgm_2;
    public CButton Btn_AddBgm_3;
    public Transform BgmKeyFrameItemContent;
    public ScrollRect ItemScv;

    private List<AudioTrackData> _audioTrackDatas = new List<AudioTrackData>();
    private List<BgmKeyFrameItem> _bgmKeyFrameItems = new List<BgmKeyFrameItem>();
    private List<AnimBgmTrackInfo> _animBgmTrackInfoList = new List<AnimBgmTrackInfo>();

    //Actions
    private Action<BgmKeyFrameItem> _onBgmKFItemSelect;

    private List<string> _randomColorList = new List<string>()
        { "#FFB800", "#82C3FF", "#D782FF", "#FF8282", "#07FFD2", "#FF7E07" };

    private string _lastColor = null; // 保存上一次生成的颜色

    // Event that gets invoked whenever _animBgmTrackInfoList changes
    public event Action OnAnimBgmTrackInfoListChanged;

    public List<AnimBgmTrackInfo> GetTrackInfo()
    {
        return _animBgmTrackInfoList;
    }

    private void Awake()
    {
        _keyFrameItemPath = "Assets/Loadable/UI/UIPanel/AnimationStudio/Prefabs/MusicKeyFrameItem.prefab";
    }

    public void InitData(List<AnimBgmTrackInfo> animBgmTrackInfoList, Action<BgmKeyFrameItem> onBgmKFItemSelect)
    {
        this._animBgmTrackInfoList = animBgmTrackInfoList;
        this._onBgmKFItemSelect = onBgmKFItemSelect;

        if (this._animBgmTrackInfoList == null || this._animBgmTrackInfoList.Count == 0)
        {
            this._animBgmTrackInfoList = new List<AnimBgmTrackInfo>()
            {
                new AnimBgmTrackInfo()
                {
                    trackId = 0,
                    animMusicList = new List<AnimMusicInfo>()
                },
                new AnimBgmTrackInfo()
                {
                    trackId = 1,
                    animMusicList = new List<AnimMusicInfo>()
                },
                new AnimBgmTrackInfo()
                {
                    trackId = 2,
                    animMusicList = new List<AnimMusicInfo>()
                },
            };
        }

        _audioTrackDatas.Add(new AudioTrackData()
        {
            trackId = 0,
            button = Btn_AddBgm_1.gameObject,
            trackPosY = Btn_AddBgm_1.GetComponent<RectTransform>().anchoredPosition.y
        });
        _audioTrackDatas.Add(new AudioTrackData()
        {
            trackId = 1,
            button = Btn_AddBgm_2.gameObject,
            trackPosY = Btn_AddBgm_2.GetComponent<RectTransform>().anchoredPosition.y
        });
        _audioTrackDatas.Add(new AudioTrackData()
        {
            trackId = 2,
            button = Btn_AddBgm_3.gameObject,
            trackPosY = Btn_AddBgm_3.GetComponent<RectTransform>().anchoredPosition.y
        });

        AddListener();
        InitBgmTrack();

        // Subscribe to the event to update buttons when the track info list changes
        OnAnimBgmTrackInfoListChanged += UpdateTrackButtonsVisibility;

        // Initial update of button visibility
        UpdateTrackButtonsVisibility();
    }

    protected override void ForceSetPadding()
    {
        base.ForceSetPadding();
        var btn1Rect = Btn_AddBgm_1.transform.GetComponent<RectTransform>();
        btn1Rect.anchoredPosition = new Vector2(ParentPanel.sizeDelta.x / 2, btn1Rect.anchoredPosition.y);
        var btn2Rect = Btn_AddBgm_2.transform.GetComponent<RectTransform>();
        btn2Rect.anchoredPosition = new Vector2(ParentPanel.sizeDelta.x / 2, btn2Rect.anchoredPosition.y);
        var btn3Rect = Btn_AddBgm_3.transform.GetComponent<RectTransform>();
        btn3Rect.anchoredPosition = new Vector2(ParentPanel.sizeDelta.x / 2, btn3Rect.anchoredPosition.y);
    }

    private void AddListener()
    {
        Btn_AddBgm_1.onClick.AddListener(() => { OnBtnAddBgmClick(0); });
        Btn_AddBgm_2.onClick.AddListener(() => { OnBtnAddBgmClick(1); });
        Btn_AddBgm_3.onClick.AddListener(() => { OnBtnAddBgmClick(2); });
    }

    private void InitBgmTrack()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(KeyFrameContent);
        foreach (var trackInfo in this._animBgmTrackInfoList)
        {
            var trackId = trackInfo.trackId;
            foreach (var animMusicInfo in trackInfo.animMusicList)
            {
                CreateBgmKeyFrameItem(trackId, animMusicInfo);
            }
        }
    }

    #region Button Func

    private void OnBtnAddBgmClick(int trackId)
    {
        ChooseUgcAnimBgmData data = new ChooseUgcAnimBgmData()
        {
            trackId = trackId,
            onChooseBgm = OnGetBgm
        };
        UIManager.Inst.OpenPanel<UgcAnimChooseTonePanel>(PanelId.UgcAnimChooseTonePanel, data);
    }

    private void OnGetBgm(int trackId, AnimMusicInfo info)
    {
        if (info == null)
            return;

        if (!UgcAnimVipChecker.Inst.CanAddMultiAudio(trackId, _animBgmTrackInfoList))
        {
            return;
        }
        
        CreateNewBgmKeyFrameItem(trackId, info);
    }

    #endregion

    private void CreateNewBgmKeyFrameItem(int trackId, AnimMusicInfo animMusicInfo)
    {
        // 1. 获取当前选中的帧索引
        if (_curSelectKeyFrameItem == null)
        {
            Debug.LogError("当前未选中任何关键帧。");
            return;
        }
        var curFrameIndex = _curSelectKeyFrameItem.GetIndex();

        // 2. 确保轨道列表中存在指定的轨道
        var trackInfo = GetTrackById(trackId);
        if (trackInfo == null)
        {
            trackInfo = new AnimBgmTrackInfo { trackId = trackId, animMusicList = new List<AnimMusicInfo>() };
            AddTrack(trackInfo);
        }

        // 3. 检查指定轨道在当前帧是否有空位，并判断是否会导致后续音频项移动
        if (!IsFrameOccupied(trackInfo, curFrameIndex))
        {
            // 临时设置新项的开始帧为当前帧，用于判断
            animMusicInfo.startFrame = curFrameIndex;

            if (!WillCauseAdjustments(trackInfo, animMusicInfo))
            {
                // 在指定轨道的当前帧插入音频Item
                AddMusicItemToTrack(trackInfo.trackId, animMusicInfo);

                // 创建 BgmKeyFrameItem，如果失败则删除已添加的数据
                if (!CreateBgmKeyFrameItem(trackInfo.trackId, animMusicInfo))
                {
                    RemoveMusicItemFromTrack(trackInfo.trackId, animMusicInfo);
                }
                else
                {
                    // 插入后需要调整后续的音频Items，避免重叠
                    AdjustFollowingItems(trackInfo, animMusicInfo);
                }

                return;
            }
            else
            {
                // 如果会导致后续音频项移动，尝试其他轨道
                animMusicInfo.startFrame = 0; // 重置startFrame，避免影响后续判断
            }
        }

        // 4. 检查其他轨道在当前帧是否有空位，且不会导致后续项调整
        foreach (var otherTrack in _animBgmTrackInfoList)
        {
            if (otherTrack.trackId == trackId) continue;

            if (!IsFrameOccupied(otherTrack, curFrameIndex))
            {
                animMusicInfo.startFrame = curFrameIndex;
                if (!WillCauseAdjustments(otherTrack, animMusicInfo))
                {
                    // 在找到的空轨道的当前帧插入音频Item
                    AddMusicItemToTrack(otherTrack.trackId, animMusicInfo);

                    // 创建 BgmKeyFrameItem，如果失败则删除已添加的数据
                    if (!CreateBgmKeyFrameItem(otherTrack.trackId, animMusicInfo))
                    {
                        RemoveMusicItemFromTrack(otherTrack.trackId, animMusicInfo);
                    }
                    else
                    {
                        // 插入后需要调整后续的音频Items，避免重叠
                        AdjustFollowingItems(otherTrack, animMusicInfo);
                    }

                    return;
                }
                else
                {
                    animMusicInfo.startFrame = 0; // 重置startFrame，避免影响后续判断
                }
            }
        }

        // 5. 在指定的轨道中寻找下一个可用的空位
        int nextFreeIndex = curFrameIndex + 1;
        while (true)
        {
            if (!IsFrameOccupied(trackInfo, nextFreeIndex))
            {
                animMusicInfo.startFrame = nextFreeIndex;
                // 此时不需要判断是否会调整后续项，因为已经避开了用户的设计
                AddMusicItemToTrack(trackInfo.trackId, animMusicInfo);

                // 创建 BgmKeyFrameItem，如果失败则删除已添加的数据
                if (!CreateBgmKeyFrameItem(trackInfo.trackId, animMusicInfo))
                {
                    RemoveMusicItemFromTrack(trackInfo.trackId, animMusicInfo);
                }
                else
                {
                    // 插入后需要调整后续的音频Items，避免重叠
                    AdjustFollowingItems(trackInfo, animMusicInfo);
                }

                break;
            }
            else
            {
                // 跳过当前重叠的音频Item，直接移动到其结束帧之后
                var overlappingItem = GetOverlappingItem(trackInfo, nextFreeIndex);
                if (overlappingItem != null)
                {
                    int endFrame = overlappingItem.startFrame + overlappingItem.frameLen;
                    nextFreeIndex = endFrame;
                }
                else
                {
                    nextFreeIndex++;
                }
            }
        }
    }

    // 新增方法：判断插入新项是否会导致后续的音频项被调整
    private bool WillCauseAdjustments(AnimBgmTrackInfo trackInfo, AnimMusicInfo newItem)
    {
        int newItemStart = newItem.startFrame;
        int newItemEnd = newItem.startFrame + newItem.frameLen;

        // 查找需要调整的项
        var itemsToAdjust = trackInfo.animMusicList
            .Where(x => x != newItem && x.startFrame >= newItemStart)
            .OrderBy(x => x.startFrame)
            .ToList();

        foreach (var item in itemsToAdjust)
        {
            int itemStart = item.startFrame;

            if (itemStart < newItemEnd)
            {
                // 有项需要调整
                return true;
            }
        }
        // 不需要调整
        return false;
    }

    // 调整后续的音频Items，避免重叠
    private void AdjustFollowingItems(AnimBgmTrackInfo trackInfo, AnimMusicInfo newItem)
    {
        int newItemStart = newItem.startFrame;
        int newItemEnd = newItem.startFrame + newItem.frameLen;

        // 按开始帧排序
        var itemsToAdjust = trackInfo.animMusicList
            .Where(x => x != newItem && x.startFrame >= newItemStart)
            .OrderBy(x => x.startFrame)
            .ToList();

        foreach (var item in itemsToAdjust)
        {
            int itemStart = item.startFrame;
            int itemEnd = item.startFrame + item.frameLen;

            if (itemStart < newItemEnd)
            {
                // 计算需要移动的帧数
                int deltaFrames = newItemEnd - itemStart;
                item.startFrame += deltaFrames;
                newItemEnd = item.startFrame + item.frameLen;

                // 更新对应的UI位置
                var bgmItem = _bgmKeyFrameItems.Find(b => b.animMusicInfo == item);
                if (bgmItem != null)
                {
                    float posX = GetPositionXFromFrameIndex(item.startFrame);
                    if (float.IsNaN(posX))
                    {
                        // 无法获取正确的 X 坐标，删除该 Item
                        Debug.LogError($"无法更新 BgmKeyFrameItem，因为未找到帧索引为 {item.startFrame} 的关键帧Item。");

                        // 从轨道中移除音频Item
                        RemoveMusicItemFromTrack(trackInfo.trackId, item);

                        // 删除 UI Item
                        _bgmKeyFrameItems.Remove(bgmItem);
                        Destroy(bgmItem.gameObject);

                        continue;
                    }
                    float posY = bgmItem.RectTrans.anchoredPosition.y;
                    bgmItem.SetPos(new Vector2(posX, posY));
                }

                // Notify that the item has been updated
                UpdateMusicItemInTrack(trackInfo.trackId, item);
            }
            else
            {
                // 后面的Items不受影响
                break;
            }
        }
    }

    // 辅助方法：检查某个轨道在指定帧是否被占用
    private bool IsFrameOccupied(AnimBgmTrackInfo trackInfo, int frameIndex, AnimMusicInfo ignoreItem = null)
    {
        if (trackInfo == null || trackInfo.animMusicList == null)
            return false;

        foreach (var animMusicInfo in trackInfo.animMusicList)
        {
            if (ignoreItem != null && animMusicInfo == ignoreItem)
                continue; // 忽略指定的项

            int start = animMusicInfo.startFrame;
            int end = animMusicInfo.startFrame + animMusicInfo.frameLen;
            if (start <= frameIndex && frameIndex < end)
            {
                return true;
            }
        }

        return false;
    }

    // 辅助方法：获取在指定帧与现有音频Item重叠的音频Item
    private AnimMusicInfo GetOverlappingItem(AnimBgmTrackInfo trackInfo, int frameIndex, AnimMusicInfo ignoreItem = null)
    {
        if (trackInfo == null || trackInfo.animMusicList == null)
            return null;

        foreach (var animMusicInfo in trackInfo.animMusicList)
        {
            if (ignoreItem != null && animMusicInfo == ignoreItem)
                continue; // 忽略指定的项

            int start = animMusicInfo.startFrame;
            int end = animMusicInfo.startFrame + animMusicInfo.frameLen;
            if (start <= frameIndex && frameIndex < end)
            {
                return animMusicInfo;
            }
        }

        return null;
    }

    public bool CreateBgmKeyFrameItem(int trackId, AnimMusicInfo info)
    {
        var startIndex = info.startFrame;
        var trackData = _audioTrackDatas.Find(x => x.trackId == trackId);
        if (trackData == null)
        {
            Debug.LogError($"轨道ID {trackId} 不存在于 _audioTrackDatas 中。");
            return false;
        }

        var trackPosY = trackData.trackPosY;
        var startPosX = GetPositionXFromFrameIndex(startIndex);
        if (float.IsNaN(startPosX))
        {
            // 无法获取正确的 X 坐标，停止创建 Item，并删除相关数据
            Debug.LogError($"无法创建 BgmKeyFrameItem，因为未找到帧索引为 {startIndex} 的关键帧Item。");

            // 不创建 Item
            return false;
        }

        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/AnimationStudio/Prefabs/BgmKeyFrameItem.prefab")
            .Instantiate(BgmKeyFrameItemContent);
        BgmKeyFrameItem item = itemObj.GetComponent<BgmKeyFrameItem>();
        if (item == null)
        {
            Debug.LogError("BgmKeyFrameItem 脚本未附加到预制件上。");
            return false;
        }

        _bgmKeyFrameItems.Add(item);

        var zoomLevel = _curKeyFrameItems[0].GetZoomLevel();
        item.SetParentScvData(ItemScv, OnDrag, OnEndDrag);
        item.SetAction(OnBgmKeyFrameItemStopDragging, OnMusicKeyFrameItemClick);
        item.SetData(trackId, info, GetRandomColor(), zoomLevel);
        item.SetPos(new Vector2(startPosX, trackPosY));

        return true;
    }

    private void OnMusicKeyFrameItemClick(BgmKeyFrameItem item)
    {
        int trackId = item.trackId;
        _bgmKeyFrameItems.ForEach(x => x.SetSelectState(false));
        item.SetSelectState(true);

        this.Btn_AddBgmDefault.onClick.RemoveAllListeners();
        this.Btn_AddBgmDefault.onClick.AddListener(() => { OnBtnAddBgmClick(trackId); });
        this.Btn_AddBgmDefault.gameObject.SetActive(true);

        _onBgmKFItemSelect?.Invoke(item);
    }

    public Color GetRandomColor()
    {
        string newColor;
        do
        {
            int randomIndex = UnityEngine.Random.Range(0, _randomColorList.Count);
            newColor = _randomColorList[randomIndex];
        } while (newColor == _lastColor); // 确保新颜色与上次不相同

        _lastColor = newColor; // 更新保存的颜色
        return DataUtil.DeSerializeColorCheckHash(newColor);
    }

    private void OnBgmKeyFrameItemStopDragging(BgmKeyFrameItem bgmKeyFrameItem)
    {
        // 步骤1：根据 Y 坐标找到最近的轨道
        float minTrackDistance = Mathf.Infinity;
        int targetTrackId = -1;
        float targetTrackPosY = -1f;
        var itemPosY = bgmKeyFrameItem.RectTrans.anchoredPosition.y + 36;

        foreach (var trackData in _audioTrackDatas)
        {
            float distance = Mathf.Abs(itemPosY - trackData.trackPosY);
            if (distance < minTrackDistance)
            {
                minTrackDistance = distance;
                targetTrackId = trackData.trackId;
                targetTrackPosY = trackData.trackPosY;
            }
        }

        if (targetTrackId == -1)
        {
            Debug.LogError("未找到可吸附的轨道。");
            return;
        }

        // 步骤2：根据 X 坐标找到最近的帧索引
        float minFrameDistance = Mathf.Infinity;
        int targetFrameIndex = -1;
        float targetPosX = 0f;
        var itemPosX = bgmKeyFrameItem.RectTrans.anchoredPosition.x;

        foreach (var keyFrameItem in _curKeyFrameItems)
        {
            float keyFramePosX = keyFrameItem.RectTrans.anchoredPosition.x;
            float distance = Mathf.Abs(itemPosX - keyFramePosX);
            if (distance < minFrameDistance)
            {
                minFrameDistance = distance;
                targetFrameIndex = keyFrameItem.GetIndex();
                targetPosX = keyFramePosX;
            }
        }

        if (targetFrameIndex == -1)
        {
            Debug.LogError("未找到可吸附的帧索引。");
            return;
        }

        // 步骤3：检查目标轨道在目标帧是否有空位（忽略当前拖拽的项）
        if (!IsFrameOccupied(targetTrackId, targetFrameIndex, bgmKeyFrameItem.animMusicInfo))
        {
            // 直接更新Item数据，不检查是否会导致后续项调整
            UpdateItemData(bgmKeyFrameItem, targetTrackId, targetFrameIndex, targetPosX, targetTrackPosY);

            // 插入后需要调整后续的音频Items，避免重叠
            var trackInfo = GetTrackById(targetTrackId);
            if (trackInfo != null)
            {
                AdjustFollowingItems(trackInfo, bgmKeyFrameItem.animMusicInfo);
            }

            return;
        }

        // 步骤4：检查其他轨道在目标帧是否有空位（忽略当前拖拽的项）
        foreach (var trackData in _audioTrackDatas)
        {
            if (trackData.trackId == targetTrackId) continue;

            if (!IsFrameOccupied(trackData.trackId, targetFrameIndex, bgmKeyFrameItem.animMusicInfo))
            {
                // 直接更新Item数据，不检查是否会导致后续项调整
                UpdateItemData(bgmKeyFrameItem, trackData.trackId, targetFrameIndex, targetPosX, trackData.trackPosY);

                // 插入后需要调整后续的音频Items，避免重叠
                var otherTrackInfo = GetTrackById(trackData.trackId);
                if (otherTrackInfo != null)
                {
                    AdjustFollowingItems(otherTrackInfo, bgmKeyFrameItem.animMusicInfo);
                }

                return;
            }
        }

        // 步骤5：在初始轨道上向后寻找下一个可用位置（忽略当前拖拽的项）
        int nextAvailableFrameIndex = targetFrameIndex + 1;
        while (true)
        {
            if (!IsFrameOccupied(targetTrackId, nextAvailableFrameIndex, bgmKeyFrameItem.animMusicInfo))
            {
                float nextPosX = GetPositionXFromFrameIndex(nextAvailableFrameIndex);
                if (float.IsNaN(nextPosX))
                {
                    // 无法获取正确的 X 坐标，删除该 Item
                    Debug.LogError($"无法更新 BgmKeyFrameItem，因为未找到帧索引为 {nextAvailableFrameIndex} 的关键帧Item。");

                    // 从轨道中移除音频Item
                    RemoveMusicItemFromTrack(targetTrackId, bgmKeyFrameItem.animMusicInfo);

                    // 删除 UI Item
                    _bgmKeyFrameItems.Remove(bgmKeyFrameItem);
                    Destroy(bgmKeyFrameItem.gameObject);

                    break;
                }

                UpdateItemData(bgmKeyFrameItem, targetTrackId, nextAvailableFrameIndex, nextPosX, targetTrackPosY);

                // 插入后需要调整后续的音频Items，避免重叠
                var trackInfo = GetTrackById(targetTrackId);
                if (trackInfo != null)
                {
                    AdjustFollowingItems(trackInfo, bgmKeyFrameItem.animMusicInfo);
                }

                break;
            }
            else
            {
                // 跳过当前重叠的音频Item，直接移动到其结束帧之后
                var overlappingItem = GetOverlappingItem(targetTrackId, nextAvailableFrameIndex, bgmKeyFrameItem.animMusicInfo);
                if (overlappingItem != null)
                {
                    int endFrame = overlappingItem.startFrame + overlappingItem.frameLen;
                    nextAvailableFrameIndex = endFrame;
                }
                else
                {
                    nextAvailableFrameIndex++;
                }
            }
        }
    }

    //检查某轨道的某帧是否被占用，忽略指定的项
    private bool IsFrameOccupied(int trackId, int frameIndex, AnimMusicInfo ignoreItem = null)
    {
        var trackInfo = GetTrackById(trackId);
        return IsFrameOccupied(trackInfo, frameIndex, ignoreItem);
    }

    //获取某轨道在指定帧上重叠的音频Item，忽略指定的项
    private AnimMusicInfo GetOverlappingItem(int trackId, int frameIndex, AnimMusicInfo ignoreItem = null)
    {
        var trackInfo = GetTrackById(trackId);
        return GetOverlappingItem(trackInfo, frameIndex, ignoreItem);
    }

    // 根据帧索引获取对应的 X 坐标
    private float GetPositionXFromFrameIndex(int frameIndex)
    {
        var keyFrameItem = _curKeyFrameItems.Find(k => k.GetIndex() == frameIndex);
        if (keyFrameItem != null)
        {
            return keyFrameItem.RectTrans.anchoredPosition.x;
        }
        // 如果未找到对应的帧，返回 NaN 表示错误
        Debug.LogError($"未找到帧索引为 {frameIndex} 的关键帧Item。");
        return float.NaN;
    }

    private void UpdateItemData(BgmKeyFrameItem item, int newTrackId, int newFrameIndex, float posX, float posY)
    {
        int oldTrackId = item.trackId;
        int oldStartFrame = item.animMusicInfo.startFrame;

        // 更新 AnimMusicInfo 的 startFrame
        item.animMusicInfo.startFrame = newFrameIndex;

        if (oldTrackId != newTrackId)
        {
            // 从旧轨道移除
            RemoveMusicItemFromTrack(oldTrackId, item.animMusicInfo);

            // 添加到新轨道
            AddMusicItemToTrack(newTrackId, item.animMusicInfo);

            // 更新 Item 的 trackId
            item.trackId = newTrackId;
        }

        // 获取新的 X 坐标
        float newPosX = GetPositionXFromFrameIndex(newFrameIndex);
        if (float.IsNaN(newPosX))
        {
            // 无法获取正确的 X 坐标，停止更新，删除相关数据
            Debug.LogError($"无法更新 BgmKeyFrameItem，因为未找到帧索引为 {newFrameIndex} 的关键帧Item。");

            // 从轨道中移除音频Item
            RemoveMusicItemFromTrack(newTrackId, item.animMusicInfo);

            // 删除 UI Item
            _bgmKeyFrameItems.Remove(item);
            Destroy(item.gameObject);

            return;
        }

        // 更新 Item 的位置
        item.SetPos(new Vector2(newPosX, posY));

        // 通知更新
        UpdateMusicItemInTrack(newTrackId, item.animMusicInfo);
    }

    /// <summary>
    /// 删除指定轨道上的音频Item，并确保Item和数据被删除
    /// </summary>
    public void DeleteBgmKeyFrameItem(BgmKeyFrameItem item)
    {
        int trackId = item.trackId;
        int frameIndex = item.animMusicInfo.startFrame;

        // 从轨道中移除音频Item
        RemoveMusicItemFromTrack(trackId, item.animMusicInfo);

        // 移除UI中的音频Item
        var bgmKeyFrameItem = _bgmKeyFrameItems.Find(x => x.animMusicInfo == item.animMusicInfo);
        if (bgmKeyFrameItem != null)
        {
            _bgmKeyFrameItems.Remove(bgmKeyFrameItem);
            Destroy(bgmKeyFrameItem.gameObject); // 删除UI对象
        }

        Debug.Log($"成功删除轨道 {trackId} 上帧索引为 {frameIndex} 的音频Item。");
    }

    // 新增方法：根据 trackId 获取轨道
    private AnimBgmTrackInfo GetTrackById(int trackId)
    {
        return _animBgmTrackInfoList.Find(x => x.trackId == trackId);
    }

    // 新增方法：添加轨道
    private void AddTrack(AnimBgmTrackInfo trackInfo)
    {
        _animBgmTrackInfoList.Add(trackInfo);
        OnAnimBgmTrackInfoListChanged?.Invoke();
    }

    // 新增方法：移除轨道
    private void RemoveTrack(AnimBgmTrackInfo trackInfo)
    {
        _animBgmTrackInfoList.Remove(trackInfo);
        OnAnimBgmTrackInfoListChanged?.Invoke();
    }

    // 新增方法：向轨道添加音频Item
    private void AddMusicItemToTrack(int trackId, AnimMusicInfo animMusicInfo)
    {
        var trackInfo = GetTrackById(trackId);
        if (trackInfo == null)
        {
            trackInfo = new AnimBgmTrackInfo { trackId = trackId, animMusicList = new List<AnimMusicInfo>() };
            AddTrack(trackInfo);
        }
        trackInfo.animMusicList.Add(animMusicInfo);
        OnAnimBgmTrackInfoListChanged?.Invoke();
    }

    // 新增方法：从轨道移除音频Item
    private void RemoveMusicItemFromTrack(int trackId, AnimMusicInfo animMusicInfo)
    {
        var trackInfo = GetTrackById(trackId);
        if (trackInfo != null)
        {
            trackInfo.animMusicList.Remove(animMusicInfo);
            OnAnimBgmTrackInfoListChanged?.Invoke();
        }
    }

    // 新增方法：更新轨道上的音频Item
    private void UpdateMusicItemInTrack(int trackId, AnimMusicInfo animMusicInfo)
    {
        // 如果需要在更新时执行某些操作，可以在这里处理
        OnAnimBgmTrackInfoListChanged?.Invoke();
    }

    /// <summary>
    /// 更新音轨按钮的可见性
    /// </summary>
    private void UpdateTrackButtonsVisibility()
    {
        foreach (var trackData in _audioTrackDatas)
        {
            var trackInfo = GetTrackById(trackData.trackId);
            if (trackInfo != null && trackInfo.animMusicList.Count > 0)
            {
                // 轨道有数据，隐藏按钮
                if (trackData.button != null)
                {
                    trackData.button.SetActive(false);
                }
            }
            else
            {
                // 轨道没有数据，显示按钮
                if (trackData.button != null)
                {
                    trackData.button.SetActive(true);
                }
            }
        }
    }

    public override void ZoomTimeLine(bool IsZoomIn)
    {
        base.ZoomTimeLine(IsZoomIn);
        LayoutRebuilder.ForceRebuildLayoutImmediate(KeyFrameContent);
        // 更新 BgmKeyFrameItems 的位置
        foreach (var bgmItem in _bgmKeyFrameItems)
        {
            int frameIndex = bgmItem.animMusicInfo.startFrame;
            float posX = GetPositionXFromFrameIndex(frameIndex);
            if (float.IsNaN(posX))
            {
                Debug.LogError($"无法更新 BgmKeyFrameItem，因为未找到帧索引为 {frameIndex} 的关键帧Item。");
                continue;
            }
            var posY = bgmItem.RectTrans.anchoredPosition.y;
            bgmItem.Zoom(IsZoomIn);
            bgmItem.SetPos(new Vector2(posX, posY));
        }
    }
}

public class AudioTrackData
{
    public int trackId;
    public float trackPosY;
    public GameObject button;
}
