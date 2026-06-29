namespace prop.editor
{
    public class GeneratedTemplate
    {
        public const string ManagerTmp = @"
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(${Name}Behaviour))]
	public class ${Name}Manager : BaseNodeManager
	{
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			nodeBehaviour.entity.AddComp<${Name}Component>();
		}
	}
}
        ";
		
		public const string ManagerNoCompTmp = @"
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(${Name}Behaviour))]
	public class ${Name}Manager : BaseNodeManager
	{
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
		}
	}
}
        ";

        public const string BehaviourTmp = @"
using Game.Base;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class ${Name}Behaviour : NodeBaseBehaviour
    {
    }
}
        ";

        public const string ComponentTmp = @"
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class ${Name}Component : BaseComponent, IComponentSerializer
	{
		public void Read(PComponentData componentData)
		{
			// if (componentData.CmpData.TryUnpack<${PBData}>(out var pbBodyData))
            // {

            // }
		}

		public PComponentData Write()
		{
			// var pbBodyData = new ${PBData}();
            
			var componentData = new PComponentData();
            // componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new ${Name}Component()
			{
			};
			return component;
		}
	}
}
        ";

		public const string ComponentRegisterTmp = @"
using System;
using System.Collections.Generic;
using Game.Props.PropsComponents;
using GameData.Config;

namespace Game.Generated
{
	public class EntityComponentRegister
	{
		public Dictionary<NodeComponentId, Type> Dict = new Dictionary<NodeComponentId, Type>();
		public EntityComponentRegister()
		{
${Content}
		}
	}
}		
		";

		public const string ManagerRegisterTmp = @"
using System;
using System.Collections.Generic;
using Game.Props.PropsManagers;
using GameData.Config;

namespace Game.Generated
{
	public class EntityManagerRegister
	{
		public Dictionary<NodeModelType, Type> Dict = new Dictionary<NodeModelType, Type>();
		public EntityManagerRegister()
		{
${Content}
		}
	}
}
		";
    }
}