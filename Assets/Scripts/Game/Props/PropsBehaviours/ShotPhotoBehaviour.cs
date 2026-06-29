using System;
using Game.Base;
using Game.Config;
using Game.Props.PropsManagers;
using Game.Utils;
using UnityEngine;
using xasset;

namespace Game.Props.PropsBehaviours
{
    public class ShotPhotoBehaviour : NodeBaseBehaviour
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

        RemoteImageRequest request;

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
            orginMesh = meshFilter.mesh;
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
            imageUrl = imageUrl.Replace(GameConsts.AccBusinessBaseUrl, GameConsts.BusinessCdnUrl);
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
            render.GetPropertyBlock(mpb);
            mpb.SetTexture("_BaseMap", tex);
            mpb.SetTexture("_EmissionMap", tex);
            render.SetPropertyBlock(mpb);
            AutoSize(tex);
        }

        void AutoSize(Texture texture)
        {
            if (newMesh == null)
            {
                newMesh = new Mesh()
                {
                    vertices = orginMesh.vertices,
                    uv = orginMesh.uv,
                    triangles = new int[] { 0, 2, 1, 0, 3, 2 },
                };
            }

            float repectRatio = (float)texture.height / texture.width;
            var photoHeight = repectRatio * PhotoFixedWidth;
            // 重设顶点
            newMesh.vertices = new Vector3[]
            {
                new Vector3(-PhotoFixedWidth/2, 0, 0),
                new Vector3(-PhotoFixedWidth/2, photoHeight, 0),
                new Vector3(PhotoFixedWidth/2, photoHeight, 0),
                new Vector3(PhotoFixedWidth/2, 0, 0)
            };
            newMesh.RecalculateBounds();
            meshFilter.mesh = newMesh;
            // 重设Collider
            boxCollider.size = new Vector3(PhotoFixedWidth, photoHeight, 0);
            boxCollider.center = new Vector3(0, photoHeight / 2, 0);
        }
    }
}

