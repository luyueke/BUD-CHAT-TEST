using System.Collections.Generic;
using UI.Base;

namespace UI {
    public class UGCPublishPanel : BasePanel<UGCPublishPanel> {


        private Dictionary<UGCPublishState, UGCBaseStateView> stateViews;
        private PreviewLightRecordData _srcLightRecordData;
        public override void OnCreate() {
            base.OnCreate();
            stateViews = new Dictionary<UGCPublishState, UGCBaseStateView>();
            var allViews = GetComponentsInChildren<UGCBaseStateView>(true);
            foreach (var view in allViews) {
                stateViews.Add(view.state, view);
                view.gameObject.SetActive(false);
            }
        }

       
        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            _srcLightRecordData = AmbientLightManager.Inst.OpenPreviewDirLight();
        }

        public override void OnHidden()
        {
            base.OnHidden();
            AmbientLightManager.Inst.ClosePreviewDirLight(_srcLightRecordData);
        }

        public T GetView<T>(UGCPublishState state) where T : UGCBaseStateView {
            stateViews.TryGetValue(state, out UGCBaseStateView view);
            return view as T;
        }

        public UGCBaseStateView GetView(UGCPublishState state) {
            stateViews.TryGetValue(state, out UGCBaseStateView view);
            return view;
        }

    }
}
