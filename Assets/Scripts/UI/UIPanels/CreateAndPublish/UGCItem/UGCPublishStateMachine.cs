using System;
using System.Collections.Generic;
using System.Linq;
using FSM;

namespace UI {
    public class UGCPublishStateMachine  {
        // 存储所有状态
        private Dictionary<UGCPublishState, UGCPublishStateBase> allStateDic;
        private List<UGCPublishStateBase> allStateList;
        private UGCPublishStateBase currentState;
        private Action finishCallBack;
        private Action cancelCallBack;
        private UGCBaseEditData editData;


        public void SetStates(List<UGCPublishStateBase> states) {
            if (states == null) {
                return;
            }
            allStateList = states;
            foreach (var state in states) {
                AddState(state);
            }
        }

        public void SetEditData(UGCBaseEditData data) {
            editData = data;
        }

        public UGCBaseEditData GetEditData() {
            return editData;
        }

        public UGCPublishStateMachine() {
            Init();
        }

        protected void Init() {
            allStateDic = new Dictionary<UGCPublishState, UGCPublishStateBase>();
        }

        public void AddState(UGCPublishStateBase state) {
            if (!allStateDic.ContainsKey(state.stateID)) {
                var ugcItemState = state;
                allStateDic.Add(state.stateID, ugcItemState);
                state.machine = this;
                ugcItemState.editData = editData;
            }
        }

        public void EnterState(UGCPublishState stateID, params object[] args) {
            if (!allStateDic.TryGetValue(stateID, out UGCPublishStateBase state)) return;
            currentState?.OnExit();
            state.InitData(editData, args);
            state.OnEnter();
            currentState = state;
        }

        public void NextState() {

        }

        public void PrevState() {

        }

        public UGCPublishState GetNextState() {
            int currentStateIndex = 0;
            if (currentState != null) {
                currentStateIndex = allStateList.IndexOf(currentState);
                if (currentStateIndex >= allStateList.Count - 1) {
                    finishCallBack?.Invoke();
                    return UGCPublishState.None;
                }
                currentStateIndex++;
            }
            return allStateList[currentStateIndex].stateID;
        }

        public UGCPublishState GetPrevState() {
            if (currentState != null) {
                int currentStateIndex = allStateList.IndexOf(currentState);
                if (currentStateIndex <= 0) {
                    cancelCallBack?.Invoke();
                    return UGCPublishState.None;
                }
                currentStateIndex--;
                return allStateList[currentStateIndex].stateID;
            } else {
                cancelCallBack?.Invoke();
                return UGCPublishState.None;
            }

        }

        public void Start() {
            EnterState(allStateList.First().stateID);
        }


        public void SetFinishCallBack(Action callBack) {
            finishCallBack = callBack;
        }

        public void SetCancelCallBack(Action callBack) {
            cancelCallBack = callBack;
        }

    }
}
