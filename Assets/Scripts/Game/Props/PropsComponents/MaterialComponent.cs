using Game.Base;
using Game.ECS;
using GameData;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class MaterialComponent : BaseComponent, IComponentSerializer
	{
		public MaterialUnionID matId = new MaterialUnionID();
		public Color color = Color.white;
		public Vector2 tile = Vector2.one;

		public void Read(PComponentData componentData)
		{
            if (componentData.CmpData.Is(PMaterialComponentData.Descriptor))
			{
				var pMaterialComponentData = componentData.CmpData.Unpack<PMaterialComponentData>();
				color = pMaterialComponentData.Cols.ToColor();
				tile = pMaterialComponentData.Tile.ToVector2();
				matId = matId.ParsePB(pMaterialComponentData);
			}
		}

		public PComponentData Write()
		{
			var componentData = new PComponentData();
			var pMaterialComponentData = new PMaterialComponentData();
			pMaterialComponentData.Tile = tile.ToPB();
			pMaterialComponentData.Cols = color.ToPB();
			pMaterialComponentData.MatId = matId.MatId;
			pMaterialComponentData.UMat = matId.UGCId;
            componentData.CmpData = Any.Pack(pMaterialComponentData);

            GameUgcMatManager.Inst.WriteUgcMat(matId);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new MaterialComponent()
			{
				matId = matId.Clone(),
				color = color,
				tile = tile,
			};
			return component;
		}
	}
}