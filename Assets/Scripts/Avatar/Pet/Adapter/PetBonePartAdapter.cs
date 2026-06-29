using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game.Avatar;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Pet {
    public class PetBonePartAdapter : BonePartAdapter {


        protected virtual string PartPath => "";
        protected Transform partParent;

        public PetBonePartAdapter(GameObject avatar, Dictionary<string, Transform> bones) : base(avatar, bones)
        {
            partParent = avatar.transform;
        }

        public override void PutOn(string id, Action action)
        {
            curPartId = id;
            var bData = Es.DataTables.GetPetAvatarCommonData(id);
            if (bData == null)
            {
                action?.Invoke();
                return;
            }
            Stopwatch watch = new Stopwatch();
            watch.Start();
            LoadRes<GameObject>(bData.texDir + bData.prefabName + prefabExt, (isSuc, warpper) =>
            {
                watch.Stop();
                //避免频繁切换异常
                if (isSuc && warpper != null && id == curPartId && avatar != null)
                {
                    TakeOff();
                    cWarpper = warpper;
                    var partPrefab = warpper.RetainAsset(avatar);
                    SkinnedMeshRenderer meshNode = null;
                    if (string.IsNullOrEmpty(PartPath)) {
                        meshNode = GetComponent<SkinnedMeshRenderer>(partPrefab);
                    } else {
                        var tmpPartPath = GameObjectEx.FindChildByName(partPrefab, PartPath);
                        meshNode =  GetComponent<SkinnedMeshRenderer>(tmpPartPath.gameObject);
                    }
                    curPart = Object.Instantiate(meshNode.gameObject, partParent);
                    lod?.SetLodMesh(curPart);
                    UpdateBones(curPart);
                    PutOnFinish();
                    ChangeColor(curColor);
                    action?.Invoke();
                }
                else
                {
                    action?.Invoke();
                }
            });
        }
    }
}
