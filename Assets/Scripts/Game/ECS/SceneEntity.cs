using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace Game.ECS
{
    public class SceneEntity : Entity
    {
#if UNITY_EDITOR
        [ShowInInspector]
        [ReadOnly]
        [DisableInEditorMode]
        [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.CollapsedFoldout)]
#endif
        public Dictionary<Type, BaseComponent> Components = new Dictionary<Type, BaseComponent>(); 
        public SceneEntity(int id):base(id)
        {
            
        }
        
        public T AddComp<T>() where T : BaseComponent, new()
        {
            Type tType = typeof(T);
            if (!Components.ContainsKey(tType))
            {
                var comp = new T();
                Components.Add(tType,comp);
            }
            return (T)Components[tType];
        }

        public BaseComponent AddComp(Type tType)
        {
            if (!Components.ContainsKey(tType))
            {
                var comp = Activator.CreateInstance(tType) as BaseComponent;
                Components.Add(tType,comp);
            }
            return Components[tType];
        }
        
        /// <summary>
        /// 注意，这不是原海外项目的Get，不会自动创建一个Component
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetComp<T>() where T : BaseComponent
        {
            Type tType = typeof(T);
            if (Components.ContainsKey(tType))
            {
                return (T)Components[tType];
            }

            return default(T);
        }
        
        public bool TryGetComp<T>(out T component) where T : BaseComponent
        {
            Type tType = typeof(T);
            if (Components.ContainsKey(tType))
            {
                component = (T) Components[tType];
                return true;
            }
            component = default;
            return false;
        }

        /// <summary>
        ///获取一个Component，如果没有则创建，兼容老习惯，不建议调用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetOrAddComp<T>() where T : BaseComponent, new()
        {
            Type tType = typeof(T);
            if (!Components.ContainsKey(tType))
            {
                var comp = new T();
                Components.Add(tType,comp);
            }
            return (T)Components[tType];
        }
        
        
        public void RemoveComp<T>() where T : BaseComponent, new()
        {
            Type tType = typeof(T);
            if (Components.ContainsKey(tType))
            {
                Components.Remove(tType);
            }
        }
        
        public bool HasComp<T>() where T : BaseComponent
        {
            return Components.ContainsKey(typeof(T));
        }
        
        public Dictionary<Type, BaseComponent> CloneNewComponents(SceneEntity newEntity)
        {
            Dictionary<Type, BaseComponent> comps = new Dictionary<Type, BaseComponent>();
            foreach (var val in Components)
            {
                var newComponent = val.Value.Clone();
                if (newComponent != null)
                    comps.Add(val.Key, newComponent);
            }
            return comps;
        }
        
        public void CloneComponents(SceneEntity newEntity)
        {
            foreach (var val in Components)
            {
                var newComponent = val.Value.Clone();
                if (newComponent != null)
                    newEntity.Components.Add(val.Key, newComponent);
            }
        }
        
        public void Destroy()
        {
            Components.Clear();
        }
        
    }
    
}
