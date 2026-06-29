using Message;
using Network.Message;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.TopList;
using UnityEngine;
using UnityEngine.UI;

public class MapHeatContributionItem : MonoBehaviour
{
    private RankItem _itemData;
    [SerializeField] private CButton _selfBtn;
    [SerializeField] private RawImage _headIcon;
    [SerializeField] private Text _rankNo;
    [SerializeField] private Text _userName;
    [SerializeField] private Text _contribution;
    [SerializeField] private CommonSpriteSwitch _rankImgTag;
    [SerializeField] private HeadViewWidget user;
    public void InitData(RankItem data)
    {
        _itemData = data;
        _selfBtn.onClick.AddListener(OnClick);
        UpdateUI();
        if (data.rank == 1)
        {
            OnClick();
        }
    }

    private void UpdateUI()
    {
        user.InitHeadCycle(_itemData.userInfo);
        _userName.text = _itemData.userInfo.nickname;
        _contribution.text = _itemData.score.ToString();
        _rankNo.text = _itemData.rank.ToString();
        _rankImgTag.gameObject.SetActive(_itemData.rank <= 3);
        _rankImgTag.Switch(_itemData.rank-1<_rankImgTag._imgList.Count?(uint)(_itemData.rank-1):0);
        var spriteSwitch = _selfBtn.GetComponent<CommonSpriteSwitch>();
        spriteSwitch?.Switch(_itemData.rank  < spriteSwitch._imgList.Count ? (uint)(_itemData.rank) : 0);
    }

    private void OnClick()
    {
        LoggerUtils.Log($"MapHeatContributionItem index = {_rankNo.text}  onclick");
        //todo 把数据信息返回到父节点界面上

        if (!string.IsNullOrEmpty(_itemData.userInfo.avatarJson))
        {
            MessageHelper.Broadcast(MessageName.OnS9HeatRankItemClick, _itemData.userInfo.avatarJson);
        }
    }
}
