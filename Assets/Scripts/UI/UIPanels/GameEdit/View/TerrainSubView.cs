/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-14 16:28:27
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-14 16:43:33
 * @ Description:
 */

using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using UnityEngine;
 using UnityEngine.UI;

namespace UI.UIPanels.GameEdit
{
	public class TerrainSubView : BasePropertyEditSubView
	{
        [SerializeField] private Toggle visibleToggle;

        TerrainComponent terrainComponent;
        TerrainBehaviour behv;

		protected override void OnInit()
		{
            var manager = GlobalNodeManager.Inst.Get<TerrainManager>();
            var terrain = manager.GetTerrain();

            terrainComponent = terrain.entity.GetComp<TerrainComponent>();
            behv = terrain.GetComponent<TerrainBehaviour>();
            
			visibleToggle.onValueChanged.AddListener(OnVisibleTogChange);
            visibleToggle.SetIsOnWithoutNotify(!terrainComponent.IsVisible);
		}

        void OnVisibleTogChange(bool isOn)
        {
            terrainComponent.IsVisible = !isOn;
            behv.HideTerrain(isOn);
        }
	}
}