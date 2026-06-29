using System;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Game.Utils;
using UnityEngine;
using xasset;

namespace Game.Props.PropsBehaviours
{
    [NodeBehaviourAttribute(typeof(AIPark_PhotoBehaviour))]
    public class AIPark_PhotoBehaviour : NodeBaseBehaviour
    {
        public string Path;
        static MaterialPropertyBlock mpb;
        static MaterialPropertyBlock mpb1;
        Renderer render;
        Renderer render1;
        Mesh orginMesh, newMesh;
        BoxCollider boxCollider;
        MeshFilter meshFilter, meshFilter1;
        ShotPhotoLoadState loadState = ShotPhotoLoadState.Empty;
        Action<ShotPhotoLoadState, Texture> onImageLoadAction;

        RemoteImageRequest request;

        // 位置标识
        public int location;
        public string hexColor;

        private Transform oldModel, model, model1;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            if (mpb == null)
            {
                mpb = new MaterialPropertyBlock();
            }
            oldModel = transform.Find("Model");
            model = transform.Find("PhotoWall/Model");
            render = model.GetComponent<Renderer>();
            boxCollider = model.GetComponent<BoxCollider>();
            meshFilter = model.GetComponent<MeshFilter>();
            orginMesh = meshFilter.sharedMesh;
            //美术给定的RGB值
            hexColor = "#333333";

            // 从AIGameCommonComponent获取位置信息
            var dataComp = this.entity?.GetComp<AIGameCommonComponent>();
            if (dataComp != null)
            {
                location = dataComp.location;
                if (location > 8)
                {
                    model1 = transform.Find("PhotoWall1/Model");
                    if (mpb1 == null)
                    {
                        mpb1 = new MaterialPropertyBlock();
                    }
                    render1 = model1.GetComponent<Renderer>();
                    meshFilter1 = model1.GetComponent<MeshFilter>();
                }
            }
        }

        public void AddImageLoadListener(Action<ShotPhotoLoadState, Texture> action)
        {
            onImageLoadAction += action;
        }

        public void RemoveImageLoadListener(Action<ShotPhotoLoadState, Texture> action)
        {
            onImageLoadAction -= action;
        }

        public void Relase(string imageUrl)
        {
            if (request != null)
            {
                request.Release();
            }
        }

        public void OnDestroy()
        {
            if (request != null)
            {
                request.Release();
            }
        }

        public void Get(string imageUrl)
        {
            if (loadState == ShotPhotoLoadState.Success)
            {
                render.GetPropertyBlock(mpb);
                Texture texture = mpb.GetTexture("_BaseMap");
                onImageLoadAction?.Invoke(loadState, texture);
            }
            else
            {
                onImageLoadAction?.Invoke(loadState, null);
            }
        }

        public void Load(string imageUrl)
        {
            //Debug.Log("AIPARK_PHOTO_URL=" + imageUrl);
            if (string.IsNullOrEmpty(imageUrl)) return;

            loadState = ShotPhotoLoadState.Loading;
            imageUrl = imageUrl.Replace(GameConsts.BusinessBaseUrl, GameConsts.BusinessCdnUrl);
            var size = GetTextureSize();
            if (imageUrl.Contains(GameConsts.BusinessCdnUrl)) imageUrl = $"{imageUrl}?imageMogr2/thumbnail/!{50}p/a.png";
            Path = imageUrl;
            onImageLoadAction?.Invoke(loadState, null);
            if (request != null) request.Release();
            request = Asset.LoadRemoteImageAsync(imageUrl);
            request.completed += (req) =>
            {
                if (this == null) return;
                if (request.path != imageUrl) return;
                if (request.result == Request.Result.Success && request.asset != null)
                {
                    try
                    {
                        SetTexture(request.asset);
                        oldModel.gameObject.SetActive(false);
                        model.gameObject.SetActive(true);
                        model1?.gameObject.SetActive(true);
                    }
                    catch(Exception e)
                    {
                        Debug.LogError("AIPark_Photo Exception e=" + e.Message);
                        oldModel.gameObject.SetActive(true);
                        model.gameObject.SetActive(false);
                        model1?.gameObject.SetActive(false);
                    }
                
                    loadState = ShotPhotoLoadState.Success;
                }
                else
                {
                    loadState = ShotPhotoLoadState.Fail;
                }
                onImageLoadAction?.Invoke(loadState, request.asset);
            };
        }

