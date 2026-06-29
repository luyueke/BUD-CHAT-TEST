using Game.Event;
using UnityEngine;

public class BaseSeasonView : MonoBehaviour {


    public virtual bool HandleBackBtnClick() {
        return false;
    }

    public virtual void OnCreate() {

    }

    public virtual void OnShow() {

    }


    public virtual void RefreshData(SeasonPassListRsp rsp) {

    }


    public virtual void RefreshData(TaskListRsp rsp) {

    }


}
