using System.Collections;
using System.Collections.Generic;
using GameData.Base;
using UnityEngine;

namespace UI
{
    public class VehicleCoverView : BaseCoverView
    {
        private VehicleEditData vehicleEditData;

        public override void SetEditData(UGCBaseEditData data)
        {
            base.SetEditData(data);
            vehicleEditData = editData as VehicleEditData;
        }

        protected override IEnumerator SetVerify()
        {
            yield return base.SetVerify();
            vehicleEditData.skinActionDraftInfo.SetVerify(GetCoverData());
        }

        protected override void OnNextBtnClick()
        {
            vehicleEditData.skinActionDraftInfo.SetCover(GetCoverData());
            vehicleEditData.GetInfo().cover = vehicleEditData.skinActionDraftInfo.GetCoverLocalUrl();
            vehicleEditData.GetInfo().coverAutoSaved = CoverSaveStatus.ManualSaved;
            nextCallback?.Invoke();

        }
    }
}