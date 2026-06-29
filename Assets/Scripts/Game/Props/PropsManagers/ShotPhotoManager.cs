
using Game.Base;
using Game.Utils;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using GameData.Config;
using UnityEngine;

namespace Game.Props.PropsManagers
{
	public enum ShotPhotoLoadState
	{
		Empty,
		Success,
		Loading,
		Fail,
	}

    [NodeBehaviourAttribute(typeof(ShotPhotoBehaviour))]
	public class ShotPhotoManager : BaseNodeManager
	{
		public IPool RemoteTexturePool;
		Es.GamePropData shotPhotoGameData;

		public ShotPhotoManager()
		{
			shotPhotoGameData = GamePropDataHelper.GetPropIdByNodeModelType(NodeModelType.ShotPhoto);
			RemoteTexturePool = new FIFOCachingPool(shotPhotoGameData.MaxNum, TextureDestoryer);
		}

		protected override void OnNotifyRelease()
		{
			base.OnNotifyRelease();
			RemoteTexturePool.Clear();
		}

		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			nodeBehaviour.entity.AddComp<ShotPhotoComponent>();
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyCreateInBuild(nodeBehaviour);
			var shotPhotoComponent = nodeBehaviour.entity.GetComp<ShotPhotoComponent>();
			var shotPhotoBehaviour = nodeBehaviour as ShotPhotoBehaviour;
			shotPhotoBehaviour.Load(shotPhotoComponent.PhotoUrl);
		}

		void TextureDestoryer(object urlKey, object texture)
		{
			var asUnityObject = texture as UnityEngine.Object;
			if (asUnityObject != null)
				GameObject.Destroy(asUnityObject);
		}
	}
}
        
