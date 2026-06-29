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
    [NodeBehaviourAttribute(typeof(AIHospital_PhotoBehaviour))]
    public class AIHospital_PhotoBehaviour : NodeBaseBehaviour
    {
        public string Path;
        static MaterialPropertyBlock mpb;
        Renderer render;
        Mesh orginMesh, newMesh;
        BoxCollider boxCollider;
        MeshFilter meshFilter;
        ShotPhotoLoadState loadState = ShotPhotoLoadState.Empty;
        Action<ShotPhotoLoadState, Texture> onImageLoadAction;
        const float PhotoFixedWidth = 1.6f;
        const float PhotoFixedHeight = 0.9f;

        RemoteImageRequest request;

        // 位置标识
        public int location;
        public string hexColor;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            if (mpb == null)
            {
                mpb = new MaterialPropertyBlock();
            }
            render = this.GetComponentInChildren<Renderer>();
            boxCollider = GetComponentInChildren<BoxCollider>(true);
            meshFilter = GetComponentInChildren<MeshFilter>();
            orginMesh = meshFilter.sharedMesh;
            //美术给定的RGB值
            hexColor = "#333333";

            // 从AIGameCommonComponent获取位置信息
            var dataComp = this.entity?.GetComp<AIGameCommonComponent>();
            if (dataComp != null)
            {
                location = dataComp.location;
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
                    SetTexture(request.asset);
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
            AutoSize(tex);
            //AutoSizeWithCrop(tex);
        }

        void AutoSize(Texture texture)
        {
            if (newMesh == null)
            {
                newMesh = Instantiate(orginMesh);
            }

            const float STANDARD_ASPECT = 16f / 9f; // 标准16:9比例
            float textureAspect = (float)texture.width / texture.height; // 图片宽高比

            // 计算顶点位置
            float width, height;
            if (textureAspect > STANDARD_ASPECT)
            {
                // 图片比16:9更宽,固定宽度,高度按比例计算
                width = PhotoFixedWidth;
                height = width / textureAspect;
            }
            else
            {
                // 图片比16:9更高,固定高度,宽度按比例计算
                height = PhotoFixedHeight;
                width = height * textureAspect;
            }

            // 重设顶点
            newMesh.vertices = new Vector3[]
            {
                new Vector3(-width, -height, 0),
                new Vector3(-width, height, 0),
                new Vector3(width, height, 0),
                new Vector3(width, -height, 0)
            };

            newMesh.RecalculateBounds();
            meshFilter.mesh = newMesh;

            // 重设Collider
            boxCollider.size = new Vector3(width * 2, height * 2, 0);

            LoggerUtils.Log($"纹理宽高比: {textureAspect:F2}, 标准比例: {STANDARD_ASPECT:F2}, " +
                            $"宽度: {width:F2}, 高度: {height:F2}");
        }

        void AutoSizeWithCrop(Texture texture)
        {
            if (newMesh == null)
            {
                newMesh = Instantiate(orginMesh);
            }
            const float TARGET_ASPECT = 16f / 10f;  // 目标宽高比 16:10

            float textureAspect = (float)texture.width / texture.height;
            Vector2[] uvs = new Vector2[4];

            if (Mathf.Approximately(textureAspect, TARGET_ASPECT))
            {
                // 纹理刚好是16:10，使用完整纹理
                uvs = new Vector2[]
                {
                    new Vector2(0, 0),  // 左下
                    new Vector2(0, 1),  // 左上
                    new Vector2(1, 1),  // 右上
                    new Vector2(1, 0)   // 右下
                };
                Debug.Log("纹理比例匹配16:10，显示完整纹理");
            }
            else if (textureAspect > TARGET_ASPECT)
            {
                // 纹理太宽，需要裁剪左右
                float uvWidth = TARGET_ASPECT / textureAspect;  // UV宽度比例
                float uvOffset = (1f - uvWidth) * 0.5f;  // 水平偏移量，保持居中

                uvs = new Vector2[]
                {
                    new Vector2(uvOffset, 0),           // 左下
                    new Vector2(uvOffset, 1),           // 左上
                    new Vector2(uvOffset + uvWidth, 1), // 右上
                    new Vector2(uvOffset + uvWidth, 0)  // 右下
                };
                Debug.Log($"纹理较宽，裁剪左右。UV宽度={uvWidth:F3}，偏移={uvOffset:F3}");
            }
            else
            {
                // 纹理太高，需要裁剪上下
                float uvHeight = textureAspect / TARGET_ASPECT;  // UV高度比例
                float uvOffset = (1f - uvHeight) * 0.5f;  // 垂直偏移量，保持居中

                uvs = new Vector2[]
                {
                    new Vector2(0, uvOffset),               // 左下
                    new Vector2(0, uvOffset + uvHeight),    // 左上
                    new Vector2(1, uvOffset + uvHeight),    // 右上
                    new Vector2(1, uvOffset)                // 右下
                };
                Debug.Log($"纹理较高，裁剪上下。UV高度={uvHeight:F3}，偏移={uvOffset:F3}");
            }

            // 更新UV坐标
            newMesh.uv = uvs;
            
            // 应用到网格过滤器
            meshFilter.mesh = newMesh;

            // 更新碰撞体（保持16:10比例）
            float colliderHeight = PhotoFixedWidth / TARGET_ASPECT;
            boxCollider.size = new Vector3(PhotoFixedWidth, colliderHeight, 0);
            boxCollider.center = new Vector3(0, colliderHeight/2, 0);

            // 打印调试信息
            Debug.Log($"Texture aspect: {textureAspect:F3}, Target aspect: {TARGET_ASPECT:F3}");
            Debug.Log($"Final collider size: {boxCollider.size}");
        }
    }
}