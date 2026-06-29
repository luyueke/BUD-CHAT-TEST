
using System.Collections.Generic;
using Game.Base;
using Game.Config;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;

namespace Game.Props.PropsComponents
{
	public class SwitchCtrComponent : BaseComponent, IComponentSerializer
	{
		public Dictionary<GameGlobalEnum.PropControlType, List<uint>> SrcTypeDicts =
			new Dictionary<GameGlobalEnum.PropControlType, List<uint>>();
		
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PSwitchCtrComponentData>(out var pbBodyData))
			{
				var srcDatas = pbBodyData.SrcDatas;
				for (int i = 0; i < srcDatas.Count; i++)
				{
					var srcData = srcDatas[i];
					var srcType = (GameGlobalEnum.PropControlType)srcData.LinkType;
					var srcUids = new List<uint>();
					srcUids.AddRange(srcData.LinkUids);
					if (!SrcTypeDicts.ContainsKey(srcType))
					{
						SrcTypeDicts.Add(srcType,srcUids);
					}
				}
			}
		}

		public PComponentData Write()
		{
			var pbBodyData = new PSwitchCtrComponentData();
			var componentData = new PComponentData();
			foreach (var srcType in SrcTypeDicts.Keys)
			{
				var srcUids = SrcTypeDicts[srcType];
				if (srcUids != null && srcUids.Count > 0)
				{
					PLinkData ctrData = new PLinkData();
					ctrData.LinkType = (int)srcType;
					ctrData.LinkUids.AddRange(srcUids);
					pbBodyData.SrcDatas.Add(ctrData);
				}
			}
			componentData.CmpData = Any.Pack(pbBodyData);
			return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new SwitchCtrComponent();
			foreach (var controlType in SrcTypeDicts.Keys)
			{
				var curUids = SrcTypeDicts[controlType];
				if (curUids != null && curUids.Count > 0)
				{
					List<uint> newUids = new List<uint>();
					newUids.AddRange(curUids);
					component.SrcTypeDicts.Add(controlType,newUids);
				}
			}
			return component;
		}
	}
}
        
