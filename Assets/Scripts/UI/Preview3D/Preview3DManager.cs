using System;
using System.Collections.Generic;
using UI.Preview3D.Base;
using UI.Preview3D.Bean;
using UI.Preview3D.Mono;
using UI.Preview3D.PreviewHandle;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using xasset;
using Object = UnityEngine.Object;

namespace UI.Preview3D
{
    public class Preview3DManager : GlobalInstance<Preview3DManager>, IPreview3D
    {
        private bool init = false;
        private int modelPosOffsetY = 0;

        private Dictionary<Preview3DRawImage, PreviewWrap> _previewWrapDict = new Dictionary<Preview3DRawImage, PreviewWrap>();
        private Dictionary<Type, IPreview3DDataConvertor> _convertors = new Dictionary<Type, IPreview3DDataConvertor>();


        public void Init()
        {
            if (init)
            {
                return;
            }

            init = true;
            InitPreview3D();
        }

        public Preview3DManager()
        {
            Init();
        }

        public override void Release()
        {
            base.Release();
            init = false;
        }

        private void InitPreview3D()
        {
            //注册数据转换器 暂废弃
        }

        #region Private

        // public void RegisterDataConvertor(Type type, IPreview3DDataConvertor convertor)
        // {
        //     if (_convertors.ContainsKey(type))
        //     {
        //         LoggerUtils.LogError($"Preview3DError 重复添加同种类型的转换器 type = {type.FullName}");
        //     }
        //
        //     _convertors[type] = convertor;
        // }

        private Preview3DModelRoot CreateModelRoot()
        {
            var asset = Asset.Load(Preview3DConstant.PathPreviewModelRoot, typeof(GameObject));
            GameObject prefab = asset.asset as GameObject;
            GameObject modelRoot = Object.Instantiate(prefab);
            modelRoot.DontDestroy();
            var preview3DModelRoot = modelRoot.GetComponent<Preview3DModelRoot>();
            preview3DModelRoot.transform.localPosition = new Vector3(999, modelPosOffsetY, 999);
            modelPosOffsetY += 100;
            preview3DModelRoot.gameObject.SetActive(true);

            preview3DModelRoot.rt = new RenderTexture(2700, 1350, 24, RenderTextureFormat.ARGB32);
            preview3DModelRoot.rt.depthStencilFormat = GraphicsFormat.D16_UNorm;
            preview3DModelRoot.rt.name = "PreviewRenderTexture";
            preview3DModelRoot.previewCamera.targetTexture = preview3DModelRoot.rt;


            return preview3DModelRoot;
        }

        private PreviewWrap CreatePreviewWrapper(Preview3DModelRoot preview3DModelRoot, Preview3DRawImage preview3DRawImage)
        {
            var wrap = new PreviewWrap();
            wrap.PreviewModelRoot = preview3DModelRoot;
            wrap.PreviewRawImage = preview3DRawImage;
            wrap.LastHandler = null;
            return wrap;
        }

        #endregion

        #region Public

        public void Attach(Preview3DRawImage preview3DRawImage)
        {
            var preview3DModelRoot = CreateModelRoot();
            var previewWrap = CreatePreviewWrapper(preview3DModelRoot, preview3DRawImage);
            _previewWrapDict[preview3DRawImage] = previewWrap;

            //bind ui & model
            preview3DRawImage.rawImage.texture = previewWrap.PreviewModelRoot.rt;
            preview3DRawImage.gestureHandle.modelRootTrans = previewWrap.PreviewModelRoot.previewModel;
        }

        public void Detach(Preview3DRawImage preview3DRawImage)
        {
            if (_previewWrapDict.TryGetValue(preview3DRawImage, out var previewWrap))
            {
                previewWrap.LastHandler?.CancelPreview(null, previewWrap);
                previewWrap.PreviewModelRoot.previewCamera.targetTexture = null;
                Object.Destroy(previewWrap.PreviewModelRoot.previewCamera.targetTexture);
                Object.Destroy(previewWrap.PreviewModelRoot.gameObject);
                _previewWrapDict.Remove(preview3DRawImage);
            }

            preview3DRawImage.rawImage = null;
        }

        public void Preview(Preview3DRawImage preview3DRawImage, Preview3DData previewData)
        {
            if (previewData == null)
            {
                LoggerUtils.LogError($"Preview3DError Preview error, data can not be null");
                return;
            }

            Preview3DType preview3DType = previewData.PreviewType;
            if (!_previewWrapDict.ContainsKey(preview3DRawImage))
            {
                LoggerUtils.LogError($"Can not find preview wrap");
                return;
            }

            PreviewWrap previewWrap = _previewWrapDict[preview3DRawImage];
            if (previewWrap.LastHandler != null)
            {
                previewWrap.LastHandler.CancelPreview(previewData, previewWrap);
            }

            //执行预览
            var preview3DHandler = PreviewHandlerFactory.GetHandlerByType(preview3DType);
            if (preview3DHandler == null)
            {
                LoggerUtils.LogError($"Can not find preview handler , type is : {preview3DType}");
                return;
            }

            previewWrap.PreviewRawImage.ResetRotation();
            preview3DHandler.HandlePreview(previewData, previewWrap);
            previewWrap.LastHandler = preview3DHandler;
        }

        public void CancelPreview(Preview3DRawImage preview3DRawImage)
        {
            if (!_previewWrapDict.ContainsKey(preview3DRawImage))
            {
                return;
            }

            PreviewWrap previewWrap = _previewWrapDict[preview3DRawImage];
            previewWrap.PreviewRawImage.ResetRotation();
            if (previewWrap.LastHandler != null)
            {
                previewWrap.LastHandler.CancelPreview(null, previewWrap);
                previewWrap.LastHandler = null;
            }
        }

        public void RefreshRole(Preview3DRawImage rawImage)
        {
        }

        #endregion
    }
}