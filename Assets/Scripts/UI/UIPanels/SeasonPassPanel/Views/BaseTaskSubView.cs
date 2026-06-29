using Game.Event;
using UnityEngine;

public class BaseTaskSubView : MonoBehaviour {
    public string taskKey;
    protected SeasonPassConfig _config;

    public virtual void InitData(SeasonPassConfig config)
    {
        this._config = config;
    }
    
    public virtual void RefreshData(SeasonPassListRsp rsp) {

    }



    public virtual void RefreshData(TaskListRsp rsp) {

    }


    public virtual void OnShow() {

    }



}
