

using Game.Config;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;
using Game.ECS;

/**
* @ Author: Jun Zhou
* @ Create Time: 2023-08-17 15:42:00
* @ Modified by: Jun Zhou
* @ Modified time: 2023-08-17 16:06:39
* @ Description: 音乐板UI适配器
*/
namespace UI.UIPanels.GameEdit
{
	public class MusicPadViewAdapter : BasePropertyAdapter
	{
        PgcListEditSubView leftSubView;
        PgcListEditSubView midSubView;
        PgcListEditSubView rightSubView;

		MusicPadBehaviour bev;
        MusicPadComponent component;

		protected override void OnCreate()
		{
			var wrapper = Loader.Load<GameObject>($"Assets/Loadable/UI/UIPanel/GameEdit/Item/MusicSelectItem.prefab");
            var go = wrapper.Instantiate(this.transform);
			var iconSelectItemPrefab = go.GetComponent<GameIconLoadItem>();

            leftSubView = AddTabView<PgcListEditSubView>("左键");
            midSubView = AddTabView<PgcListEditSubView>("中键");
            rightSubView = AddTabView<PgcListEditSubView>("右键");
			leftSubView.AddItemSelectListener((id)=>OnItemSelect(0, id));
			midSubView.AddItemSelectListener((id)=>OnItemSelect(1, id));
			rightSubView.AddItemSelectListener((id)=>OnItemSelect(2, id));
			leftSubView.AddUndoSelectListener((id)=>OnItemSelect(0, id));
			midSubView.AddUndoSelectListener((id)=>OnItemSelect(1, id));
			rightSubView.AddUndoSelectListener((id)=>OnItemSelect(2, id));
			leftSubView.AddItemCreateListener(OnItemCreate);
			midSubView.AddItemCreateListener(OnItemCreate);
			rightSubView.AddItemCreateListener(OnItemCreate);

			var stoneConfig = MusicPadConfig.All;
			leftSubView.InitConfig(stoneConfig, iconSelectItemPrefab);
			midSubView.InitConfig(stoneConfig, iconSelectItemPrefab);
			rightSubView.InitConfig(stoneConfig, iconSelectItemPrefab);
		}

		protected override void OnStart()
		{
			base.OnStart();
			leftSubView.SelectItemWithNoNotify(component.KeyIds[0]);
			midSubView.SelectItemWithNoNotify(component.KeyIds[1]);
			rightSubView.SelectItemWithNoNotify(component.KeyIds[2]);

			SelectTabItem(1);
		}

		protected override void OnSelectEntity()
		{
			var go = selectEntity.GetViewGo();
            component = selectEntity.GetComp<MusicPadComponent>();
			bev = selectEntity.GetBehaviour<MusicPadBehaviour>();
		}

        void OnItemSelect(int areaIndex, string propId)
        {
            bev.SetColor(areaIndex, propId);
			component.KeyIds[areaIndex] = propId;
        }

		void OnItemCreate(int index, GameIconSelectItem item)
		{
			var bg = item.transform.Find("Bg").GetComponent<Image>();
			Color color;
			if (ColorUtility.TryParseHtmlString(MusicPadConfig.All[index].NormalColor, out color))
			{
				bg.color = color;
			}
		}
	}
}