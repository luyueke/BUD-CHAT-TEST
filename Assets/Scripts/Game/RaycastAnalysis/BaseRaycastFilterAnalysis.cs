// using UnityEngine;
//
// public abstract class BaseRaycastFilterAnalysis<T> where T:MonoBehaviour
// {
//     protected List<iFilerCondition> conditions = new List<iFilerCondition>();
//     protected List<iMultFilterCondition> multConditions = new List<iMultFilterCondition>();
//     protected List<T> nodes = new List<T>();
//     protected virtual void ChangeColldersToType(Collider[] colliders) 
//     {
//         nodes = new List<T>();
//         for (int i = 0; i < colliders.Length; i++)
//         {
//             var bahaviours = colliders[i].GetComponentsInParent<T>();
//             nodes.AddRange(bahaviours);
//         }
//     }
//
//     public virtual void FilerCollider(Collider[] colliders, ref List<T> behaviours)
//     {
//         ChangeColldersToType(colliders);
//         FilerCollider(ref behaviours);
//     }
//
//     public virtual void FilerCollider(ref List<T> behaviours)
//     {
//         List<T> filteredBehaviours = new List<T>();
//         foreach (var nodeBehaviour in nodes)
//         {
//             if (!IsFileredNode(nodeBehaviour))
//             {
//                 filteredBehaviours.Add(nodeBehaviour);
//             }
//         }
//         behaviours = filteredBehaviours;
//         MultFilteredNode(ref behaviours);
//     }
//
//     protected void MultFilteredNode(ref List<T> node)
//     {
//         for (var i = 0; i < multConditions.Count; i++)
//         {
//             multConditions[i].FileredNode(ref node);
//         }
//     }
//
//     protected bool IsFileredNode(T node)
//     {
//         for (var i = 0; i < conditions.Count; i++)
//         {
//             if (conditions[i].IsFileredNode(node))
//             {
//                 return true;
//             }
//         }
//         return false;
//     }
// }
//
//
// public class UIWorldFilterConditionAnalysis : BaseRaycastFilterAnalysis<NodeBaseBehaviour>
// {
//     public UIWorldFilterConditionAnalysis()
//     {
//         // var tags = new List<string>() {"prop"};
//         conditions.Add(new InViewFilerCondition());
//         // conditions.Add(new TagFilterCondition(tags));
//         conditions.Add(new PropTouchFilterCondition());
//         multConditions.Add(new DistanceFilterCondition());
//     }
// }
//
//
//
// /// <summary>
// /// 多节点过滤结构
// /// </summary>
// public interface iMultFilterCondition
// {
//     /// <summary>
//     /// 过滤不符合条件节点
//     /// </summary>
//     public void FileredNode<T>(ref List<T> behaviour) where T:MonoBehaviour;
// }
//
//
// public class DistanceFilterCondition : iMultFilterCondition
// {
//     public void FileredNode<T>(ref List<T> behaviours) where T : MonoBehaviour
//     {
//         if(behaviours.Count == 0)
//             return;
//         var pos = AvatarController.Inst.GetSelfAvatarPosition();
//         float minValue = 100;
//         int index = 0;
//         for (var i = 0; i < behaviours.Count; i++)
//         {
//             float dis = GetDistance(pos, behaviours[i] as NodeBaseBehaviour);
//             if (dis < minValue)
//             {
//                 minValue = dis;
//                 index = i;
//             }
//         }
//         var behaviour = behaviours[index];
//         behaviours.Clear();
//         behaviours.Add(behaviour);
//     }
//     //特殊道具考虑接口实现，可扩展
//     private float GetDistance(Vector3 pos,NodeBaseBehaviour nBehav)
//     {
//         return Vector3.Distance(pos, nBehav.transform.position);
//     }
// }
//
// /// <summary>
// /// 单节点过滤结构
// /// </summary>
// public interface iFilerCondition
// {
//     /// <summary>
//     /// 是否被过滤掉
//     /// </summary>
//     /// <param name="behaviour"></param>
//     /// <typeparam name="T"></typeparam>
//     /// <returns></returns>
//     public bool IsFileredNode<T>(T behaviour) where T : MonoBehaviour;
// }
//
// public class TagFilterCondition : iFilerCondition
// {
//     private List<string> tags;
//     public TagFilterCondition(List<string> _tags)
//     {
//         tags = _tags;
//     }
//
//     public bool IsFileredNode<T>(T behaviour) where T : MonoBehaviour
//     {
//         if (!tags.Contains(behaviour.tag))
//         {
//             return true;
//         }
//
//         return false;
//     }
// }
//
//
// public class InViewFilerCondition : iFilerCondition
// {
//     public bool IsFileredNode<T>(T behaviour) where T : MonoBehaviour
//     {
//         var cam = GlobalCameraManager.Inst.GlobalMainCamera;
//         Vector2 viewPos = cam.WorldToViewportPoint(behaviour.transform.position);
//         Vector3 dir = (behaviour.transform.position - cam.transform.position).normalized;
//         float dot = Vector3.Dot(cam.transform.forward, dir); //判断物体是否在相机前面
//         if (dot > 0 && viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1)
//         {
//             return false;
//         }
//         return true;
//     }
// }
//
//
// public class PropTouchFilterCondition : iFilerCondition
// {
//     public bool IsFileredNode<T>(T behaviour) where T : MonoBehaviour
//     {
//         var nodeBehaviour =  behaviour as NodeBaseBehaviour;
//         var propData = nodeBehaviour.entity.GetPropConfig();
//         if (propData.touchType != 1)
//         {
//             return true;
//         }
//         return false;
//     }
// }