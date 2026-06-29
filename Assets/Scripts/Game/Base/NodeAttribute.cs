/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-18 11:20:14
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-07-18 11:23:20
 * @ Description: 道具节点相关标签
 */

using System;

namespace Game.Base
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class NodeBehaviourAttribute : Attribute
    {
        private Type behaviourT;
        public Type BehaviourType { get => behaviourT; }
        public NodeBehaviourAttribute(Type behaviourT)
        {
            this.behaviourT = behaviourT;
        }
    }
}