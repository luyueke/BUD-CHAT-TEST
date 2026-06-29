using System;
using Basic.Utils;
using Es;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Game.Utils;
using UI.BaseWidgets;
using UI.UIPanels.GameEdit;
using UI.UIPanels.ProfilePanel;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class InteractiveBoardSubView : BasePropertyEditSubView {
    [SerializeField] private TextInputView InputText;
    [SerializeField] private CButton EmoteEditBtn;
    [SerializeField] private Text EmoteName;

    private InteractiveBoardComponent _boardComponent;

    protected override void OnInit() {
        InputText.SetOnInput(OnSetName);
        EmoteEditBtn.onClick.AddListener(OnEmoteEditClick);
    }

    public override void OnSelectEntity(SceneEntity entity) {
        base.OnSelectEntity(entity);
        _boardComponent = selectEntity.GetComp<InteractiveBoardComponent>();
        EmoteName.SetText(_boardComponent.EmoteName);
        InputText.SetInputWithoutNotify(_boardComponent.ShowText);
    }

    private void OnSetName(string nameStr) {
        if (_boardComponent != null) {
            _boardComponent.ShowText = nameStr;
        }
    }

    private void OnEmoteEditClick() {
        LoggerUtils.Log("InteractiveBoardPanel OnEmoteEditClick");
        // SelectEmotePanel.Show();
        // var tComp = selectEntity.GetComp<InteractiveBoardComponent>();
        // SelectEmotePanel.Instance.ShowDefault(tComp.emoId);
        // SelectEmotePanel.Instance.DoneListener.AddListener(OnSelectDoneClick);
        // var emoteSelectPanel = UIManager.Inst.OpenPanel<EmoteSelectPanel>(PanelId.EmoteSelectPanel,_boardComponent.EmoteId);
        // emoteSelectPanel.DoneListener.AddListener(OnSelectEmote);

        Action<string, string> callBack = OnSelectEmote;

        UIManager.Inst.OpenPanel<InteractiveEmoteSelectPanel>(PanelId.InteractiveEmoteSelectPanel,
            _boardComponent.EmoteId,
            callBack);
    }

    private void OnSelectEmote(string emoteId, string emoteName) {


        if (_boardComponent != null)
        {
            _boardComponent.EmoteId = emoteId;
            _boardComponent.EmoteName = emoteName;
        }

        EmoteName.SetText(emoteName);
    }
}
