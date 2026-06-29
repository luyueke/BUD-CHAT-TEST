using System.Collections.Generic;
using System.Linq;
using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Pb.Map;
using UnityEngine;

namespace Game.Utils {
    /// <summary>
    /// 防止程序集引用错误，不使用GameObject Extends
    /// </summary>
    public static class GamePropUtils {
        public static bool TryGetComponent<T>(this PNodeData pNodeData, out T comp)
            where T : IComponentSerializer, new() {
            var compData = pNodeData?.Attrs.FirstOrDefault(tmp =>
                tmp.CmpId == (uint)GameTypeRegister.Inst.GetComponentId(typeof(T)));
            if (compData == null) {
                comp = default;
                return false;
            }

            comp = new T();
            comp.Read(compData);
            return true;
        }


        public static NodeBaseBehaviour GetBehaviourByGameObject(GameObject gameObject) {
            return gameObject.GetComponent<NodeBaseBehaviour>();
        }

        public static NodeBaseBehaviour GetBehaviourByGameObjectParent(GameObject gameObject) {
            return gameObject.GetComponentInParent<NodeBaseBehaviour>();
        }

        public static SceneEntity GetEntityByGameObject(GameObject gameObject) {
            var bev = GetBehaviourByGameObject(gameObject);
            return bev ? bev.entity : null;
        }

        public static Vector3 GetCenterPoint(List<SceneEntity> entitys) {
            if (entitys.Count == 0) {
                return Vector3.zero;
            } else if (entitys.Count == 1) {
                return entitys[0].GetComp<GameObjectComponent>().BindGo.transform.position;
            }

            Vector3 min = entitys[0].GetComp<GameObjectComponent>().BindGo.transform.position;
            Vector3 max = min;
            entitys.ForEach(x => {
                var pos = x.GetComp<GameObjectComponent>().BindGo.transform.position;
                min.x = Mathf.Min(min.x, pos.x);
                min.y = Mathf.Min(min.y, pos.y);
                min.z = Mathf.Min(min.z, pos.z);
                max.x = Mathf.Max(max.x, pos.x);
                max.y = Mathf.Max(max.y, pos.y);
                max.z = Mathf.Max(max.z, pos.z);
            });
            return (min + max) / 2;
        }

        public static SceneEntity GetCanControllerNode(GameObject hitGo) {
            var nodeBehav = hitGo.GetComponentInParent<NodeBaseBehaviour>();
            var entity = GetCanControllerEntity(nodeBehav);
            return entity;
        }

        private static SceneEntity GetCanControllerEntity(NodeBaseBehaviour nodeBehav) {
            if (nodeBehav == null) {
                return null;
            }

            var parent = nodeBehav.transform.parent;
            if (parent != null) {
                var parBehav = parent.GetComponentInParent<MultiChildBehaviour>();
                if (parBehav != null && !parBehav.ChildSelectable) {
                    return GetCanControllerEntity(parBehav);
                }

                var fishingBev = parent.GetComponentInParent<ActorNodeBehaviour>();
                if (fishingBev != null) {
                    return GetCanControllerEntity(fishingBev);
                }
            }

            return nodeBehav.entity;
        }

        /// <summary>
        /// 获取选中之后真正的节点
        /// </summary>
        public static GameObject GetSelectRealNode(GameObject hitGo) {
            var parBehav = hitGo.GetComponentInParent<MultiChildBehaviour>();
            if (parBehav != null) {
                return parBehav.gameObject;
            }

            return hitGo;
        }

        private static Vector3 colorMultiplier = new Vector3(1.3f, 1.3f, 1.3f);

        public static Color GetHighlightColor(Color oldColor) {
            Color newColor = oldColor;
            if (newColor.r < 0.2f) {
                newColor.r += 0.1f;
            }

            newColor.r *= colorMultiplier.x;

            if (newColor.g < 0.2f) {
                newColor.g += 0.1f;
            }

            newColor.g *= colorMultiplier.y;

            if (newColor.b < 0.2f) {
                newColor.b += 0.1f;
            }

            newColor.b *= colorMultiplier.z;

            if (newColor.a < 0.5f) newColor.a *= 1.4f;

            return newColor;
        }


        public static Color GetHighlightColor(Color oldColor, float adjustValue) {
            Color newColor = oldColor;
            if (newColor.r < 0.2f) {
                newColor.r += 0.1f;
            }

            newColor.r *= adjustValue;

            if (newColor.g < 0.2f) {
                newColor.g += 0.1f;
            }

            newColor.g *= adjustValue;

            if (newColor.b < 0.2f) {
                newColor.b += 0.1f;
            }

            newColor.b *= adjustValue;

            if (newColor.a < 0.5f) newColor.a *= 1.4f;

            return newColor;
        }


        public static void HighLight(bool isHigh, ref Color[] originColor, Renderer[] renderers,
            float hightVaule = 1.3f) {
            if (isHigh) {
                originColor = new Color[renderers.Length];
                for (int i = 0; i < originColor.Length; i++) {
                    var mat = renderers[i].material;
                    originColor[i] = mat.GetColor("_BaseColor");
                    mat.SetColor("_BaseColor", GetHighlightColor(originColor[i], hightVaule));
                }
            } else {
                if (originColor != null && originColor.Length == renderers.Length) {
                    for (int i = 0; i < originColor.Length; i++) {
                        var mat = renderers[i].material;
                        mat.SetColor("_BaseColor", originColor[i]);
                    }
                }
            }
        }

        public static void HighLight(bool isHigh, ref Color[] originColor, Renderer[] renderers, Color hightColor) {
            if (isHigh) {
                originColor = new Color[renderers.Length];
                for (int i = 0; i < originColor.Length; i++) {
                    var mat = renderers[i].material;
                    originColor[i] = mat.GetColor("_BaseColor");
                    mat.SetColor("_BaseColor", hightColor);
                }
            } else {
                if (originColor != null && originColor.Length == renderers.Length) {
                    for (int i = 0; i < originColor.Length; i++) {
                        var mat = renderers[i].material;
                        mat.SetColor("_BaseColor", originColor[i]);
                    }
                }
            }
        }
    }
}
