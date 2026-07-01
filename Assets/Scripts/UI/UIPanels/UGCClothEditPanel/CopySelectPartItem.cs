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

        /// <summary>
        /// 初始化部件选择项。overrideMaskTex 不为空时直接使用该贴图作为描边遮罩（盒子场景专用），
        /// 否则回退到 ugcData 配置表路径加载。
        /// </summary>
        public void Init(RenderTexture tex, int partId, Action<int> onPartSelect, UgcPartData data, bool isCharacter, Texture overrideMaskTex = null)
        {
            partImage.texture = tex;
            btn.onClick.AddListener(() => onPartSelect?.Invoke(partId));
            maskImage.enabled = false;

            if (overrideMaskTex != null)
            {
                maskImage.enabled = true;
                maskImage.texture = overrideMaskTex;
            }
            else if (data != null)
            {
                var path = (isCharacter ? GameConsts.ClothesAssetDir : GameConsts.PetClothesAssetDir) + data.maskSpriteName;
                var targetTex = Loader.Load<Texture>(path, gameObject);
                maskImage.enabled = true;
                maskImage.texture = targetTex;
            }
        }
    }
}