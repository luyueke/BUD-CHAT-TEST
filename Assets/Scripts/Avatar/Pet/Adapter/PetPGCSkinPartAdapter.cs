using System;
using System.Collections.Generic;
using System.IO;
using Game.Avatar;
using UnityEngine;

namespace Game.Pet {


    /// <summary>
    /// PGC肤色适配器
    /// </summary>
    class PetPGCSkinPartAdapter : StandardPartAdapter {
        private static readonly int baseColor = Shader.PropertyToID("_BaseColor");
        private Dictionary<string, SkinnedMeshRenderer> bodyParts = new Dictionary<string, SkinnedMeshRenderer>();
        private string matPath = "Assets/Loadable/Pet/PGCPart/Skins/PGCMat/";
        private List<string> bodyPartNames = new List<string>() {"ugcbody_1_1", "ugcbody_1_2", "ugcface_1_1", "ugcface_1_2", "ugchand_1_1", "ugchand_1_2", "ugcleg_1_1", "ugcleg_1_2"};

        private readonly int baseMapName = Shader.PropertyToID("_BaseMap");
        private Dictionary<string, AssetWrapper<Texture>> wrappers;
        private bool isSetColor = true;
        private Color curColor = Color.white;
        private GameObject ugcBody;
        public PetPGCSkinPartAdapter(GameObject _avatar) : base(_avatar) {
            ugcBody = GameObjectEx.FindChildByName(_avatar, "UgcBody").gameObject;
            foreach (var partName in bodyPartNames) {
                bodyParts.Add(partName, GameObjectEx.FindComponentByName<SkinnedMeshRenderer>(ugcBody, $"pet_{partName}"));
            }
        }

        public override void AddEffect() {

        }

        public override void PutOn(string id, Action action) {
            curPartId = id;
            var bData = Es.DataTables.GetPetAvatarCommonData(id);
            if (bData == null)
            {
                action?.Invoke();
                return;
            }

            isSetColor = bData.setColor;
            List<string> matPaths = new List<string>();
            foreach (string texName in bodyPartNames) {
                matPaths.Add( $"{matPath}pet_{texName}.mat" );
            }
            LoadRes<Material>(matPaths, (isSuccess, wrapperDic) => {
                if (isSuccess)
                {
                    var w  = wrapperDic;
                    foreach (var wrapper in  w) {
                        var fileName = Path.GetFileNameWithoutExtension(wrapper.Key);
                        var partsGo = GameObjectEx.FindChildByName(ugcBody,fileName);
                        partsGo.GetComponent<SkinnedMeshRenderer>().material = wrapper.Value.RetainAsset(avatar);
                    }
                } else {
                    LoggerUtils.LogError("Load Mat Failed:" + id);
                    action?.Invoke();
                    return;
                }

                List<string> paths = new List<string>();
                foreach (string texName in bData.texName) {
                    paths.Add(bData.texDir + texName + texExt);
                }
                LoadRes<Texture>(paths, (isTextureSuccess, textureWrapperDic) => {
                    if (isTextureSuccess && curPartId == id && avatar != null)
                    {
                        TakeOff();
                        wrappers = textureWrapperDic;
                        foreach (var wrapper in wrappers) {

                            var fileName = Path.GetFileNameWithoutExtension(wrapper.Key);
                            var index = fileName.LastIndexOf("_ugc", StringComparison.Ordinal);

                            var partName = fileName.Substring(index + 1);
                            if (bodyParts.TryGetValue(partName,out var bodyPart)) {
                                bodyPart.material.SetTexture(baseMapName, wrapper.Value.RetainAsset(avatar));
                            }
                        }

                        if (!isSetColor) {
                            ResetColor();
                        } else {
                            ChangeColor(curColor);
                        }

                        action?.Invoke();
                    }
                    else
                    {
                        LoggerUtils.LogError("Load Texture Failed:" + id);
                        action?.Invoke();
                    }
                });
            });
        }

        public override void RemoveEffect() {
        }
        //考虑资源释放
        public override void TakeOff() {
            foreach (var bodyPart in bodyParts) {
                bodyPart.Value.material.SetTexture(baseMapName, null);
            }
            wrappers = null;
        }


        public override void ChangeColor(Color col) {
            if (!isSetColor) {
                return;
            }

            curColor = col;
            foreach (var bodyPart in bodyParts) {
                bodyPart.Value.material.SetColor(baseColor, col);
            }
        }

        private void ResetColor() {
            foreach (var bodyPart in bodyParts) {
                bodyPart.Value.material.SetColor(baseColor, Color.white);
            }
        }

    }
}


