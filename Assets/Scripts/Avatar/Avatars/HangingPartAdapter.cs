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

namespace Game.Avatar { 

    public abstract class HangingPartAdapter : StandardPartAdapter
{
    protected abstract string hanging_path { get; set; }
    protected GameObject hangingBone;

    public virtual GameObject curPart
    {
        get;
        set;
    }

    protected GameObject nodeParent;
    protected AssetWrapper<GameObject> cWarpper;
    public HangingPartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
    {
        hangingBone = avatar.transform.Find(hanging_path).gameObject;
    }


    public override void Move(Vector3 pos)
    {
        hangingBone.transform.localPosition = pos;
    }

    public override void Rotate(Vector3 rot)
    {

        hangingBone.transform.localRotation = Quaternion.Euler(rot);
        LoggerUtils.Log("Rotate:" + rot + "," + hangingBone.transform.localEulerAngles);

    }

    public override void Scale(Vector3 sca)
    {
        hangingBone.transform.localScale = sca;
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
                //action?.Invoke();
            }
            else
            {
                //action?.Invoke();
            }
        });
        action?.Invoke();
    }


    public override void TakeOff()
    {
        if (curPart != null)
        {
            curPart.transform.SetParent(null);
            GameObject.Destroy(curPart);
            curPart = null;
        }

        if (cWarpper != null)
        {
            cWarpper = null;
        }
    }
}

}