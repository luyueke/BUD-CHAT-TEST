using System.Collections.Generic;
using System.Linq;
using BUD.AnimPose;
using Es;
using GameData.BaseInfo;
using UnityEngine;

public class UgcAnimVipChecker : GlobalInstance<UgcAnimVipChecker>
{
    private const int MaxFreeDurationInSeconds = 5;
    private const int MaxFreeKeyFramesPerSegment = 5; // 非VIP每段最多5个非空关键帧
    private const int FrameFrequency = 10; // 每秒帧率为10

    /// <summary>
    /// 检查用户是否为VIP
    /// </summary>
    public bool IsVipUser()
    {
        return VipDataManager.Inst.isVip;
        // return true;
    }

    /// <summary>
    /// 检查非VIP用户是否可以添加更多关键帧（根据动画时长限制）
    /// </summary>
    public bool CanAddMoreKeyFrames(int targetFrameIndex)
    {
        if (IsVipUser())
        {
            // VIP用户没有限制
            return true;
        }

        // 非VIP用户限制为 MaxFreeDurationInSeconds 秒的动画
        int maxFreeFrameIndex = MaxFreeDurationInSeconds * FrameFrequency + 1; // 最大允许的帧索引为51（含）
        bool canAdd = targetFrameIndex <= maxFreeFrameIndex;
        if (!canAdd)
        {
            var joinVipType = new List<JoinVipType>(){JoinVipType.VIP_Long_Time};
            var joinVipTitle = "您正在使用的VIP功能：" + "超5秒动画制作";
            if (joinVipType.Count > 0)
            {
                UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, joinVipTitle, joinVipType);
            }
            LoggerUtils.LogError("非VIP用户无法制作超过5秒的动画。");
        }
        return canAdd;
    }

    /// <summary>
    /// 检查在当前段落内，非VIP用户是否可以添加新的关键帧
    /// </summary>
    public bool CanAddKeyFramesInSegment(AnimFrameData oriData, int frameToAdd)
    {
        if (IsVipUser())
        {
            // VIP用户没有限制
            return true;
        }

        int segmentSize = 10; // 每段包含10个帧

        // 计算段落的起始和结束帧（包含）
        int segmentIndex;
        int segmentStartFrame;
        int segmentEndFrame;

        if (frameToAdd <= segmentSize)
        {
            // 第一段特殊处理（帧0-10）
            segmentIndex = 0;
            segmentStartFrame = 0;
            segmentEndFrame = segmentSize;
        }
        else
        {
            // 其他段
            segmentIndex = (frameToAdd - 1) / segmentSize;
            segmentStartFrame = segmentIndex * segmentSize + 1;
            segmentEndFrame = segmentStartFrame + segmentSize - 1;
        }
        
        // 统计该段内非空关键帧的数量（isEmptySlot == 0）
        int nonEmptyKeyFrameCount = oriData.animFrames
            .Count(frame => frame.frame >= segmentStartFrame && frame.frame <= segmentEndFrame && frame.isEmptySlot == 0);

        // 判断是否可以添加新的关键帧
        bool canAdd = nonEmptyKeyFrameCount < MaxFreeKeyFramesPerSegment;
        if (!canAdd)
        {
            var joinVipType = new List<JoinVipType>(){JoinVipType.VIP_Multi_Frame};
            var joinVipTitle = "您正在使用的VIP功能：" + " 每秒超5个关键帧";
            if (joinVipType.Count > 0)
            {
                UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, joinVipTitle, joinVipType);
            }
            Debug.LogWarning($"非VIP用户在帧区间[{segmentStartFrame}-{segmentEndFrame}]内已达到5个非空关键帧的限制。");
        }
        return canAdd;
    }
    
    public bool CanChooseLoopAnim()
    {
        if (IsVipUser())
        {
            // VIP用户没有限制
            return true;
        }
        
        var joinVipType = new List<JoinVipType>(){JoinVipType.VIP_Loop};
        var joinVipTitle = "您正在使用的VIP功能：" + " 循环动画发布";
        if (joinVipType.Count > 0)
        {
            UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, joinVipTitle, joinVipType);
        }

        return false;
    }

    public bool CheckUgcAnimCanPublish(AnimInfo animInfo)
    {
        List<int> vipTrackIds = new List<int>() { 1, 2 };
        int multiPropCount = 1;
        int multiBindCount = 1;
        if (IsVipUser())
        {
            vipTrackIds = new List<int>();
            multiPropCount = 5;
            multiBindCount = 100;
        }
        
        var joinVipType = new List<JoinVipType>();
        var joinVipTitle = "您正在使用的VIP功能：";
        
        var propList = animInfo.propList;
        if (propList != null && propList.Count > 0)
        {
            bool isVipBind = false;
            bool isVipProp = false;
            foreach (var propData in propList)
            {
                if (!string.IsNullOrEmpty(propData.id))
                {
                    if (propData.index > multiPropCount)
                    {
                        isVipProp = true;
                    }

                    if (propData.bindIndex > multiBindCount)
                    {
                        isVipBind = true;
                    }
                }
            }
            if (isVipProp)
            {
                joinVipTitle += "更多动作道具";
                joinVipType.Add(JoinVipType.VIP_Multi_Prop);
            }

            if (isVipBind)
            {
                joinVipTitle += " 更多动作挂点 ";
                joinVipType.Add(JoinVipType.VIP_Multi_Bind);
            }
        }
        
        var animBgmTrackInfos = animInfo.animBgmTrackInfos;
        bool isVipTrack = false;
        if (animBgmTrackInfos != null)
        {
            foreach (var trackInfo in animBgmTrackInfos)
            {
                if (vipTrackIds.Contains(trackInfo.trackId) && trackInfo.animMusicList != null && trackInfo.animMusicList.Count > 0)
                {
                    isVipTrack = true;
                }
            }
        }
        
        if (isVipTrack)
        {
            joinVipTitle += " 动作多音轨";
            joinVipType.Add(JoinVipType.VIP_Multi_Audiotrack);
        }

        if (joinVipType.Count > 0)
        {
            UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, joinVipTitle, joinVipType);
            return false;
        }
        return true;
    }

    public bool CanAddMultiAudio(int targetTrackId, List<AnimBgmTrackInfo> animBgmTrackInfos)
    {
        int maxAudiosPerTrack = 3;
        if (IsVipUser())
            maxAudiosPerTrack = 10;

        bool canAdd = false;
        var targetTrackInfo = animBgmTrackInfos.Find(x => x.trackId == targetTrackId);
        if (targetTrackInfo != null && targetTrackInfo.animMusicList !=null)
        {
            canAdd = targetTrackInfo.animMusicList.Count < maxAudiosPerTrack;
        }
        
        if (!canAdd)
        {
            var joinVipType = new List<JoinVipType>(){JoinVipType.VIP_Multi_Clip};
            var joinVipTitle = "您正在使用的VIP功能：" + "超3个动作音频添加";
            if (joinVipType.Count > 0)
            {
                UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, joinVipTitle, joinVipType);
            }
            LoggerUtils.LogError("非VIP用户无法制作超过5秒的动画。");
        }
        return canAdd;
    }

    public bool CanAddVipTone(UgcAnimBgmConfig config)
    {
        if (config.isVip == 0)
            return true;
        
        if (IsVipUser())
            return true;
        
        var joinVipType = new List<JoinVipType>(){JoinVipType.VIP_Multi_Frame};
        var joinVipTitle = "您正在使用的VIP功能：" + "VIP音色-" + config.toneName;
        if (joinVipType.Count > 0)
        {
            UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, joinVipTitle, joinVipType);
        }
        return false;
    }

    public bool CanUseVipAnim(PoseInfo poseInfo)
    {
        if (IsVipUser() || poseInfo?.isVip == 0)
            return true;
        
        var joinVipType = new List<JoinVipType>(){JoinVipType.VIP_Pose};
        var joinVipTitle = "您正在使用的VIP功能：" + "VIP姿势-" + poseInfo?.name;
        if (joinVipType.Count > 0)
        {
            UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, joinVipTitle, joinVipType);
        }

        return false;
    }
}
