

using Es;
using Game.Base;
using GameData;
using System;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers;
using UnityEngine;
/**
* @ Author: Jun Zhou
* @ Create Time: 2023-09-12 17:02:40
* @ Modified by: Jun Zhou
* @ Modified time: 2023-09-12 17:05:10
* @ Description: 游戏中材质加载器
*/
namespace Game.Utils
{
    public class GameMaterialLoader
    {
        MaterialUnionID lastMatId;
        MaterialUnionID curMatId;
        GameObject nodeObj;
        MaterialPropertyBlock matMpb;
        Renderer[] renderers;
        Action<int,Texture> onLoadAction;

        public GameMaterialLoader(GameObject obj, Renderer[] renderers, MaterialPropertyBlock mpb)
        {
            nodeObj = obj;
            matMpb = mpb;
            this.renderers = renderers;

            onLoadAction = OnLoadUGCTexSuccess;
        }

        public GameMaterialLoader(GameObject obj, MaterialPropertyBlock mpb)
        {
            nodeObj = obj;
            matMpb = mpb;
            renderers = nodeObj.GetComponentsInChildren<Renderer>();

            onLoadAction = OnLoadUGCTexSuccess;
        }

        public void Destroy()
        {
            onLoadAction = null;
        }

        public void Load(MaterialUnionID id)
        {
            lastMatId = curMatId;

            #region 记录UGCMat使用情况
            var nodeBev = nodeObj.GetComponent<NodeBaseBehaviour>();
            if (nodeBev != null && nodeBev.entity != null && nodeBev.entity.HasComp<GameObjectComponent>())
            {
                if ((nodeBev is TerrainBehaviour) == false)
                {
                    var gCom = nodeBev.entity.GetComp<GameObjectComponent>();
                    var uid = gCom.Uid;
                    if (lastMatId != null && lastMatId.IsUGC)
                    {
                        GameProfilerManager.Inst.RecordMatUnUsed(lastMatId.UGCId, uid);
                    }
                    if (id != null && id.IsUGC)
                    {
                        GameProfilerManager.Inst.RecordMatUsed(id.UGCId, uid);
                    }
                }
            }
            #endregion

            curMatId = id;
            if (curMatId.IsUGC)
            {
                LoadUGC(curMatId.UGCId,nodeObj);
            } else {
                LoadNormal(curMatId.MatId);
            }
        }

        void LoadNormal(int id)
        {
            var matData = DataTables.GetMatDataConfig(id) ?? DataTables.GetMatDataConfig(158);
            if(renderers.Length <= 0)
                return;
            CustomMaterialLoaderUntils.Inst.SetMaterial(renderers, matData);
    
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].GetPropertyBlock(matMpb);
                if (matData.AnimeType == (int) CustomMaterialLoaderUntils.AnimeTypeEnum.Default)
                {
                    CustomMaterialLoaderUntils.Inst.SetTexture(nodeObj, matMpb, matData);
                }
                else
                {
                    CustomMaterialLoaderUntils.Inst.SetAnimeTexture(nodeObj, matMpb, matData);
                }

                renderers[i].SetPropertyBlock(matMpb);
            }
            var color = matMpb.GetColor("_BaseColor");
            SetColor(color);
        }

        void LoadUGC(string id,GameObject refNode)
        {
            GameUgcMatManager.Inst.LoadGameUgcTextureById(id,refNode,renderers, onLoadAction);
        }

        void OnLoadUGCTexSuccess(int ugcStyle,Texture tex)
        {
            if (nodeObj == null) {
                LoggerUtils.Log("节点已经销毁");
                return;
            }
            if (tex == null) {
                LoggerUtils.Log("OnLoadUGCTexSuccess: tex == null:", curMatId);
                return;
            }
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].GetPropertyBlock(matMpb);
                CustomMaterialLoaderUntils.Inst.SetDefaultTexture(ugcStyle,nodeObj, matMpb, tex);
                renderers[i].SetPropertyBlock(matMpb);
            }
            var color = matMpb.GetColor("_BaseColor");
            SetColor(color);
        }

        public void SetColor(Color color)
        {
            if (color == null || curMatId == null)
            {
                return;
            }
            if (curMatId.IsUGC)
            {
                SetUGCColor(color);
            } else {
                SetNormalColor(color, curMatId.MatId);
            }
        }

        void SetUGCColor(Color color)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].GetPropertyBlock(matMpb);
                matMpb.SetColor("_BaseColor", color);
                matMpb.SetColor("_EmissionColor", Color.clear);
                renderers[i].SetPropertyBlock(matMpb);
            }
        }

        void SetNormalColor(Color color, int matId)
        {
            if(renderers.Length <= 0)
                return;

            var matDataConfig = DataTables.GetMatDataConfig(matId);

            if (matDataConfig != null && matDataConfig.IsTransparent)
            {
                color.a = CustomMaterialLoaderUntils.tAlpha;
            }
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].GetPropertyBlock(matMpb);
                matMpb.SetColor("_BaseColor", color);
                if (matDataConfig != null && matDataConfig.IsEmission)
                {
                    matMpb.SetColor("_EmissionColor", color);
                } else {
                    matMpb.SetColor("_EmissionColor", Color.clear);
                }
                renderers[i].SetPropertyBlock(matMpb);
            }
        }

        public void SetMaterialTiling(Vector2 tiling)
        {
            if (renderers.Length <= 0)
            {
                return;
            }
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].GetPropertyBlock(matMpb);
                matMpb.SetVector("_BaseMap_ST", new Vector4(tiling.x, tiling.y, 0, 0));
                renderers[i].SetPropertyBlock(matMpb);
            }
        }

    }
}
