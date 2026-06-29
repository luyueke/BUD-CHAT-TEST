/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-15 18:45:41
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-16 15:58:26
 * @ Description: 传送光柱
 */


using Game.Base;
using UnityEngine;
using TMPro;

namespace Game.Props.PropsBehaviours
{
    public class PortalPointBehaviour : NodeBaseBehaviour
    {
        TextMeshPro textMeshPro;

		public override void OnInitByCreate()
		{
			base.OnInitByCreate();
			textMeshPro = this.GetComponentInChildren<TextMeshPro>();
		}
	
		public void SetNum(int num)
		{
			textMeshPro.text = num.ToString();
		}
    }
}
        
