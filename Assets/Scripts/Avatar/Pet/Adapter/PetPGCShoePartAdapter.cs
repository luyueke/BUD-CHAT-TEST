using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game.Avatar;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Pet {
    public class PetPGCShoePartAdapter : PetBonePartAdapter {

        private List<GameObject> shoeParts = new List<GameObject>();

        public PetPGCShoePartAdapter(GameObject _avatar, Dictionary<string, Transform> bones) : base(_avatar, bones) {
        }


        public override void PutOn(string id, Action action) {
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
                    List<SkinnedMeshRenderer> meshNodes = GetComponents<SkinnedMeshRenderer>(partPrefab);
                    foreach (var meshNode in meshNodes) {
                        var tmpPart = Object.Instantiate(meshNode.gameObject, partParent);

                        lod?.SetLodMesh(tmpPart);
                        UpdateBones(tmpPart);
                        shoeParts.Add(tmpPart);
                    }
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

        public override void TakeOff() {
            foreach (var shoePart in shoeParts) {
                shoePart.transform.SetParent(null);
                GameObject.Destroy(shoePart);
            }
            shoeParts.Clear();
            cWarpper = null;
        }
    }
}
