using System.Collections.Generic;
using System.Text;
using Game.Base;
using Game.ECS;
using GameData.UGCData;
using Newtonsoft.Json;
using UnityEngine;
using Game.Props.PropsManagers;
using GameData.Base;
using GameData.MapData;
using UI;
using xasset;

public class ModelPreview : MonoBehaviour
{
    public PreviewCameraHandler previewCameraHandler;
    public Camera previewCamera;
    private GameObject coverObj = null;
    private string curPreviewMetaUrl;
    private bool _isBoxMode = false;
    private readonly List<Texture2D> _boxTextures = new List<Texture2D>();
    private const string BoxModelPath = "Assets/Arts/QiYe/UGCScence/UGCBreedingFarm/UGCBreedingFarm_1/qiye_ugcBreedingFarm_1_3d.prefab";

    private void OnDestroy()
    {
        DestroyPreviewObj();
    }

    public void StartPreview(UgcBaseInfo ugcInfo)
    {
        this.gameObject.SetActive(true);

        DestroyPreviewObj();
        var ugcId = ugcInfo.id;
        this.curPreviewMetaUrl = ugcInfo.metaDataUrl;
        var mUrl = this.curPreviewMetaUrl;
        var propNode = AssetPropNodeManager.Inst.CreateProp(ugcId, mUrl, tmpAsset => {
            previewCameraHandler.SetFocus();
        });
        propNode.transform.SetParent(previewCamera.transform);
        propNode.Reset();
        previewCameraHandler.SetTarget(propNode);
        previewCameraHandler.SetFocus();
    }

    public void StartPreviewBox(CharacterBoxInfo info)
    {
        this.gameObject.SetActive(true);
        DestroyPreviewObj();
        _isBoxMode = true;

        var wrapper = Loader.Load<GameObject>(BoxModelPath);
        if (wrapper == null) return;

        coverObj = wrapper.Instantiate(previewCamera.transform);
        if (coverObj == null) return;
        coverObj.transform.localPosition = Vector3.zero;

        int modelLayer = LayerMask.NameToLayer("Model");
        SetLayerRecursive(coverObj, modelLayer);

        previewCameraHandler.SetTarget(coverObj);
        previewCameraHandler.SetFocus();

        if (string.IsNullOrEmpty(info.metaDataUrl)) return;

        var cachedModel = coverObj;
        var request = Asset.LoadRemoteAssetAsync(info.metaDataUrl);
        if (request == null) return;
        request.completed += _ =>
        {
            if (cachedModel == null || request.result != Request.Result.Success) return;
            var text = Encoding.UTF8.GetString(request.asset);
            if (string.IsNullOrEmpty(text)) return;
            var boxData = JsonConvert.DeserializeObject<UGCBoxSceneData>(text);
            if (boxData != null) ApplyBoxTextures(cachedModel, boxData);
        };
    }

    private void ApplyBoxTextures(GameObject model, UGCBoxSceneData boxData)
    {
        if (boxData?.parts == null || boxData.parts.Count == 0) return;
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        foreach (var part in boxData.parts)
        {
            if (part?.pixels == null || part.pixels.Count == 0) continue;
            int rendererIndex = part.type - 1;
            if (rendererIndex < 0 || rendererIndex >= renderers.Length) continue;
            int canvasSize = part.pixels.Count <= 1024 ? 32 : 64;
            var colors = new Color32[canvasSize * canvasSize];
            foreach (var pixel in part.pixels)
            {
                var pos = DataUtil.DeSerializeVector2Int(pixel.p);
                Color col = DataUtil.DeSerializeColor(pixel.col);
                int idx = pos.y * canvasSize + pos.x;
                if (idx >= 0 && idx < colors.Length) colors[idx] = col;
            }
            var tex = new Texture2D(canvasSize, canvasSize, TextureFormat.RGBA32, false);
            tex.SetPixels32(colors);
            tex.Apply();
            _boxTextures.Add(tex);
            var mat = renderers[rendererIndex].material;
            if (mat.shader.name == "Universal Render Pipeline/Lit")
                mat.SetTexture("_BaseMap", tex);
            else if (mat.shader.name == "bud/patterns_ugc_ARI")
                mat.SetTexture("_patterns_tex", tex);
            else
                mat.SetTexture("_MainTex", tex);
        }
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    private void DestroyPreviewObj()
    {
        foreach (var t in _boxTextures) if (t != null) Destroy(t);
        _boxTextures.Clear();

        if (coverObj != null)
        {
            if (_isBoxMode)
                Destroy(coverObj);
            else
                AssetPropNodeManager.Inst.DestroyProp(coverObj);
            coverObj = null;
        }
        _isBoxMode = false;
    }
}
