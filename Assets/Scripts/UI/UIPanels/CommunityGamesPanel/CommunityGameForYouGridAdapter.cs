using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.Util.IO;
using frame8.Logic.Misc.Other.Extensions;

namespace Game.CommunityGame
{
	public class CommunityGameForYouGridAdapter : BaseSectionInfoAdapter
	{
		public Action InteractiveCallback;
		protected override void OnCellViewsHolderCreated(BaseSectionInfoViewsHolder cellVH, CellGroupViewsHolder<BaseSectionInfoViewsHolder> cellGroup)
		{
			base.OnCellViewsHolderCreated(cellVH, cellGroup);
			cellVH.IconRemoteImageBehaviour.InitializeWithPool(texturePool);
		}

		protected override void UpdateCellViewsHolder(BaseSectionInfoViewsHolder newOrRecycled)
		{
			base.UpdateCellViewsHolder(newOrRecycled);
			var model = Data[newOrRecycled.ItemIndex];
			newOrRecycled.UpdateViews(model);
			newOrRecycled.SetInteractiveCallback(InteractiveCallback);
			newOrRecycled.IconRemoteImageBehaviour.Load(model?.ugcInfo?.cover);
		}
	}
}
