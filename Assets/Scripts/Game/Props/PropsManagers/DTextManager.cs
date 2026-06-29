
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(DTextBehaviour))]
	public class DTextManager : BaseNodeManager
	{
		private const string DefaultInputStr = "请输入文字";
		protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour) 
		{
			nodeBehaviour.entity.AddComp<DTextComponent>();
			RefreshNode(nodeBehaviour as DTextBehaviour);
		}

		protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
		{
			base.OnNotifyCreateInClone(oldBehaviour, newBehaviour);
			
			RefreshNode(newBehaviour as DTextBehaviour);
		}

		protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
		{
			base.OnNotifyCreateInBuild(nodeBehaviour);

			RefreshNode(nodeBehaviour as DTextBehaviour);
		}

		void RefreshNode(DTextBehaviour behaviour)
		{
			if (behaviour == null || behaviour.entity == null || !behaviour.entity.HasComp<DTextComponent>()) return;
			var dTextComponent = behaviour.entity.GetComp<DTextComponent>();
			behaviour.SetColor(dTextComponent.TextColor);
			if (string.IsNullOrEmpty(dTextComponent.Content) || dTextComponent.Content == DefaultInputStr)
			{
				string defaultStr = LocalizationManager.Inst.GetLocalizedText("请输入文字");
				behaviour.SetText(defaultStr);
			} else {
				behaviour.SetText(dTextComponent.Content);
			}
		}
	}
}
        
