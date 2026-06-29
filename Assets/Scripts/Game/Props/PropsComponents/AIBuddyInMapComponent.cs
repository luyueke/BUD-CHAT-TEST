
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class AIBuddyInMapComponent : BaseComponent, IComponentSerializer
	{
		public string ShowText = "";
		public string AiBuddyID = "";       // Cabin 伙伴 CabinCharacterUgcInfo.id
		public string SkinPackId = "";      // 编辑态选中的皮肤 packId（空=默认皮肤），运行时固定
		public string BoxId = "";           // 盒子 BoxSceneInfo.id（空=无盒子）
		public string BoxMetaUrl = "";      // 盒子 metaDataUrl，烘焙进数据供访客直接渲染外观
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<AIBuddyInMapComponentData>(out var pbBodyData))
			{
				AiBuddyID = pbBodyData.AiId;
				ShowText = pbBodyData.ShowText;
				SkinPackId = pbBodyData.SkinPackId;
				BoxId = pbBodyData.BoxId;
				BoxMetaUrl = pbBodyData.BoxMetaUrl;
			}
		}

		public PComponentData Write()
		{
			if (string.IsNullOrEmpty(AiBuddyID))
			{
				return null;
			}
			var pbBodyData = new AIBuddyInMapComponentData();
			pbBodyData.AiId = AiBuddyID;
			pbBodyData.ShowText = ShowText;
			pbBodyData.SkinPackId = SkinPackId ?? "";
			pbBodyData.BoxId = BoxId ?? "";
			pbBodyData.BoxMetaUrl = BoxMetaUrl ?? "";
			var componentData = new PComponentData();
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new AIBuddyInMapComponent()
			{
				AiBuddyID = this.AiBuddyID,
				ShowText = this.ShowText,
				SkinPackId = this.SkinPackId,
				BoxId = this.BoxId,
				BoxMetaUrl = this.BoxMetaUrl,
			};
			return component;
		}
	}
}
        
