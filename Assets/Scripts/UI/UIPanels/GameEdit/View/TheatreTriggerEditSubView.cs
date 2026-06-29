using Com.TheFallenGames.OSA.Util.IO;
using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.GameEdit
{
    public class TheatreTriggerEditSubView : BasePropertyEditSubView
    {
        [SerializeField] private GameObject btnGroup;
        [SerializeField] private CButton theatreTypeBtn;
        [SerializeField] private CButton actorTypeBtn;
        [SerializeField] private CButton triggerTypeBtn;

        [SerializeField] private GameObject infoGroup;
        [SerializeField] private RemoteImageBehaviour theatreCoverImage;
        [SerializeField] private Text theatreNameText;
        [SerializeField] private GameObject actorInfoHolder;
        [SerializeField] private Text actorNameText;
        [SerializeField] private CButton editBtn;
        [SerializeField] private CButton changeTypeBtn;

        private TheatreTriggerComponent _comp;

        protected override void OnInit()
        {
            theatreTypeBtn.onClick.AddListener(() => OpenSelectPanel(0));
            actorTypeBtn.onClick.AddListener(() => OpenSelectPanel(1));
            triggerTypeBtn.onClick.AddListener(() => OpenSelectPanel(2));
            editBtn.onClick.AddListener(OnEditBtnClick);
            changeTypeBtn.onClick.AddListener(OnChangeTypeClick);
        }

        public override void OnSelectEntity(SceneEntity entity)
        {
            base.OnSelectEntity(entity);
            _comp = selectEntity.GetComp<TheatreTriggerComponent>();
            RefreshView();
        }

        private void RefreshView()
        {
            bool hasTheatre = _comp != null && !string.IsNullOrEmpty(_comp.TheatreId);
            btnGroup.SetActive(!hasTheatre);
            infoGroup.SetActive(hasTheatre);
            if (!hasTheatre) return;
            theatreNameText.text = _comp.TheatreName;
            if (theatreCoverImage != null && !string.IsNullOrEmpty(_comp.TheatreCover))
                theatreCoverImage.Load(_comp.TheatreCover);
            bool isActor = _comp.TriggerType == 1;
            actorInfoHolder.SetActive(isActor);
            if (isActor)
                actorNameText.text = _comp.ActorName;
        }

        private void OnChangeTypeClick()
        {
            if (_comp == null) return;
            _comp.TheatreId = "";
            RefreshView();
        }

        private void OpenSelectPanel(int triggerType)
        {
            var panel = UIManager.Inst.OpenPanel<TheatreTriggerSelectPanel>(PanelId.TheatreTriggerSelectPanel, triggerType);
            panel.SetOnConfirm(OnSelectConfirm);
        }

        private void OnEditBtnClick()
        {
            if (_comp == null) return;
            OpenSelectPanel(_comp.TriggerType);
        }

        private void OnSelectConfirm(TheatreTriggerSelectResult result)
        {
            if (_comp == null) return;
            _comp.TriggerType = result.TriggerType;
            _comp.TheatreId = result.TheatreId;
            _comp.TheatreName = result.TheatreName;
            _comp.TheatreCover = result.TheatreCover;
            _comp.ActorId = result.ActorId;
            _comp.ActorName = result.ActorName;
            _comp.ClothesIndex = result.ClothesIndex;
            _comp.ClothesName = result.ClothesName;
            RefreshView();
            var behaviour = selectEntity?.GetBehaviour<TheatreTriggerBehaviour>();
            GlobalNodeManager.Inst.Get<TheatreTriggerManager>()?.RefreshVisual(behaviour, result.TriggerType);
        }
    }
}
