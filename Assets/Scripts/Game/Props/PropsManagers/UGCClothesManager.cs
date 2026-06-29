
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Utils;
using GameData.Config;
using GameData.BaseInfo;
using UnityEngine;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(UGCClothesBehaviour))]
	public class UGCClothesManager : BaseNodeManager
	{

		public List<SkinInfo> clothesInfos = new List<SkinInfo>();

		public IPool RemoteTexturePool { get; private set; }
		Es.GamePropData ugcClothesDameData;
		public UGCClothesManager()
		{
			ugcClothesDameData = GamePropDataHelper.GetPropIdByNodeModelType(NodeModelType.UGCClothes);
			RemoteTexturePool = new FIFOCachingPool(ugcClothesDameData.MaxNum, TextureDestroyer);
		}

		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			nodeBehaviour.entity.AddComp<UGCClothesComponent>();
			nodeBehaviour.entity.AddComp<TransactionComponent>();
		}
		
		void TextureDestroyer(object urlKey, object texture)
		{
			var asUnityObject = texture as UnityEngine.Object;
			if (asUnityObject != null)
				GameObject.Destroy(asUnityObject);
		}

		public override void OnPlay()
		{
			base.OnPlay();
			SetEmptyClothesVisible(false);
		}

		public override void OnGuest()
		{
			base.OnGuest();
			SetEmptyClothesVisible(false);
		}

		public override void OnEdit()
		{
			base.OnEdit();
			SetEmptyClothesVisible(true);
		}
		
		private void SetEmptyClothesVisible(bool isVisible)
		{
			foreach (var tmpEntity in entities)
			{
				var tmpComp = tmpEntity.entity.GetComp<UGCClothesComponent>();
				if (tmpComp == null || string.IsNullOrEmpty(tmpComp.id))
				{
					tmpEntity.gameObject.SetActive(isVisible);
				}
			}
		}


	}
}
        
