using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Basic.Utils;
using BUD.AnimPose;
using Es;
using Game.AvatarTool;
using Game.Config;
using Game.KinematicCharacter;
using Game.Pet;
using GameData.BaseInfo;
using GameData.MapData;
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
    public class SpecialSkinPartAdapter : StandardPartAdapter
    {
        private Dictionary<int, PartAdapter> parts = new Dictionary<int, PartAdapter>();

        private PartAdapter curPart;

        protected AssetWrapper<GameObject> cWarpper;
        private GameObject effect;
        protected string curEffectId = string.Empty;

        public SpecialSkinPartAdapter(GameObject Avatar, GameObject head, Dictionary<string, Transform> bonesDic) : base(Avatar)
        {
            parts.Add(UniqueType.GetAvatar(AvatarSubType.Hand), new SpecialSkinHandPartAdapter(Avatar, bonesDic));
            parts.Add(UniqueType.GetAvatar(AvatarSubType.Glove), new GlovePartAdapter(Avatar, bonesDic));
            parts.Add(UniqueType.GetAvatar(AvatarSubType.Clothes), new ClothesPartAdapter(Avatar, bonesDic));
            parts.Add(UniqueType.GetAvatar(AvatarSubType.Hats), new SpecialSkinHatsPartAdapter(Avatar));
            parts.Add(UniqueType.GetAvatar(AvatarSubType.Backpack), new SpecialSkinBackBagPartAdapter(Avatar, bonesDic));
            parts.Add(UniqueType.GetAvatar(AvatarSubType.Effect), new SpecialSkinEffectPartAdapter(Avatar));
            parts.Add(UniqueType.GetAvatar(AvatarSubType.Shoe), new SpecialSkinShoePartAdapter(Avatar, bonesDic));

        }

        public override void AddEffect()
        {
            curPart?.AddEffect();
        }

        public override void ChangeColor(Color col)
        {
            curPart?.ChangeColor(col);
        }

        public override void HVScale(Vector3 sca)
        {
            curPart?.HVScale(sca);
        }

        public override void Move(Vector3 pos)
        {
            curPart?.Move(pos);
        }

        public override void PropSkinPutOn(string id, string url, Action suc)
        {
            var resData = Es.DataTables.GetGameResData(id);
            var partType = UniqueType.GetAvatar((AvatarSubType)resData.SubType);
            if (parts.ContainsKey(partType))
            {
                curPart = parts[partType];
                curPart.PropSkinPutOn(id, url, suc);
            }
        }

        public override void PutOn(string id, Action action)
        {
            var resData = Es.DataTables.GetGameResData(id);
            var partType = UniqueType.GetAvatar((AvatarSubType)resData.SubType);
            if (parts.ContainsKey(partType))
            {
                curEffectId = id;
                if (curPart != null)
                {
                    if (effect != null)
                        GameObject.Destroy(effect);

                    curPart.TakeOff();
                    curPart.ResetCurrentID();
                }
                curPart = parts[partType];
                curPart.PutOn(id, () =>
                {
                    var animCtrl = avatar.GetComponent<PlayerAnimationCtrl>();
                    if (animCtrl) animCtrl.specialAnimRoot = (curPart as SpecialSkinPart)?.GetAniRoot();
                    CreateEffect();
                    action?.Invoke();
                });
            }
        }

        private void CreateEffect()
        {
            var bData = Es.DataTables.GetAvatarCommonData(curEffectId);
            var specialData = Es.DataTables.GetSpecialSkinConfig(curEffectId);
            if (bData == null || specialData == null)
            {
                LoggerUtils.LogError("特效创建失败，因为配置拿不到！");
                return;
            }

            if (specialData.Effect == 0) return;

            Stopwatch watch = new Stopwatch();
            watch.Start();
            var id = curEffectId;

            // Debug.LogError($"AvatarController curEffectId={curEffectId},  path={bData.texDir + bData.prefabName} ");
            LoadRes<GameObject>(bData.texDir + bData.prefabName + "_effect.prefab", (isSuc, warpper) =>
            {
                watch.Stop();
                //避免频繁切换异常
                if (isSuc && warpper != null && id == curEffectId && avatar != null)
                {
                    TakeOffEffect();
                    cWarpper = warpper;
                    var partPrefab = warpper.RetainAsset(avatar);
                    Transform effectRoot = avatar.transform;
                    if (!string.IsNullOrEmpty(specialData.EffectRoot))
                    {
                        effectRoot = avatar.transform.Find(specialData.EffectRoot);
                        if (effectRoot == null)
                        {
                            LoggerUtils.LogError("配置的特效根节点拿不到！:" + specialData.EffectRoot);
                            effectRoot = avatar.transform;
                        }
                    }
                    effect = GameObject.Instantiate(partPrefab, effectRoot);
                    lod?.SetLodMesh(effect);

                    var animCtrl = avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                    if (animCtrl)
                    {
                        animCtrl.specialAnimRoot = effect;
                        // UI 预览模式下，特效(重新)创建时：①把本体特殊皮肤覆盖(此时 dic 已是 preview)应用到 AOC，使本体显示 preview；
                        // ②驱动特效切 preview。这样不依赖各面板各自调用，统一生效；局内 UIPreviewIdleMode=false 不触发，零影响。
                        if (animCtrl.UIPreviewIdleMode)
                        {
                            // 关闭 Animator rebind on enable —— 否则 FittingRoom 装备特殊皮肤时 avatar.SetActive(false→true)
                            // 会触发 Animator OnEnable → rebind 到 controller 默认 state（云的默认 state 是 "idle"），
                            // 把我们随后 Play("preview_idle_exhibit") 排队的 state 直接擦回 idle，用户看到云一直停在 idle。
                            var effectAnim = effect.GetComponentInChildren<Animator>(true);
                            if (effectAnim != null) effectAnim.keepAnimatorStateOnDisable = true;
                            animCtrl.CheckAndOverrideSpecialAnim();
                            animCtrl.DriveSpecialEffectPreviewIdle();
                        }
                    }
                }
            });
        }

        public override void RemoveEffect()
        {
            curPart?.RemoveEffect();
        }

        public override void Reset()
        {
            curPart?.Reset();
        }

        public override void ResetCurrentID()
        {
            curPart?.ResetCurrentID();
            curEffectId = "";
        }

        public override void Rotate(Vector3 rot)
        {
            curPart?.Rotate(rot);
        }

        public override void Scale(Vector3 sca)
        {
            curPart?.Scale(sca);
        }

        public override void SetAnchor(Vector3 anchor)
        {
            curPart?.SetAnchor(anchor);
        }

        public override void SetLeftOrRight(int leftOrRight)
        {
            curPart?.SetLeftOrRight(leftOrRight);
        }

        public override void TakeOff()
        {
            curPart?.TakeOff();

            TakeOffEffect();
        }

        private void TakeOffEffect()
        {
            if (effect != null)
            {
                effect.transform.SetParent(null);
                GameObject.Destroy(effect);
                effect = null;
            }

            if (cWarpper != null)
            {
                cWarpper = null;
            }
        }

        public void ChangeSpecialSkinStatus()
        {
            // Debug.LogError("ChangeSpecialSkinStatus");
            // Debug.LogError("effect: " + effect);
        }

        public override void UGCPutOn(string id, string url, int ugcStyle = 0, Action suc = null)
        {
            var resData = Es.DataTables.GetGameResData(id);
            var partType = UniqueType.GetAvatar((AvatarSubType)resData.SubType);
            if (parts.ContainsKey(partType))
            {
                curPart = parts[partType];
                curPart.UGCPutOn(id, url, ugcStyle, suc);
            }
        }

        /// <summary>
        /// 获取当前 SpecialSkin 实例化出来的对象（不依赖固定节点/PlayerAnimationCtrl.specialAnimRoot）
        /// </summary>
        public GameObject GetCurrentLoadedObj()
        {
            // SpecialSkin 的各子部位适配器实现了 SpecialSkinPart.GetAniRoot()，返回其当前实例
            return (curPart as SpecialSkinPart)?.GetAniRoot();
        }
    }

}