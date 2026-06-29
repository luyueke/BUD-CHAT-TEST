using System;
using FSM;

namespace UI {
    public class UGCPublishStateBase {
        public UGCPublishState stateID {
            get;
        }

        protected UGCBaseStateView stateView;
        public UGCPublishStateMachine machine;

        public UGCBaseEditData editData;

        public UGCPublishStateBase(UGCPublishState id) {
            stateID = id;
        }

        private Action onNextCallback;
        private Action onPrevCallback;

        public virtual void InitData(UGCBaseEditData data, params object[] args) {

            LoggerUtils.Log($"UGCItemState InitData {stateID}");
            editData = data;
            var publishPanel = UIManager.Inst.FindPanel<UGCPublishPanel>(PanelId.UGCPublishPanel);
            if (publishPanel == null) {
                publishPanel = UIManager.Inst.OpenPanel<UGCPublishPanel>(PanelId.UGCPublishPanel);
            }
            stateView = publishPanel.GetView(stateID);
            if (stateView != null) {
                stateView.SetEditData(editData);
            }
        }

        public virtual void OnEnter() {
            if (stateView != null) {
                stateView.Show();
                stateView.SetNext(NextState);
                stateView.SetBack(PrevState);
            }
        }

        public virtual void OnExit() {
            if (stateView != null) {
                stateView.Hide();
            }
        }

        protected virtual void NextState() {
            onNextCallback?.Invoke();
            if (machine == null) {
                onNextCallback?.Invoke();
                return;
            }
            var nextState = machine.GetNextState();
            if (nextState != UGCPublishState.None) {
                machine.EnterState(nextState);
            } else {
                OnExit();
                UIManager.Inst.ClosePanel(PanelId.UGCPublishPanel);
            }
        }

        protected virtual void PrevState() {
            onPrevCallback?.Invoke();
            if (machine == null) {
                return;
            }
            var nextState = machine.GetPrevState();
            if (nextState != UGCPublishState.None) {
                machine.EnterState(nextState);
            } else {
                OnExit();
                UIManager.Inst.ClosePanel(PanelId.UGCPublishPanel);
            }
        }

        public virtual void SetPreCallback(Action callback) {
            onPrevCallback = callback;
        }

        public virtual void SetNextCallback(Action callback) {
            onNextCallback = callback;
        }


    }
}
