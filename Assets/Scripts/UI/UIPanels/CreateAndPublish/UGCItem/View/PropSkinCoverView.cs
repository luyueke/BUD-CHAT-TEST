using System.Collections;
using Game.Base;
using Game.Props.PropsManagers;
using GameData.Base;
using GameData.MapData;
using UI.BaseWidgets;

namespace UI {
    public class PropSkinCoverView : BaseCoverView {
        private SkinEditData skinEditData;

        public override void SetEditData(UGCBaseEditData data) {
            base.SetEditData(data);
            skinEditData = editData as SkinEditData;
        }

        protected override IEnumerator SetVerify() {
            yield return base.SetVerify();
            skinEditData.draftInfo.SetVerify(GetCoverData());
        }

        protected override void OnNextBtnClick() {
            skinEditData.draftInfo.SetCover(GetCoverData());
            skinEditData.GetInfo().cover = skinEditData.draftInfo.GetCoverLocalUrl();
            skinEditData.GetInfo().coverAutoSaved = CoverSaveStatus.ManualSaved;
            nextCallback?.Invoke();

        }
    }
}
