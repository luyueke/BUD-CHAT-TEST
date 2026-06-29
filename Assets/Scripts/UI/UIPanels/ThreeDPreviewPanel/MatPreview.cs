using System;
using System.IO;
using Game.Base;
using Game.ECS;
using UnityEngine;
using Game.Props.PropsManagers;
using GameData.Base;
using GameData.BaseInfo;
using GameData.MapData;
using GameData.UGCData;
using UI;

public class MatPreview : MonoBehaviour
{
    public PreviewCameraHandler previewCameraHandler;
    public Camera previewCamera;
    public MeshRenderer modelMaterial;
    private string curPreviewMetaUrl;
    // private GameUgcMatManager gumMng;

    private void Awake()
    {
        // gumMng = new GameUgcMatManager();
    }

    public void StartPreview(MaterialInfo ugcInfo)
    {
        this.gameObject.SetActive(true);
        var url = ugcInfo.materialUrl;
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(previewCamera.transform);
        if (ugcInfo.ugcStyle == (int) UgcShaderStyle.Anime)
        {
            var wrapper = Loader.Load<Material>("Assets/Arts/Game/BaseMatMaterial/AnimeStyleMatte.mat");
            var styleMaterial = wrapper.RetainAsset(this.gameObject);
            modelMaterial.material = styleMaterial;
        }
        
        GameUgcMatManager.Inst.LoadTextureAsync(url,ugcInfo.ugcStyle,this.gameObject, (ugcStyle,tex) =>
        {
            if (tex != null)
            {
                modelMaterial.material.SetTexture("_BaseMap", tex);
                var item = modelMaterial.transform;
                item.localPosition = new Vector3(0, 0, 20);
                item.localEulerAngles = Vector3.zero;
                item.localScale = Vector3.one * 5;
                modelMaterial.gameObject.SetActive(true);
                
                previewCameraHandler.SetTarget(item.gameObject);
                previewCameraHandler.SetFocus();
            }
        });
        // gumMng.DownloadTexture(url, (cacheData) =>
        // {
        //     if (cacheData == null)
        //     {
        //         LoggerUtils.LogError($"加载材质：{url}有问题");
        //         return;
        //     }
        //
        //     try
        //     {
        //         var texture = new Texture2D(0, 0);
        //         texture.LoadImage(File.ReadAllBytes(Path.Combine(GameUgcMatManager.CachePath, cacheData.LocalFileName)));
        //         modelMaterial.material.SetTexture("_BaseMap", texture);
        //         var item = modelMaterial.transform;
        //         item.localPosition = new Vector3(0, 0, 20);
        //         item.localEulerAngles = Vector3.zero;
        //         item.localScale = Vector3.one * 5;
        //         modelMaterial.gameObject.SetActive(true);
        //         
        //         previewCameraHandler.SetTarget(item.gameObject);
        //         previewCameraHandler.SetFocus();
        //     }
        //     catch (Exception e)
        //     {
        //         LoggerUtils.LogError("加载材质有问题:" + e.Message + ", e:" + e.StackTrace);
        //     }
        // });
    }
}
