
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class UGCClothesComponent : BaseComponent, IComponentSerializer
	{

		public string id = "";
		public string templateId = "";
		public string cover = "";
		public string clothesUrl = "";
		public string metaDataUrl = "";
		public bool isPgc = false;
		
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData != null &&  componentData.CmpData.Is(PUGCClothesComponent.Descriptor))
			{
				if (componentData.CmpData.TryUnpack<PUGCClothesComponent>(out var pbBodyData))
				{
					id = pbBodyData.Id;
					templateId = pbBodyData.TemplateId;
					cover = pbBodyData.Cover;
					clothesUrl = pbBodyData.ClothesUrl;
					metaDataUrl = pbBodyData.MetaDataUrl;
					isPgc = pbBodyData.IsPgc;
				}
			}
		}

		public PComponentData Write()
		{
			var componentData = new PComponentData();
			var pUGCClothesComponentData = new PUGCClothesComponent()
			{
				Id = id,
				TemplateId = templateId,
				Cover = cover,
				ClothesUrl = clothesUrl,
				MetaDataUrl = metaDataUrl,
				IsPgc = isPgc
			};
			componentData.CmpData = Any.Pack(pUGCClothesComponentData);
			return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new UGCClothesComponent()
			{
				id = id,
				clothesUrl = clothesUrl,
				cover = cover,
				metaDataUrl = metaDataUrl,
				templateId = templateId,
				isPgc = isPgc
			};
			return component;
		}
	}
}
        
