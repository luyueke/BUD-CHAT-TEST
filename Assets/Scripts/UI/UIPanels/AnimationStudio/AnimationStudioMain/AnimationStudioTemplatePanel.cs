using System;
using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using GameData.PgcData;
using UnityEngine;

namespace Game.AnimationStudio
{
    public class AnimationStudioTemplatePanel : MonoBehaviour
    {
        public AnimStudioTemplateItem ItemPrefab;
        public Transform ItemParent;
        private AnimationStudioType _animationStudioType;

        private List<StudioTempConfig> _templateConfig = new List<StudioTempConfig>();

        public void Init(AnimationStudioType animationStudioType)
        {
            _animationStudioType = animationStudioType;
            _templateConfig.Clear();

            switch (animationStudioType)
            {
                case AnimationStudioType.Animation:
                    InitAnimConfig();
                    break;
                
                case AnimationStudioType.Pose:
                    InitPoseConfig();
                    break;
            }
        }

        public void InitAnimConfig()
        {
            StudioTempConfig SingleEmo = new StudioTempConfig()
            {
                iconName = "UGCAnim_1",
                title = "单人动作",
                animInfo = new AnimInfo()
                {
                     name = "单人动作",
                     animType = (int)EmoteSubType.Single,
                }
            };
            StudioTempConfig DoubleEmo = new StudioTempConfig()
            {
                iconName = "UGCAnim_3",
                title = "双人动作",
                animInfo = new AnimInfo()
                {
                    name = "双人动作",
                    animType = (int)EmoteSubType.Double,
                }
            };
            StudioTempConfig PetSingleEmo = new StudioTempConfig()
            {
                iconName = "UGCAnim_5",
                title = "宠物单人动作",
                animInfo = new AnimInfo()
                {
                    name = "宠物单人动作",
                    animType = (int)EmoteSubType.PetSingle,
                    skinType = 1,
                }
            };
            StudioTempConfig PetDoubleEmo = new StudioTempConfig()
            {
                iconName = "UGCAnim_7",
                title = "宠物双人动作",
                animInfo = new AnimInfo()
                {
                    name = "宠物双人动作",
                    animType = (int)EmoteSubType.PetWithPlayer,
                    skinType = 1,
                }
            };
            
            _templateConfig.Add(SingleEmo);
            _templateConfig.Add(DoubleEmo);
            _templateConfig.Add(PetSingleEmo);
            _templateConfig.Add(PetDoubleEmo);
            
            _templateConfig.ForEach(x =>
            {
                var comp = GameObject.Instantiate(ItemPrefab, ItemParent);
                comp.InitData(_animationStudioType, x);
            });
        }
        
        public void InitPoseConfig()
        {
            StudioTempConfig SingleEmo = new StudioTempConfig()
            {
                iconName = "UGCAnim_1",
                title = "单人姿势",
                poseInfo = new PoseInfo()
                {
                    name = "单人姿势",
                    poseType = (int)EmoteSubType.Single,
                }
            };
            StudioTempConfig DoubleEmo = new StudioTempConfig()
            {
                iconName = "UGCAnim_3",
                title = "双人姿势",
                poseInfo = new PoseInfo()
                {
                    name = "双人姿势",
                    poseType = (int)EmoteSubType.Double,
                }
            };
            StudioTempConfig PetSingleEmo = new StudioTempConfig()
            {
                iconName = "UGCAnim_5",
                title = "宠物单人姿势",
                poseInfo = new PoseInfo()
                {
                    name = "宠物单人姿势",
                    poseType = (int)EmoteSubType.PetSingle,
                    skinType = 1,
                }
            };
            StudioTempConfig PetDoubleEmo = new StudioTempConfig()
            {
                iconName = "UGCAnim_7",
                title = "宠物双人姿势",
                poseInfo = new PoseInfo()
                {
                    name = "宠物双人姿势",//+ DateTime.Now.ToString("yyyy-MM-dd")
                    poseType = (int)EmoteSubType.PetWithPlayer,
                    skinType = 1,
                }
            };
            
            _templateConfig.Add(SingleEmo);
            _templateConfig.Add(DoubleEmo);
            _templateConfig.Add(PetSingleEmo);
            _templateConfig.Add(PetDoubleEmo);
            
            _templateConfig.ForEach(x =>
            {
                var comp = GameObject.Instantiate(ItemPrefab, ItemParent);
                comp.InitData(_animationStudioType, x);
            });
        }
    }

    public class StudioTempConfig
    {
        public string iconName;
        public string title;
        public AnimInfo animInfo;
        public PoseInfo poseInfo;
    }
}
