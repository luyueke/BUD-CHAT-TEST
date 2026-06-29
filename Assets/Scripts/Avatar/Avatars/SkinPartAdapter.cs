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
    public class SkinPartAdapter : StandardPartAdapter
    {
        private SkinnedMeshRenderer patternRenderer;
        private Renderer bodyRenderer;
        private MeshRenderer partRenderer;
        public SkinPartAdapter(GameObject _avatar) : base(_avatar)
        {
            var fbx_Face = avatar.transform.Find("body_face").gameObject;
            var fbx_Body = avatar.transform.Find("body").gameObject;
            var nose_Node = avatar.transform.Find(BodyPath.BONE_PATH + "/nose/nose_x/body_nose").gameObject;

            patternRenderer = fbx_Face.GetComponent<SkinnedMeshRenderer>();
            bodyRenderer = fbx_Body.GetComponent<SkinnedMeshRenderer>();
            partRenderer = nose_Node.GetComponent<MeshRenderer>();
        }

        public override void ChangeColor(Color col)
        {
            base.ChangeColor(col);
            patternRenderer.material.SetColor("_BaseColor", col);
            // patternRenderer.material.SetColor("_patterns_color", col);
            bodyRenderer.material.SetColor("_BaseColor", col);
            partRenderer.material.SetColor("_BaseColor", col);
        }

        public override void AddEffect()
        {
        }

        public override void RemoveEffect()
        {
        }

        public override void PutOn(string id, Action action)
        {
            action?.Invoke();
        }

        public override void TakeOff()
        {
        }
    }


}