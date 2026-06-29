using GameData.Config;
using UnityEngine;

namespace Game.ECS
{
    public class GameObjectComponent : BaseComponent
    {
        public uint Uid;
        public string PropId;
        public NodeModelType ModelType;
        public GameObject BindGo;

        public override BaseComponent Clone()
        {
            return new GameObjectComponent()
            {
                Uid = UidManager.Inst.GetUid(),
                PropId = PropId,
                ModelType = ModelType,
                BindGo = null, // 由克隆完之后再进行赋值
            };
        }
    }
}