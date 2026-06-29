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
    public class HeadPartAdapter : StandardPartAdapter
    {
        protected AssetWrapper<Mesh> cWarpper;
        protected AssetWrapper<Mesh> ugcWarpper;
        private SkinnedMeshRenderer patternRenderer;
        private MeshFilter ugcFaceRenderer;
        private Mesh ugcFaceDefaultMesh;


        public override void AddEffect()
        {

        }

        public override void RemoveEffect()
        {

        }

        public HeadPartAdapter(GameObject avatar) : base(avatar)
        {
            var fbx_Face = avatar.transform.Find("body_face").gameObject;
            var ugcFacePath = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 Head/ugc_face";
            var ugc_Face = avatar.transform.Find(ugcFacePath).gameObject;
            patternRenderer = fbx_Face.GetComponent<SkinnedMeshRenderer>();
            ugcFaceRenderer = ugc_Face.GetComponent<MeshFilter>();
            ugcFaceDefaultMesh = ugcFaceRenderer.mesh;
        }

        public override void PutOn(string id, Action action)
        {
            curPartId = id;
            var bData = Es.DataTables.GetAvatarCommonData(id);
            if (bData == null)
            {
                action?.Invoke();
                return;
            }
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var meshs = bData.meshName;
            var headMeshName = meshs[0];
            LoadRes<Mesh>(bData.texDir + headMeshName + meshExt, (isSuc, warpper) =>
            {
                watch.Stop();
                //避免频繁切换异常
                if (isSuc && warpper != null && id == curPartId && avatar != null)
                {
                    cWarpper = warpper;
                    var partPrefab = warpper.RetainAsset(avatar);
                    patternRenderer.sharedMesh = partPrefab;
                    action?.Invoke();
                }
                else
                {
                    action?.Invoke();
                }
            });
            var ugcheadMeshName = meshs[1];
            LoadRes<Mesh>(bData.texDir + ugcheadMeshName + meshExt, (isSuc, warpper) =>
            {
                //避免频繁切换异常
                if (isSuc && warpper != null && id == curPartId && avatar != null)
                {
                    ugcWarpper = warpper;
                    var partPrefab = warpper.RetainAsset(avatar);
                    ugcFaceRenderer.sharedMesh = partPrefab;
                }
            });

        }

        //考虑资源释放
        public override void TakeOff()
        {
            patternRenderer.sharedMesh = null;
            ugcFaceRenderer.sharedMesh = ugcFaceDefaultMesh;
            if (cWarpper != null)
            {
                cWarpper = null;
            }
            if (ugcWarpper != null)
            {
                ugcWarpper = null;
            }

        }
    }



    public class ScarfPartAdapter : BonePartAdapter
    {
        public ScarfPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
        }

    }

}