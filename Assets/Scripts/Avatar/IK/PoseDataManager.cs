
using System;
using System.Collections.Generic;
using GameData.BaseInfo;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;

namespace BUD.AnimPose
{
     public enum IKPart
     {
          LeftHand = 0,
          RightHand,
          LeftShoulder,
          RightShoulder,
          LeftFoot,
          RightFoot,
          LeftThigh,
          RightThigh,
          Head,
          Body,
     }

     public struct PoseJointTransform
     {
          public Vector3 pos;
          public Quaternion rot;
     }

     public enum IKPetPart
     {
          LeftHand = 0,
          RightHand,
          LeftFoot,
          RightFoot,
          Head,
     }

     public enum PoseImageType
     {
          Current,
          WhiteBody
     }


     public enum BindNodePart
     {
          None = 0,
          RightHand = 1,
          LeftHand = 2,
          Head = 3,
          Back = 4,
          Waist = 5,
          PetBody = 6,
          PetHead = 7,
          PetRightHand = 8,
          PetLeftHand = 9,
     }

     public enum RoleType
     {
          Avatar = 1,
          Pet = 2,
          FullBody = 3
     }
     
     public enum EnterPanelMode
     {
          Standard,
          AnimEnter
     }

     [Serializable]
     public class PartKeyframeData
     {
          public int partType;
          public Vec3 pos;
          public Quaternion4 rot;
     }


     [Serializable]
     public class ItemKeyFrameData
     {
          public int index;//素材道具列表索引
          public int bindIndex;//绑定位置,对应PoseModeConfig表中BindIndexs字段的索引
          public int state;//0:场景可见 1：场景不可见
          public Vec3 pos;
          public Vec3 sca;
          public Quaternion4 rot;
     }
     
     public class BatchPropRep : HttpPageBaseData
     {
          public List<PropInfoData> propList;
     }

     public class PropInfoData
     {
          public PropInfo propInfo;
     }



     [Serializable]
     public class RoleKeyframeData
     {
          public RoleType roleType;
          public List<PartKeyframeData> parts;
          public Vec3 pos;
          public Vec3 sca;
          public Quaternion4 rot;
     }

     [Serializable]
     public class KeyFrameData
     {
          //从0开始，与Index一致
          [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
          public int frame;
          [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
          public int isEmptySlot;
          /// <summary>
          /// 从左到右的顺序
          /// </summary>
          public List<RoleKeyframeData> keyFrame = new List<RoleKeyframeData>();
          public List<ItemKeyFrameData> items;
     }

     [Serializable]
     public class AnimFrameData
     {
          public int framefrequency = 5;
          public List<KeyFrameData> animFrames = new List<KeyFrameData>();
          public List<AnimBgmTrackInfo> animBgmTrackInfos = new List<AnimBgmTrackInfo>();
     }

     [Serializable]
     public class RoleKeyframeEntity
     {
          public int roleType;
          public Dictionary<IKPart, Transform> parts;
     }

     public class KeyFrameEntity
     {
          public List<RoleKeyframeEntity> roleEntitys;
     }

     public class PoseDataManager
     {
          public static Dictionary<BindNodePart, string> bindNodePaths = new()
          {
               {
                    BindNodePart.None,
                    ""
               }, {
                    BindNodePart.Head,
                    "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head/anim_hat/anim_hat_01_x"
               },{
                    BindNodePart.LeftHand,
                    "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm/Bip001 L Hand/anim_prop_l"
               },{
                    BindNodePart.RightHand,
                    "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand/anim_prop_r"
               },{
                    BindNodePart.Back,
                    "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/anim_back"
               },{
                    BindNodePart.Waist,
                    "Bip001/Bip001 Pelvis/Bip001 Spine/anim_waist/anim_waist_x"
               },{
                    BindNodePart.PetBody,
                    "Pet001 Root/Pet001 Spine/Pet001 Spine1/anim_pet_back_01/anim_pet_back_02"
               }, {
                    BindNodePart.PetHead,
                    "Pet001 Root/Pet001 Spine/Pet001 Spine1/pet001 Head/anim_pet_hat_01/anim_pet_hat_02"
               },{
                    BindNodePart.PetLeftHand,
                    "Pet001 Root/Pet001 Spine/Pet001 Spine1/Pet001 Spine2/Pet001 L UppArm/Pet001 L ForeArm/anim_prop_l"
               },{
                    BindNodePart.PetRightHand,
                    "Pet001 Root/Pet001 Spine/Pet001 Spine1/Pet001 Spine2/Pet001 R UppArm/Pet001 R ForeArm/anim_prop_r"
               }
          };
          private AnimFrameData animData;

          public void AddKeyFrameData(KeyFrameData data)
          {
               animData.animFrames.Add(data);
          }

          public void SetAnimFrameData(AnimFrameData data)
          {
               animData = data;
          }

          public AnimFrameData GetAnimFrameData()
          {
               return animData;
          }
          
          public void RemoveKeyFrameData(int frameNumber)
          {
               if (frameNumber < animData.animFrames.Count)
               {
                    animData.animFrames.RemoveAt(frameNumber);
               }
          }

          public List<KeyFrameData> GetKeyFrameList()
          {
               return animData.animFrames;
          }
          
          public List<AnimBgmTrackInfo> GetBgmTrackInfo()
          {
               return animData?.animBgmTrackInfos;
          }

          //frame从0开始
          public int GetCurValidFrameNumber(int frame)
          {
               if (frame >= animData.animFrames.Count)
               {
                    LoggerUtils.LogError("frame is over animData.animFrames.Count");
                    return 0;
               }

               for (int i = frame; i >= 0; i--)
               {
                    if (animData.animFrames[i].keyFrame != null && animData.animFrames[i].keyFrame.Count != 0)
                    {
                         return i;
                    }
               }

               return 0;
          }

          public int GetNextValidFrameNumber(int frame)
          {
               if (frame >= animData.animFrames.Count)
               {
                    LoggerUtils.LogError("frame is over animData.animFrames.Count");
                    return 0;
               }

               for (int i = frame + 1; i < animData.animFrames.Count; i++)
               {
                    if (animData.animFrames[i].keyFrame != null && animData.animFrames[i].keyFrame.Count != 0)
                    {
                         return i;
                    }
               }

               return 0;
          }

        

          public int GetKeyFrameCount()
          {
               return animData.animFrames.Count;
          }

     }
}