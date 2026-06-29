using System.Collections;
using GameData.Base;

namespace UI {
    public class PropCoverView : BaseCoverView {
        private PropEditData itemEditData;

        protected override void OnNextBtnClick() {
            itemEditData.draftInfo.SetCover(GetCoverData());
			itemEditData.GetInfo().coverAutoSaved = CoverSaveStatus.ManualSaved;
            base.OnNextBtnClick();
        }

        public override void SetEditData(UGCBaseEditData data) {
            base.SetEditData(data);
            itemEditData = (PropEditData)data;
        }


        protected override IEnumerator SetVerify() {
            yield return base.SetVerify();
            itemEditData.draftInfo.SetVerify(GetCoverData());
        }
    }
}
