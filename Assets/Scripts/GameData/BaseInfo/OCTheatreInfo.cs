using System;
using System.Collections.Generic;
using GameData.Base;
using GameData.TheatreData;
using Newtonsoft.Json;
using Pb.Theatre;
using UnityEngine;

namespace GameData.BaseInfo
{
    public class OCTheatreInfo : UgcBaseInfo
    {
        public List<OCTheatreAvatarOc> avatarList; //所有演员列表
        public List<string> allUseEmote; //剧中使用的表情列表 pgc和ugc id都有可能
        public List<string> allSoundEffects; //剧中使用的音效列表 ugc id
        public PaymentInfo paymentInfo;
        public int isBan;
        public int isDelete;
        public int sectionCount; //剧本段落数量
        public int optionCount; //剧本选项数量
        public int textCount; //剧本文本数量

        //不参与序列化
        [JsonIgnore]
        public POCTheatreDetailInfo detailInfoPb; //所有剧本数据
        
        public OCTDetailInfoRuntime LoadDetailInfo(byte[] data){
            detailInfoPb = TheatrePbDataTool.ParseTheatrePb(data);
            return new OCTDetailInfoRuntime(id, detailInfoPb);
        }

        public OCTDetailInfoRuntime GetDetailInfoRuntime(){
            if (detailInfoPb == null) return null;
            return new OCTDetailInfoRuntime(id, detailInfoPb);
        }

    }

    //整个剧场的的剧本的Runtime数据
    public class OCTDetailInfoRuntime
    {
        public string theatreID;
        public Dictionary<int, OCTheatreSection> sections;
        public List<OCTheatreAvatarOc> allAvatars;
        public List<string> allBackgrounds;
        public string bgMusic; //整场BGM的URL（UGC音频）

        public OCTDetailInfoRuntime(string theatreID, POCTheatreDetailInfo detailInfoPb){
            this.theatreID = theatreID;
            sections = new Dictionary<int, OCTheatreSection>();
            allAvatars = new List<OCTheatreAvatarOc>();
            allBackgrounds = new List<string>();
            if (detailInfoPb == null) return;

            bgMusic = detailInfoPb.BgMusic ?? string.Empty;

            if (detailInfoPb.AllBackgrounds != null)
            {
                allBackgrounds.AddRange(detailInfoPb.AllBackgrounds);
            }

            if (detailInfoPb.Sections == null) return;

            foreach (var pbSection in detailInfoPb.Sections)
            {
                if (pbSection == null) continue;

                var runtimeSection = new OCTheatreSection
                {
                    sectionIndex = pbSection.SectionIndex,
                    nextIndex = pbSection.NextIndex,
                    avatarID = pbSection.AvatarId,
                    avatarType = pbSection.AvatarType,
                    backgroundIndex = pbSection.BackgroundUrl,
                    backgroundURL = ResolveBackgroundUrl(pbSection.BackgroundUrl, allBackgrounds),
                    dialogues = new List<OCTheatreDialogue>(),
                    audio = ConvertAudio(pbSection.Audio),
                    dubbing = ConvertAudio(pbSection.Dubbing),
                    emote = ConvertEmote(pbSection.Emote)
                };

                if (pbSection.Dialogues != null)
                {
                    foreach (var pbDialogue in pbSection.Dialogues)
                    {
                        if (pbDialogue == null) continue;
                        var runtimeDialogue = new OCTheatreDialogue
                        {
                            type = pbDialogue.Type,
                            text = pbDialogue.Text,
                            options = new List<OCTheatreOption>()
                        };

                        if (pbDialogue.Options != null)
                        {
                            foreach (var pbOption in pbDialogue.Options)
                            {
                                if (pbOption == null) continue;
                                runtimeDialogue.options.Add(new OCTheatreOption
                                {
                                    text = pbOption.Text,
                                    jumpIndex = pbOption.JumpIndex
                                });
                            }
                        }

                        runtimeSection.dialogues.Add(runtimeDialogue);
                    }
                }

                sections[runtimeSection.sectionIndex] = runtimeSection;
            }

            if (detailInfoPb.AllAvatars != null)
            {
                foreach (var pbAvatar in detailInfoPb.AllAvatars)
                {
                    if (pbAvatar == null) continue;
                    allAvatars.Add(new OCTheatreAvatarOc
                    {
                        playerId = pbAvatar.PlayerId,
                        avatarName = pbAvatar.AvatarName,
                        avatarURL = pbAvatar.AvatarUrl,
                        clothesIndex = pbAvatar.ClothesIndex
                    });
                }
            }
        }

        public bool TryGetSection(int sectionIndex, out OCTheatreSection section)
        {
            section = null;
            return sections != null && sections.TryGetValue(sectionIndex, out section);
        }

        public OCTheatreSection GetSection(int sectionIndex)
        {
            TryGetSection(sectionIndex, out var section);
            return section;
        }

