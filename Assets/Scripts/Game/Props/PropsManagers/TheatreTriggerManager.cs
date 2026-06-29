
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using GameData;
using UnityEngine;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(TheatreTriggerBehaviour))]
    public class TheatreTriggerManager : BaseNodeManager
    {
        // key = TriggerType (0=Theatre, 2=Trigger): prefab used as assetObj (visual + collider)
        // key = TriggerType (1=Actor): prefab used as floating icon above actor head
        private static readonly Dictionary<int, string> PrefabPaths = new()
        {
            { 0, "Assets/Loadable/Model3D/Editor_Props/Theatre/interact_effect/eff/OC_Filmreel.prefab" },
            { 1, "Assets/Loadable/Model3D/Editor_Props/Theatre/info_point/eff_tishi.prefab" },
            { 2, "Assets/Loadable/Model3D/Editor_Props/Theatre/trigger_area/eff_chufa.prefab" },
        };

        protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
        {
            var comp = nodeBehaviour.entity.AddComp<TheatreTriggerComponent>();
            var behaviour = nodeBehaviour as TheatreTriggerBehaviour;
            LoadVisual(behaviour, comp?.TriggerType ?? 0);
            if (comp?.TriggerType == 1)
                LoadActorForEdit(behaviour, comp);
        }

        protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
        {
            base.OnNotifyCreateInBuild(nodeBehaviour);
            var comp = nodeBehaviour.entity.GetComp<TheatreTriggerComponent>();
            var behaviour = nodeBehaviour as TheatreTriggerBehaviour;
            if (string.IsNullOrEmpty(comp?.TheatreId))
            {
                var model = GameController.GetEnterGameModel();
                if (model == EnterGameModel.ContinueEditScene || model == EnterGameModel.CreateEmptyScene)
                    LoadVisual(behaviour, 0);
                return;
            }
            LoadVisual(behaviour, comp.TriggerType);
            if (comp.TriggerType == 1)
                behaviour?.LoadActorCharacter(comp.ActorId, comp.ActorName, comp.ClothesIndex);
            behaviour?.FetchTheatreInfo(comp.TheatreId);
        }

        public void RefreshVisual(TheatreTriggerBehaviour behaviour, int triggerType)
        {
            if (behaviour == null) return;
            if (behaviour.assetObj != null)
            {
                Object.Destroy(behaviour.assetObj);
                behaviour.assetObj = null;
            }
            behaviour.ClearPlaceholder();
            behaviour.ClearActorCharacter();
            behaviour.ClearFloatingIcon();
            LoadVisual(behaviour, triggerType);
            if (triggerType == 1)
            {
                var comp = behaviour.entity.GetComp<TheatreTriggerComponent>();
                LoadActorForEdit(behaviour, comp);
            }
        }

        private void LoadActorForEdit(TheatreTriggerBehaviour behaviour, TheatreTriggerComponent comp)
        {
            if (comp != null && !string.IsNullOrEmpty(comp.ActorId))
                behaviour.LoadActorCharacter(comp.ActorId, comp.ActorName, comp.ClothesIndex);
            else
                behaviour.LoadPlaceholder();
        }

        private void LoadVisual(TheatreTriggerBehaviour behaviour, int triggerType)
        {
            if (behaviour == null) return;

            // Actor type (1): assetObj is a detection collider zone; icon floats above head separately.
            // Other types: assetObj is the prefab itself (which carries its own collider).
            GameObject assetObj;
            if (triggerType != 1 && PrefabPaths.TryGetValue(triggerType, out var path) && !string.IsNullOrEmpty(path))
            {
                assetObj = ModelCachePool.Inst.Get(behaviour.GetAssetId(), path);
            }
            else
            {
                assetObj = new GameObject("TheatreTriggerZone");
                assetObj.layer = LayerMask.NameToLayer("Model");
                var col = assetObj.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(2f, 2f, 2f);
                col.center = new Vector3(0f, 0.5f, 0f);
            }
            behaviour.SetAssetObj(assetObj);

            // Floating icon above actor head (only for actor type)
            if (triggerType == 1 && PrefabPaths.TryGetValue(1, out var iconPath) && !string.IsNullOrEmpty(iconPath))
                behaviour.LoadFloatingIcon(iconPath);
        }
    }
}
