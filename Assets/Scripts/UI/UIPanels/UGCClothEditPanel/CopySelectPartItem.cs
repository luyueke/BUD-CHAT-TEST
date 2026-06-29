using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UGCEditor
{
    public class CopySelectPartItem : MonoBehaviour
    {
        public RawImage partImage;
        public Button btn;
        public RawImage maskImage;

        public void Init(RenderTexture tex, int partId, Action<int> onPartSelect, UgcPartData data,bool isCharacter)
        {
            partImage.texture = tex;
            btn.onClick.AddListener(() => onPartSelect?.Invoke(partId));
            maskImage.enabled = false;
            if (data != null)
            {
                var path = (isCharacter ? GameConsts.ClothesAssetDir : GameConsts.PetClothesAssetDir) + data.maskSpriteName;
                var targetTex = Loader.Load<Texture>(path, gameObject);
                maskImage.enabled = true;
                maskImage.texture = targetTex;
            }
        }
    }
}