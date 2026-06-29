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

    public abstract class StandardPartAdapter : PartAdapter
    {
        protected string curPartId = string.Empty;
        protected const string texExt = ".png";
        protected const string prefabExt = ".prefab";
        protected const string meshExt = ".mesh";
        protected GameObject avatar;
        protected Color curColor;
        protected Dictionary<string, Transform> avatarBones;
        protected CharacterLOD lod;
        protected GameObject ugcNode_l;
        protected GameObject ugcNode_r;
        protected GameObject pgcNode_l;
        protected GameObject pgcNode_r;

        protected StandardPartAdapter(GameObject _avatar)
        {
            avatar = _avatar;
            lod = avatar.GetComponent<CharacterLOD>();
        }

        protected StandardPartAdapter(GameObject _avatar, Dictionary<string, Transform> _bones)
        {
            avatar = _avatar;
            lod = avatar.GetComponent<CharacterLOD>();
            avatarBones = _bones;
        }


        protected virtual Material SetPartMaterial(Renderer renderer, int ugcStyle)
        {
            if (ugcStyle == (int)UgcShaderStyle.Anime)
            {
                if (!renderer.material.name.Contains("UgcAnimeStyle"))
                {
                    var animeWrapper = Loader.Load<Material>("Assets/Arts/Game/BaseMatMaterial/UgcAnimeStyle.mat");
                    renderer.material = animeWrapper.Instantiate();
                }
            }
            return renderer.material;
        }


        public void AttachParent(Transform Par)
        {
            avatar.transform.SetParent(Par);
        }

        public override void Move(Vector3 pos)
        {

        }
        public override void Rotate(Vector3 rot)
        {

        }
        public override void Scale(Vector3 sca)
        {

        }

        public override void HVScale(Vector3 sca)
        {

        }

        public override void ChangeColor(Color col)
        {
            curColor = col;
        }

        public override void SetLeftOrRight(int leftOrRight)
        {

        }

        /// <summary>
        /// 和美术约定默认第一个
        /// </summary>
        /// <param name="obj"></param>
        protected GameObject GetPartNode(Transform obj)
        {
            if (obj.childCount == 0)
            {
                return null;
            }

            return obj.GetChild(0).gameObject;
        }

        /// <summary>
        /// 可考虑缓存
        /// </summary>
        /// <param name="node"></param>
        protected void ClearChildNode(Transform node)
        {
            for (int i = node.childCount - 1; i >= 0; i--)
            {
                var childNode = node.GetChild(i);
                if (childNode != null)
                {
                    childNode.SetParent(null);
                    GameObject.Destroy(childNode.gameObject);
                }
            }
        }

        /// <summary>
        /// 获取一级节点中对应Part资源
        /// </summary>
        /// <param name="nodeParent"></param>
        /// <param name="compareStr"></param>
        /// <returns></returns>
        protected T GetComponent<T>(GameObject nodeParent) where T : Component
        {
            if (nodeParent == null)
            {
                return null;
            }

            for (int i = 0; i < nodeParent.transform.childCount; i++)
            {
                var node = nodeParent.transform.GetChild(i);
                if (node.TryGetComponent<T>(out var comp))
                {
                    return comp;
                }
            }
            return null;
        }

        protected List<T> GetComponents<T>(GameObject nodeParent) where T : Component
        {
            if (nodeParent == null)
            {
                return null;
            }

            var list = new List<T>();
            for (int i = 0; i < nodeParent.transform.childCount; i++)
            {
                var node = nodeParent.transform.GetChild(i);
                if (node.TryGetComponent<T>(out var comp))
                {
                    list.Add(comp);
                }
            }
            return list;
        }


        /// <summary>
        /// UGC资源获取对应Part资源
        /// </summary>
        /// <param name="nodeParent"></param>
        /// <param name="compareStr"></param>
        /// <returns></returns>
        protected Transform GetNode(GameObject nodeParent, string nodeName)
        {
            for (int i = 0; i < nodeParent.transform.childCount; i++)
            {
                var node = nodeParent.transform.GetChild(i);
                if (node.name.Equals(nodeName))
                {
                    return node;
                }
            }
            return null;
        }



        protected void UpdateBones(GameObject curPart)
        {
            if (curPart == null)
                return;
            var meshRenderer = curPart.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (meshRenderer != null)
            {
                for (var k = 0; k < meshRenderer.Length; k++)
                {
                    var mRenderer = meshRenderer[k];
                    List<Transform> clothBones = new List<Transform>();
                    for (int i = 0; i < mRenderer.bones.Length; i++)
                    {
                        var bone = mRenderer.bones[i];
                        if (avatarBones != null && avatarBones.ContainsKey(bone.name))
                        {
                            clothBones.Add(avatarBones[bone.name]);
                        }
                    }

                    if (clothBones.Count != 0)
                    {
                        mRenderer.bones = clothBones.ToArray();
                        mRenderer.rootBone = avatarBones[mRenderer.rootBone.name];
                    }
                }

            }
        }

        public override void UGCPutOn(string id, string url, int ugcStyle, Action suc)
        {
            suc?.Invoke();
        }

        public override void PropSkinPutOn(string id, string url, Action suc)
        {

            if (this is IUGCPartAdapter ugcPartAdapter)
            {

                if (ugcPartAdapter.propSkinRoot == null)
                {
                    LoggerUtils.Log("未获取到 3D 素材父节点");
                    suc?.Invoke();
                    return;
                }

                if (curPartId == id)
                {
                    suc?.Invoke();
                    return;
                }

                curPartId = id;
                ugcPartAdapter.curUrl = url;
                var skinObj = PropSkinPartCachePool.Inst.GetSkinPartObj(id, url);
                if (skinObj != null)
                {
                    TakeOff();
                    ugcPartAdapter.curPart = skinObj;
                    ugcPartAdapter.curPart.transform.SetParent(ugcPartAdapter.propSkinRoot.transform);
                    ugcPartAdapter.curPart.Reset();
                    suc?.Invoke();
                }
                else
                {
                    suc?.Invoke();
                }
            }
        }

        //用于控制眼睛动画是否切换UGC
        public void SetUGCEyeBoneShow(string clipName)
        {
            if (ugcNode_l != null && ugcNode_r != null && !string.IsNullOrEmpty(clipName))
            {
                //hack： "eye_"为眨眼动画的前缀，是不需要替换眼睛的，并且眨眼动画代表idle状态用于暂重制
                //部分动画有面部表情单没有眼睛动画，这类动画在动画明前会有明确前缀ugceyeignore
                bool isSwitchUGC = !clipName.Contains("ugceyeignore") && !clipName.Contains("ugcignore");
                pgcNode_l.SetActive(isSwitchUGC);
                pgcNode_r.SetActive(isSwitchUGC);
                ugcNode_l.SetActive(!isSwitchUGC);
                ugcNode_r.SetActive(!isSwitchUGC);
            }
        }

        public override void SetAnchor(Vector3 anchor)
        {
            if (this is IUGCPartAdapter ugcPartAdapter)
            {

                if (ugcPartAdapter.propSkinRoot == null)
                {
                    LoggerUtils.Log("未获取到 3D 素材父节点");
                    return;
                }
                // 没办法直接调用 Game 程序集下脚本，通过节点名字设置锚点

                if (ugcPartAdapter.curPart == null)
                {
                    LoggerUtils.Log("未获取到对应部位");
                    return;
                }

                var anchorRoot = GameObjectEx.FindChildByName(ugcPartAdapter.curPart.gameObject, "AnchorRoot");
                if (anchorRoot != null)
                {
                    var anchorObj = anchorRoot.Find("CombineAsset");
                    if (anchorObj != null)
                    {
                        anchorObj.transform.localPosition = anchor;
                    }
                    else
                    {
                        LoggerUtils.Log("未找到 CombineAsset 节点");
                    }
                }
            }
        }

        public override void ResetCurrentID()
        {
            curPartId = "0";
        }

        public override void Reset()
        {

        }

        private bool useAsyncLoad;
        public override void IsUIOrSelfPlayer(bool flag)
        {
            useAsyncLoad = flag;
        }

        public void LoadRes<T>(string path, Action<bool, AssetWrapper<T>> callback) where T : UnityEngine.Object
        {
            if (useAsyncLoad) Loader.LoadAsyncOrSync(path, callback);
            else Loader.LoadAsync(path, callback);
        }

        public void LoadRes<T>(List<string> paths, Action<bool, Dictionary<string, AssetWrapper<T>>> callback) where T : UnityEngine.Object
        {
            if (useAsyncLoad)
            {
                var result = new Dictionary<string, AssetWrapper<T>>();
                bool isAllSuccess = true;
                foreach (string path in paths)
                {
                    Loader.LoadAsyncOrSync<T>(path, (isSuccess, wrapper) =>
                    {
                        if (!isSuccess)
                        {
                            LoggerUtils.LogError("LoadRes LoadAsyncOrSync Error:", path + "," + wrapper?.request?.error);
                        }
                        isAllSuccess &= isSuccess;
                        result[path] = wrapper;
                        if (result.Count >= paths.Count)
                        {
                            callback?.Invoke(isAllSuccess, result);
                        }
                    });
                }
            }
            else
            {
                var result = new Dictionary<string, AssetWrapper<T>>();
                bool isAllSuccess = true;
                foreach (string path in paths)
                {
                    Loader.LoadAsync<T>(path, (isSuccess, wrapper) =>
                    {
                        if (!isSuccess)
                        {
                            LoggerUtils.LogError("LoadRes LoadAsync Error:", path + "," + wrapper?.request?.error);
                        }
                        isAllSuccess &= isSuccess;
                        result[path] = wrapper;
                        if (result.Count >= paths.Count)
                        {
                            callback?.Invoke(isAllSuccess, result);
                        }
                    });
                }
            }
        }

    }


}