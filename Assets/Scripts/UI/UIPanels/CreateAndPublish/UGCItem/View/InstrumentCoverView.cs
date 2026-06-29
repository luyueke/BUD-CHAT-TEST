using System.Collections;
using Game.Base;
using Game.Props.PropsManagers;
using GameData.Base;
using GameData.MapData;
using UI.BaseWidgets;

namespace UI {
    public class InstrumentCoverView : BaseCoverView {
        private InstrumentEditData instrumentEditData;

        public override void SetEditData(UGCBaseEditData data) {
            base.SetEditData(data);
            instrumentEditData = (InstrumentEditData)data;
        }

        protected override IEnumerator SetVerify() {
            yield return base.SetVerify();
            instrumentEditData.skinActionDraftInfo.SetVerify(GetCoverData());
        }

        protected override void OnNextBtnClick() {
            instrumentEditData.skinActionDraftInfo.SetCover(GetCoverData());
            instrumentEditData.GetInfo().cover = instrumentEditData.skinActionDraftInfo.GetCoverLocalUrl();
            instrumentEditData.GetInfo().coverAutoSaved = CoverSaveStatus.ManualSaved;
            nextCallback?.Invoke();

        }
    }
}

