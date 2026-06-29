
using System.Collections.Generic;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.Props.PropsManagers;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;

namespace Game.Props.PropsComponents
{
	public class SwitchBtnComponent : BaseComponent, IComponentSerializer
	{
		public int SwitchIndex = 0;
		//按类型分类的控制实体uid列表
		public Dictionary<GameGlobalEnum.PropControlType, List<uint>> CtrTypeDicts =
			new Dictionary<GameGlobalEnum.PropControlType, List<uint>>();
		
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PSwitchBtnComponentData>(out var pbBodyData))
			{
				SwitchIndex = pbBodyData.SwitchIndex;
				var ctrDatas = pbBodyData.CtrDatas;
				for (int i = 0; i < ctrDatas.Count; i++)
				{
					var ctrData = ctrDatas[i];
					var ctrType = (GameGlobalEnum.PropControlType)ctrData.LinkType;
					var ctrUids = new List<uint>();
					ctrUids.AddRange(ctrData.LinkUids);
					if (!CtrTypeDicts.ContainsKey(ctrType))
					{
						CtrTypeDicts.Add(ctrType,ctrUids);
					}
				}
			}
		}
		
		public void RemoveAllCtrId(uint uid)
		{
			foreach (var ctrUids in CtrTypeDicts.Values)
			{
				if (ctrUids != null && ctrUids.Contains(uid))
				{
					ctrUids.Remove(uid);
				}
			}
		}

		public void AddCtrId(uint ctrId, GameGlobalEnum.PropControlType controlType)
		{
			if (!CtrTypeDicts.ContainsKey(controlType))
			{
				CtrTypeDicts.Add(controlType,new List<uint>());
			}

			var ctrUids = CtrTypeDicts[controlType];
			if (!ctrUids.Contains(ctrId))
			{
				ctrUids.Add(ctrId);
			}
		}

		public PComponentData Write()
		{
			var pbBodyData = new PSwitchBtnComponentData();
			var componentData = new PComponentData();
			pbBodyData.SwitchIndex = SwitchIndex;
			foreach (var ctrType in CtrTypeDicts.Keys)
			{
				var ctrUids = CtrTypeDicts[ctrType];
				if (ctrUids != null && ctrUids.Count > 0)
				{
					PLinkData ctrData = new PLinkData();
					ctrData.LinkType = (int)ctrType;
					ctrData.LinkUids.AddRange(ctrUids);
					pbBodyData.CtrDatas.Add(ctrData);
				}
			}
			componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new SwitchBtnComponent();
			component.SwitchIndex = GlobalNodeManager.Inst.Get<SwitchBtnManager>().GetNewIndex();
			component.CtrTypeDicts.Clear();
			return component;
		}
	}
}
        
