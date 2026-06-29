using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.ECS
{
    public class EcsSceneWorld
    {
        private static int entityCount = 0;
        private List<SceneEntity> allEntities = new List<SceneEntity>();

        public SceneEntity CreateEntity()
        {
            entityCount++;
            SceneEntity entity = new SceneEntity(entityCount);
            allEntities.Add(entity);
            return entity;
        }

        public void DestroyEntity(SceneEntity entity)
        {
            entity.Destroy();
            if (allEntities.Contains(entity))
            {
                allEntities.Remove(entity);
                entityCount--;
            }
        }

        public SceneEntity CloneEntity(SceneEntity targetEntity)
        {
            SceneEntity newEntity = CreateEntity();
            targetEntity.CloneComponents(newEntity);
            return newEntity;
        }

        public void DestroyAllEntity()
        {
            for (int i = 0; i < allEntities.Count; i++)
            {
                SceneEntity entity = allEntities[i];
                entity.Destroy();
            }
            allEntities.Clear();
            entityCount = 0;
        }
    }
}
