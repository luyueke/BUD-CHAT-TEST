using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BUD.AnimPose;
using Es;
using Game.Config;
using Game.KinematicCharacter;
using Game.Pet;
using GameData.PgcData;
using GameData.UGCData;
using Message;
using Pb.Base;
using RootMotion.FinalIK;
using UnityEngine;
using xasset;
using Debug = UnityEngine.Debug;

namespace Game.Avatar
{

    public class SpecialSkinShoePartAdapter : ShoePartAdapter, SpecialSkinPart
    {
        private string left_foot_path = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 L Thigh/Bip001 L Calf/Bip001 L Foot";
        private string right_foot_path = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 R Thigh/Bip001 R Calf/Bip001 R Foot";

        private string expect_L_prefab = "Bip001 L Toe0";
        private string expect_R_prefab = "Bip001 R Toe0";

        private List<GameObject> effects = new List<GameObject>();

        private AssetWrapper<GameObject> cWrapper;
        public SpecialSkinShoePartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {

        }

        public GameObject GetAniRoot()
        {
            return curPart;
        }

        public override void PutOn(string id, Action action)
        {
            base.PutOn(id, action);
            TakeOffEffect();
            var bData = Es.DataTables.GetAvatarCommonData(id);
            if (bData == null)
            {
                LoggerUtils.LogError("特效创建失败，因为配置拿不到！");
                return;
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();
            var shoeId = id;
            // Debug.LogError($"AvatarController curEffectId={curEffectId},  path={bData.texDir + bData.prefabName} ");
            LoadRes<GameObject>(bData.texDir + bData.prefabName + "_effect.prefab", (isSuc, wrapper) =>
            {
                watch.Stop();
                //避免频繁切换异常
                if (isSuc && wrapper != null && shoeId == id && avatar != null)
                {
                    TakeOffEffect();
                    cWrapper = wrapper;
                    var partPrefab = wrapper.RetainAsset(avatar);
                    
                    // 复制left_foot_path和right_foot_path路径下的对象
                    CopyFootPathObjects(partPrefab, left_foot_path, expect_L_prefab);
                    CopyFootPathObjects(partPrefab, right_foot_path, expect_R_prefab);
                }
            });
            
        }

        public override void TakeOff()
        {
            base.TakeOff();
            RemoveEffect();
        }

        public override void RemoveEffect()
        {
            base.RemoveEffect();
        }

        private void TakeOffEffect()
        {
            foreach (var effect in effects)
            {
                GameObject.Destroy(effect);
            }
            if (cWrapper != null)
            {
                cWrapper = null;
            }
            effects.Clear();
        }

        /// <summary>
        /// 根据路径字符串查找Transform
        /// </summary>
        private Transform FindTransformByPath(Transform root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path))
                return null;

            string[] pathParts = path.Split('/');
            Transform current = root;

            foreach (string part in pathParts)
            {
                if (current == null)
                    return null;

                current = current.Find(part);
                if (current == null)
                    return null;
            }

            return current;
        }

        /// <summary>
        /// 复制指定路径下的对象（排除指定名称的对象）到avatar的相同路径下
        /// </summary>
        private void CopyFootPathObjects(GameObject prefab, string footPath, string excludeName)
        {
            if (prefab == null || avatar == null)
                return;

            // 在预制体中查找指定路径
            Transform prefabFootTransform = FindTransformByPath(prefab.transform, footPath);
            if (prefabFootTransform == null)
            {
                LoggerUtils.LogError($"在预制体中找不到路径: {footPath}");
                return;
            }

            // 在avatar中查找相同路径
            Transform avatarFootTransform = FindTransformByPath(avatar.transform, footPath);
            if (avatarFootTransform == null)
            {
                LoggerUtils.LogError($"在avatar中找不到路径: {footPath}");
                return;
            }

            // 遍历预制体路径下的所有子对象
            for (int i = 0; i < prefabFootTransform.childCount; i++)
            {
                Transform child = prefabFootTransform.GetChild(i);
                
                // 排除指定名称的对象
                if (child.name == excludeName)
                    continue;

                // 复制对象到avatar的相同路径下，保留位置、旋转和缩放信息
                GameObject copiedObject = GameObject.Instantiate(child.gameObject, avatarFootTransform);
                copiedObject.name = child.name;
                
                // 保留本地变换信息（位置、旋转、缩放）
                Transform copiedTransform = copiedObject.transform;
                copiedTransform.localPosition = child.localPosition;
                copiedTransform.localRotation = child.localRotation;
                copiedTransform.localScale = child.localScale;
                
                effects.Add(copiedObject);
            }
        }
    }

}