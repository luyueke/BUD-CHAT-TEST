/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-15 18:03:05
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-26 14:47:27
 * @ Description: 传送按钮
 */


using Game.Base;
using Game.KinematicCharacter;
using Game.Avatar;
using UnityEngine;
using TMPro;
using System.Collections;
using Game.Audio;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using UIAgent;

namespace Game.Props.PropsBehaviours
{
    public class PortalButtonBehaviour : NodeBaseBehaviour
    {
		TextMeshPro textMeshPro;
		Animator animator;
		Renderer mainTexRender;
		static MaterialPropertyBlock mpb;
		KinematicCharacterController selfPlayer => AvatarController.Inst.SelfController;

		Texture normalTexture;
		Texture emptyTexture;
		string texturePath = "Assets/Loadable/Model3D/Editor_Props/Button/teleportBtn/";

		public override void OnInitByCreate()
		{
			base.OnInitByCreate();
			if (mpb == null)
            {
                mpb = new MaterialPropertyBlock();
            }
			textMeshPro = this.GetComponentInChildren<TextMeshPro>();
			animator = this.GetComponentInChildren<Animator>();
			mainTexRender = this.transform.Find("portalbutton/thumbsuptap").GetComponent<Renderer>();

			emptyTexture = XAssetLoaderMgr.Inst.LoadResource<Texture>($"{texturePath}gdgt_teleportBtn_col_TEX.png", gameObject);
			normalTexture = XAssetLoaderMgr.Inst.LoadResource<Texture>($"{texturePath}gdgt_teleportBtn_thumbsuppale_col_TEX.png", gameObject);
		}

		public override void OnTouchClick()
		{
			if (AvatarController.Inst.SelfStateController.IsLinkPlayerB())
			{
				UIAgentManager.Inst.ShowToast("牵手状态下不可以点击地图中的交互道具哦");
				return;
			}
			base.OnTouchClick();
			AkSoundManager.Inst.PlayInteractable3DSound("General_Button",gameObject);
			// 传送
			StartCoroutine(GotoNewPos());
		}

		IEnumerator GotoNewPos()
		{
			var buttonComponent = entity.GetComp<PortalButtonComponent>();
			var pointManager = GlobalNodeManager.Inst.Get<PortalPointManager>();
			var pointNode = pointManager.GetBehaviour(buttonComponent.PointUid);
			if(pointNode == null)
			{
				yield break;
			}
			animator.Play("Inacbtn", 0, 0);
			yield return new WaitForSeconds(0.3f);
			UIAgent.UIAgentManager.Inst.OpenPanel(PanelId.BlackPanel,true);
			var pointNodeTF = pointNode.transform;
			yield return new WaitForSeconds(0.5f);
			selfPlayer.Motor.SetPositionAndRotation(pointNodeTF.position, pointNodeTF.rotation);
			selfPlayer.OnTeleport();
		}
	
		public void SetNum(int num)
		{
			textMeshPro.text = num.ToString();
		}

		public void ActiveText(bool active)
		{
			textMeshPro.gameObject.SetActive(active);
			mainTexRender.GetPropertyBlock(mpb);
			if (active)
			{
				mpb.SetTexture("_BaseMap", emptyTexture);
			} else {
				mpb.SetTexture("_BaseMap", normalTexture);
			}
			mainTexRender.SetPropertyBlock(mpb);
		}
    }
}
        
