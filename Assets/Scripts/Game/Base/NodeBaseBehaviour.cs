using System;
using Game.ECS;
using UnityEngine;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace Game.Base
{
    public class NodeBaseBehaviour : MonoBehaviour
    {
#if UNITY_EDITOR
        [ShowInInspector]
        [ReadOnly]
        [DisableInEditorMode]
#endif
        public SceneEntity entity;
        public int Uid;

        [SerializeField]private bool _isCanClick = true;
        public virtual bool IsCanClick
        {
            get => _isCanClick;
            set => _isCanClick = value;
        }


        public virtual string GetTouchName() {
            return null;
        }


        public virtual void OnInitByCreate()
        {
            Uid = (int)this.entity.GetComp<GameObjectComponent>().Uid;
        }

        public virtual void HighLight(bool isHigh)
        {

        }

        public virtual void OnTouchClick()
        {
        }

        public virtual void OnTrigEnter()
        {
        }

        public virtual void OnTrigExit()
        {
        }

        public virtual void OnColliderEnter()
        {
        }

        /// <summary>
        /// Destroy 被回收到缓存池 时调用
        /// </summary>
        public virtual void OnReset() {

        }

        /// <summary>
        /// 道具属性：设置碰撞体是否可以碰撞
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetColliderEnable(bool value)
        {
        }

        public virtual void SetStandardShader()
        {

        }

        public virtual void SetGlowShader()
        {
        }
    }
}