        public Vector2 GetTextureSize()
        {
            var x = Mathf.RoundToInt(transform.localScale.x * boxCollider.size.x * 512);
            var y = Mathf.RoundToInt(transform.localScale.y * boxCollider.size.y * 512);

            return new Vector2(Mathf.Min(2048, x), Mathf.Min(2048, y));
        }

        void SetTexture(Texture tex)
        {
            Debug.Log($"Texture size: {tex.width}x{tex.height}, format: {tex.graphicsFormat}");
            render.GetPropertyBlock(mpb);
            mpb.SetTexture("_BaseMap", tex);
            Color color;
            ColorUtility.TryParseHtmlString(hexColor, out color);
            mpb.SetTexture("_EmissionMap", tex);
            mpb.SetColor("_EmissionColor", color);
            render.SetPropertyBlock(mpb);
            if(location>8)
            {
                render1.GetPropertyBlock(mpb1);
                mpb1.SetTexture("_BaseMap", tex);
                mpb1.SetTexture("_EmissionMap", tex);
                mpb1.SetColor("_EmissionColor", color);
                render1.SetPropertyBlock(mpb1);
            }
    
            //if (location > 8)
            //    return;
            AutoSize(tex);
            //AutoSizeWithCrop(tex);
        }

        void AutoSize(Texture texture)
        {
            if (newMesh == null)
            {
                newMesh = new Mesh();
            }

            // 使用实际的碰撞体尺寸比例
            float colliderAspect = boxCollider.size.x / boxCollider.size.y;
            float textureAspect = (float)texture.width / texture.height; // 图片宽高比
            
            Debug.Log($"碰撞体宽高比: {colliderAspect:F3} (size.x={boxCollider.size.x:F2}, size.y={boxCollider.size.y:F2})");
            Debug.Log($"纹理宽高比: {textureAspect:F3} ({texture.width}x{texture.height})");
            
            // 计算顶点位置 - 使用碰撞体实际尺寸
            float width, height;
            if (textureAspect > colliderAspect)
            {
                // 图片比碰撞体更宽,需要裁剪左右两边
                // 使用碰撞体实际高度,宽度按碰撞体比例计算
                height = boxCollider.size.y;
                width = height * colliderAspect;
                Debug.Log($"图片较宽，裁剪左右。最终尺寸: {width:F3}x{height:F3}");
            }
            else
            {
                // 图片比碰撞体更高,需要裁剪上下两边
                // 使用碰撞体实际宽度,高度按碰撞体比例计算
                width = boxCollider.size.x;
                height = width / colliderAspect;
                Debug.Log($"图片较高，裁剪上下。最终尺寸: {width:F3}x{height:F3}");
            }

            // 设置顶点 - 使用x-y平面，Z轴为深度
            newMesh.vertices = new Vector3[]
            {
                new Vector3(-width, -height, 0),  // 左下
                new Vector3(-width, height, 0),   // 左上
                new Vector3(width, height, 0),    // 右上
                new Vector3(width, -height, 0)    // 右下
            };

            // 设置三角形索引 - 翻转顶点顺序以确保正面朝向
            newMesh.triangles = new int[]
            {
                0, 2, 1,  // 第一个三角形 - 翻转顶点顺序
                0, 3, 2   // 第二个三角形 - 翻转顶点顺序
            };

            // 设置UV坐标 - 翻转X轴以修正左右方向
            newMesh.uv = new Vector2[]
            {
                new Vector2(1, 0),  // 右下
                new Vector2(1, 1),  // 右上
                new Vector2(0, 1),  // 左上
                new Vector2(0, 0)   // 左下
            };

            // 重新计算法线以确保正面朝向
            newMesh.RecalculateNormals();
            newMesh.RecalculateBounds();
            
            meshFilter.mesh = newMesh;
            if(location > 8 && meshFilter1 != null)
            {
                meshFilter1.mesh = newMesh;
            }
  

            // 重设Collider
            // boxCollider.size = new Vector3(width * 2, height * 2, 0);

            // LoggerUtils.Log($"纹理宽高比: {textureAspect:F2}, 标准比例: {STANDARD_ASPECT:F2}, " +  $"宽度: {width:F2}, 高度: {height:F2}");
        }

    }
}