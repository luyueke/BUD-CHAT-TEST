using System;
using Com.TheFallenGames.OSA.Util.IO;
using GroupConsume;
using UI.BaseWidgets;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.UI;

public class GroupConsumeMemberView : MonoBehaviour {
    [SerializeField] private GameObject captainObj;

    [SerializeField] private GameObject nickNameTextBg;
    [SerializeField] private Text nickNameText;

    [SerializeField] private CButton inviteBtn;
    [SerializeField] private CButton headButton;

    [SerializeField] private Text indexText;

    [SerializeField] private Text consumeText;
    [SerializeField] private HeadViewWidget _headViewWidget;
    [SerializeField] GameObject topEffectObj;

    private Action inviteCallBack;

    private MemberInfo mInfo;

    private void Awake() {
        inviteBtn.onClick.AddListener(OnInviteBtnClicked);

        headButton.onClick.AddListener(() =>
        {
            if (mInfo?.userInfo == null)
            {
                return;
            }

            string uid = mInfo.userInfo.uid;
            if (string.IsNullOrEmpty(uid))
            {
                return;
            }

            UIManager.Inst.OpenPanel<ProfilePanel>(PanelId.ProfilePanel, uid);
        });
    }



    public void SetMemberInfo(int index, GroupStatus status, MemberInfo memberInfo, Action callBack = null) {
        nickNameTextBg.gameObject.SetActive(false);
        indexText.SetText(index.ToString());
        inviteBtn.gameObject.SetActive(true);
        captainObj.SetActive(false);
        inviteCallBack = callBack;
        topEffectObj.SetActive(false);
        consumeText.transform.parent.gameObject.SetActive(false);
        this.mInfo = memberInfo;
        if (memberInfo != null) {
            topEffectObj.SetActive(true);
            nickNameTextBg.gameObject.SetActive(true);
            inviteBtn.gameObject.SetActive(false);
            //consumeText.transform.parent.gameObject.SetActive(status == GroupStatus.Finished);
            consumeText.transform.parent.gameObject.SetActive(true);
            captainObj.SetActive(memberInfo.groupRole == GroupRole.Captain);
            nickNameText.SetText(memberInfo.userInfo.nickname);
            consumeText.SetText(memberInfo.amount.ToString());
            var sizeDelta = consumeText.rectTransform.sizeDelta;
            //consumeText.rectTransform.sizeDelta = new Vector2(consumeText.preferredWidth, sizeDelta.y);
            // var cycleId = memberInfo.userInfo.avatarFrame;
            // var headCycleData = UserUIWidgetManager.Inst.GetHeadCycleData(cycleId, this.gameObject);
            // headCycle.sprite = headCycleData.Sp_Cycle;
            _headViewWidget.gameObject.SetActive(true);
            _headViewWidget.InitHeadCycle(memberInfo.userInfo);
        }
        else
        {
            _headViewWidget.gameObject.SetActive(false);
            _headViewWidget.InitHeadCycle("","", 0);
        }

    }


    private void OnInviteBtnClicked() {
        inviteCallBack?.Invoke();
    }

}
