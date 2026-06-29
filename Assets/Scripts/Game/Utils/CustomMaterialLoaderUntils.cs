/**
* @ Author: Jun Zhou
* @ Create Time: 2023-07-27 16:48:52
* @ Modified by: Jun Zhou
* @ Modified time: 2023-07-27 16:52:54
* @ Description: 基础材质加载
*/

using System;
using Es;
using UnityEngine;
using System.Collections.Generic;
using Game.Base;
using GameData.UGCData;

namespace Game.Utils
{
    public class CustomMaterialLoaderUntils : GameInstance<CustomMaterialLoaderUntils>, IAutoInit
    {
        public const string Material_PATH = "Assets/Arts/Game/BaseMatMaterial/";
        public const string Texture_PATH = "Assets/Arts/Game/BaseMatTexture/";
        public const float tAlpha = 0.36f;

        public enum MaterialTypeEnum
        {
            Normal, // 通用材质
            Emission, // 发光材质
            Transparent, // 透明材质
        }
        
        
        public enum AnimeTypeEnum
        {
            Default = 0,
            Matte = 1,
            SemiMatte = 2,
            HighGloss = 3
        }

        Dictionary<MaterialTypeEnum, Material> materialCache = new Dictionary<MaterialTypeEnum, Material>();

        public void SetMaterial(Renderer[] renderers, MatDataConfig matData)
        {
            Material mat = null;
            switch ((AnimeTypeEnum) matData.AnimeType)
            {
                case AnimeTypeEnum.Default:
                    if (matData.IsTransparent)
                    {
                        mat = GetMaterial(MaterialTypeEnum.Transparent, "StandardTransparent");
                    } else {
                        mat = GetMaterial(MaterialTypeEnum.Normal, "StandardOpaque");
                    }
                    break;
                case AnimeTypeEnum.Matte:
                    mat = GetAnimeMaterial("AnimeStyleMatte");
                    break;
                case AnimeTypeEnum.SemiMatte:
                    mat = GetAnimeMaterial("AnimeStyleSemiMatte");
                    break;
                case AnimeTypeEnum.HighGloss:
                    mat = GetAnimeMaterial("AnimeStyleHighGloss");
                    break;
            }
            
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material = mat;
            }
        }

        public void SetUGCTexture(Renderer[] renderers, MaterialPropertyBlock mpb, Texture2D tex)
        {
            Material mat = GetMaterial(MaterialTypeEnum.Normal, "StandardOpaque");
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material = mat;
                renderers[i].GetPropertyBlock(mpb);
                mpb.SetTexture("_BaseMap", tex);
                renderers[i].SetPropertyBlock(mpb);
            }
        }

        public void SetTexture(GameObject obj, MaterialPropertyBlock mpb, MatDataConfig matData)
        {
            // 加载贴图
            var tex = XAssetLoaderMgr.Inst.LoadResource<Texture>($"{Texture_PATH}{matData.TexName}_col_TEX.png", obj);
            var normalTex = XAssetLoaderMgr.Inst.LoadResource<Texture>($"{Texture_PATH}{matData.TexName}_nor_TEX.png", obj);

            if (tex != null)
            {
                mpb.SetTexture("_BaseMap", tex);
                // 处理发光材质
                if (matData.IsEmission)
                {
                    mpb.SetTexture("_EmissionMap", tex);
                }
            }

            mpb.SetFloat("_Smoothness", matData.Smoothness);
            mpb.SetFloat("_Metallic", matData.Metallic);

            if (normalTex!=null)
            {
                mpb.SetTexture("_BumpMap", normalTex);
            } else {
                // 主法线贴图不存在时加载兜底贴图；若兜底也为 null，跳过设置避免 ArgumentNullException
                var defNormalMap = XAssetLoaderMgr.Inst.LoadResource<Texture>($"{Texture_PATH}gdgt_base_d_nor_TEX.png", obj);
                if (defNormalMap != null)
                {
                    mpb.SetTexture("_BumpMap", defNormalMap);
                }
            }
        }
        
        public void SetAnimeTexture(GameObject obj, MaterialPropertyBlock mpb, MatDataConfig matData)
        {
            // 加载贴图
            var tex = XAssetLoaderMgr.Inst.LoadResource<Texture>($"{Texture_PATH}{matData.TexName}_col_TEX.png", obj);
            if (tex != null)
            {
                mpb.SetTexture("_BaseMap", tex);
            }
        }
        

        public void SetDefaultTexture(int ugcStyle,GameObject obj, MaterialPropertyBlock mpb, Texture tex)
        {
            if (ugcStyle == (int) UgcShaderStyle.Anime)
            {
                mpb.SetTexture("_BaseMap", tex);
            }
            else
            {
                mpb.SetTexture("_BaseMap", tex);
                mpb.SetTexture("_EmissionMap", tex);
                mpb.SetFloat("_Glossiness", 0);
                mpb.SetFloat("_Metallic", 0);
                // 加载兜底法线贴图；若也为 null，跳过设置避免 ArgumentNullException
                var defNormalMap = XAssetLoaderMgr.Inst.LoadResource<Texture>($"{Texture_PATH}gdgt_base_d_nor_TEX.png", obj);
                if (defNormalMap != null)
                {
                    mpb.SetTexture("_BumpMap", defNormalMap);
                }
            }
        }

        public Material GetDefaultMaterial()
        {
            return GetMaterial(MaterialTypeEnum.Normal, "StandardOpaque");
        }

        Material GetMaterial(MaterialTypeEnum e, string matName)
        {
            if (materialCache.ContainsKey(e))
            {
                return materialCache[e];
            }

            var wrapper = Loader.Load<Material>($"{Material_PATH}{matName}.mat");
            var mat = wrapper.Instantiate();
            mat.enableInstancing = EquipmentSizingGame.IsSupportOpenGLES30();
            materialCache.Add(e, mat);
            return mat;
        }
        
        
        Material GetAnimeMaterial(string matName)
        {
            var wrapper = Loader.Load<Material>($"{Material_PATH}{matName}.mat");
            var mat = wrapper.Instantiate();
            return mat;
        }
        

        public void Init() {

        }
    }
}
