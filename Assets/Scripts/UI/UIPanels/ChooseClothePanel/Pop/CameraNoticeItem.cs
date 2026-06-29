using Com.TheFallenGames.OSA.Util.IO;
using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class CameraNoticeItem : MonoBehaviour
{
    public RemoteImageBehaviour CreatHead;
    public Text Name;
    public Image CreatFrame;
    public Transform Trans_BottomEffect;
    public Transform Trans_TopEffect;

    public Toggle Toggle;

    private object _data;
    private Action<object> _action;
    private void Awake()
    {
        Toggle.onValueChanged.AddListener(OnClaimBtn);
    }

    void OnClaimBtn(bool bo)
    {
        if (_data is CameraNoticeCellData cellData)
        {
            cellData.isSelected = bo;
        }
        _action?.Invoke(_data);
    }

    public void SetData(object data, Action<object> action, int idx)
    {
        _data = data;
        _action = action;

        var cellData = data as CameraNoticeCellData;
        var friendInfo = cellData?.friendInfo;
        var userInfo = friendInfo?.userInfo ?? AccountDataManager.Inst.UserInfo;

        if (cellData != null)
        {
            Toggle.SetIsOnWithoutNotify(cellData.isSelected);
        }

        CreatHead.Load(userInfo.portraitUrl);
        Name.text = userInfo.nickname;

        CreatFrame.gameObject.SetActive(false);
        Trans_BottomEffect.ClearChildren();
        Trans_TopEffect.ClearChildren();
        var headCycleData = UserUIWidgetManager.Inst.GetHeadCycleData(userInfo.avatarFrame, this.gameObject);
        if (headCycleData != null)
        {
            CreatFrame.gameObject.SetActive(true);
            CreatFrame.sprite = headCycleData.Sp_HeadCycle;

            if (headCycleData.Effect_Bottom != null)
            {
                headCycleData.Effect_Bottom.Instantiate(Trans_BottomEffect);
            }

            if (headCycleData.Effect_Top != null)
            {
                headCycleData.Effect_Top.Instantiate(Trans_TopEffect);
            }
        }

    }
}