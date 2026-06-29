
using Game.Base;
using Game.ECS;
using Game.Props.PropsManagers;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class TrapBoxComponent : BaseComponent, IComponentSerializer
	{
		public int BoxIndex = 0;
		public int TransType;//传送类型
		public int HasTips;//0 不显示，1 显示
		public string TipsStr = "";//提示
		public int HitState = 0;//是否开启伤害 0:关闭； 1:打开
		public uint PointId = 0;//复活点uid
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PTrapBoxComponent>(out var pbBodyData))
			{
				BoxIndex = pbBodyData.BoxIndex;
				TransType = pbBodyData.TransType;
				HasTips = pbBodyData.HasTips;
				TipsStr = pbBodyData.TipsStr;
				HitState = pbBodyData.HitState;
				PointId = pbBodyData.PointId;
			}
		}

		public PComponentData Write()
		{
			var pbBodyData = new PTrapBoxComponent();
			var componentData = new PComponentData();
			pbBodyData.BoxIndex = BoxIndex;
			pbBodyData.TransType = TransType;
			pbBodyData.HasTips = HasTips;
			pbBodyData.TipsStr = TipsStr;
			pbBodyData.HitState = HitState;
			pbBodyData.PointId = PointId;
			
			componentData.CmpData = Any.Pack(pbBodyData);
			return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new TrapBoxComponent()
			{
				BoxIndex = GlobalNodeManager.Inst.Get<TrapBoxManager>().GetNewIndex(),
				TransType = TransType,
				HasTips = HasTips,
				TipsStr = TipsStr,
				HitState = HitState,
			};
			return component;
		}
	}
}
        
