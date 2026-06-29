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
    public class InstrumentPartAdapter : HangingPartAdapter
    {
        protected override string hanging_path { get; set; } = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/instrument_back";
        public InstrumentPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            nodeParent = hangingBone;
        }

        public override void AddEffect()
        {
        }

        public override void RemoveEffect()
        {
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
            LoadRes<GameObject>(bData.texDir + bData.prefabName + prefabExt, (isSuc, warpper) =>
            {
                if (isSuc && warpper != null && curPartId == id && avatar != null)
                {
                    TakeOff();
                    cWarpper = warpper;
                    var bagPrefab = warpper.RetainAsset(avatar);
                    var bagNode = bagPrefab.transform.Find(hanging_path);
                    var nodeMeshRenderer = GetComponent<Renderer>(bagNode.gameObject);
                    curPart = GameObject.Instantiate(nodeMeshRenderer.gameObject, nodeParent.transform);
                    lod?.SetLodMesh(curPart);
                    UpdateBones(curPart);
                    ChangeColor(curColor);
                    action?.Invoke();
                    var bev = avatar.GetComponentInChildren<PlayerHoldBehaviour>();
                    if (bev != null)
                    {
                        if (bev.curInstrumentState == MusicalHoldState.Play)
                        {
                            bev.FouceSetToHand();
                        }
                    }
                }
                else
                {
                    action?.Invoke();
                }
            });


        }
    }
}