        private static OCTheatreAudio ConvertAudio(POCTheatreAudio pbAudio)
        {
            if (pbAudio == null) return null;
            return new OCTheatreAudio
            {
                type = pbAudio.Type,
                audioID = pbAudio.AudioId,
                audioURL = pbAudio.AudioUrl,
                triggerTime = pbAudio.TriggerTime,
                triggerTimeEnd = pbAudio.TriggerTimeEnd,
                duration = pbAudio.Duration,
                triggerType = pbAudio.TriggerType,
                isLoop = pbAudio.IsLoop
            };
        }

        private static OCTheatreEmote ConvertEmote(POCTheatreEmote pbEmote)
        {
            if (pbEmote == null) return null;
            var runtimeEmote = new OCTheatreEmote
            {
                emoteID = pbEmote.EmoteId ?? string.Empty,
                triggerTime = pbEmote.TriggerTime,
                emotePlayers = new List<OCTheatreAvatarOc>(),
                triggerType = pbEmote.TriggerType
            };

            runtimeEmote.customPosition = pbEmote.CustomPosition != null
                ? new Vector3(pbEmote.CustomPosition.X, pbEmote.CustomPosition.Y, 0f)
                : Vector3.zero;
            runtimeEmote.customRotation = pbEmote.CustomRotation != null
                ? new Vector3(pbEmote.CustomRotation.X, pbEmote.CustomRotation.Y, 0f)
                : Vector3.zero;
            runtimeEmote.scale = pbEmote.Scale;

            if (pbEmote.EmotePlayers != null)
            {
                foreach (var pbAvatar in pbEmote.EmotePlayers)
                {
                    if (pbAvatar == null) continue;
                    runtimeEmote.emotePlayers.Add(new OCTheatreAvatarOc
                    {
                        playerId = pbAvatar.PlayerId,
                        avatarName = pbAvatar.AvatarName,
                        avatarURL = pbAvatar.AvatarUrl,
                        clothesIndex = pbAvatar.ClothesIndex
                    });
                }
            }

            return runtimeEmote;
        }

        private static string ResolveBackgroundUrl(int backgroundIndex, List<string> allBackgrounds)
        {
            if (allBackgrounds == null || allBackgrounds.Count == 0) return string.Empty;
            // backgroundIndex is 1-based (0 = no background, 1 = allBackgrounds[0])
            if (backgroundIndex <= 0 || backgroundIndex > allBackgrounds.Count) return string.Empty;
            return allBackgrounds[backgroundIndex - 1] ?? string.Empty;
        }
    }

    //一个对话段落，包含多个OCTheatreDialogue
    public class OCTheatreSection
    {
        public int sectionIndex; //段落索引
        public int nextIndex; //下一个段落的索引, 0表示结束
        public string avatarID; //这个会话角色id
        public string avatarType; //这个会话角色指定立绘种类
        public int backgroundIndex; //这个会话背景url在allBackgrounds中的索引
        public string backgroundURL; //这个会话背景url
        public List<OCTheatreDialogue> dialogues;
        public OCTheatreAudio audio;
        public OCTheatreAudio dubbing;
        public OCTheatreEmote emote;
    }

    //单独的一句话，包括是否是选项，有没有选择跳转
    public class OCTheatreDialogue{
        public int type; //1:对话 2:选项
        public string text; //对话内容
        public List<OCTheatreOption> options; //选项内容
    }

    public class OCTheatreOption{
        public string text; //选项内容
        public int jumpIndex; //跳转的对话id
    }

    public class OCTheatreAudio{
        public int type; //1:商城音效 2:上传音效
        public string audioID; //音频id
        public string audioURL; //音频url
        public int triggerTime; //音频开始触发时机 (0=段落开始, -1=段落结尾, >0=第N句对话)
        public int triggerTimeEnd; //音频停止时机: 在第N句对话结束时停止 (0=不主动停止)
        public float duration; //音频持续时间
        public int triggerType; //触发方式: 0=对话开始前 1=对话文本播放完成后 (仅影响开始)
        public int isLoop; //是否循环: 0=不循环 1=循环
    }

    public class OCTheatreEmote{
        public string emoteID; //表情id
        public int triggerTime; //表情触发时机
        public List<OCTheatreAvatarOc> emotePlayers; //表情演绎玩家id列表
        public int triggerType; //触发方式: 0=对话开始前 1=对话文本播放完成后
        public Vector3 customPosition; // 镜头 XY 偏移 (0=默认)
        public Vector3 customRotation; // 角色 euler XY 旋转偏移 (0=默认)
        public float scale;            // 镜头 Z 偏移，即远近 (0=默认)
    }

    public class OCTheatreAvatarOc{
        public string playerId; //表演用的玩家id
        public string avatarName; //角色名字
        public string avatarURL; //头像url
        public int clothesIndex; //当前衣服是角色卡中的第几套衣柜索引

        public bool IsEqual(OCTheatreAvatarOc other){
            return playerId == other.playerId 
            && avatarName == other.avatarName 
            && avatarURL == other.avatarURL 
            && clothesIndex == other.clothesIndex
            ;
        }
    }
}