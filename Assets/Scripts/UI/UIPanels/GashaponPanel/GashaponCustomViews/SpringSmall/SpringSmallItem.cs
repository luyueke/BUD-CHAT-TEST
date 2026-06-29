using System;
using Game.Store;
using GameData.Gashapon;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class SpringSmallItem : MonoBehaviour {
    [SerializeField] private Image iconImage;

    [SerializeField] private Image bgImage;

    [SerializeField] private GameObject specialMarkObj;

    [SerializeField] private Sprite defaultSprite;

    [SerializeField] private Sprite specialSprite;

    [SerializeField] private GameObject ownedMarkObj;
    [SerializeField] private GameObject specialOwnedMarkObj;
    [SerializeField] private Text numberTxt;

    private RewardItem _rewardItem;
    private Action<string> onClickCallBack;

    private string atlasPath = "Assets/Loadable/UI/UIPanel/GashaponPanel/GashaponPanel.spriteatlas";


    private void Awake() {
        GetComponent<CButton>().onClick.AddListener(OnItemClicked);
    }
    
    public void Init(RewardItem rewardItem, Action<string> callBack = null) {
        _rewardItem = rewardItem;
        bgImage.sprite = rewardItem.rewardId == "100051" ? specialSprite : defaultSprite;
        iconImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, rewardItem.rewardIcon1, gameObject);
        if (rewardItem.pgcIds != null && rewardItem.pgcIds.Count > 0)
        {
            bool isOwned = AssetsDataManager.IsOwned(rewardItem.pgcIds[0]);
            ownedMarkObj.SetActive(isOwned);
        }
        else
        {
            ownedMarkObj.SetActive(false);
        }

        int num = rewardItem.num;
        numberTxt.gameObject.SetActive(num > 1);
        numberTxt.text = "x" + num.ToString();
        onClickCallBack = callBack;
    }


    public void RefreshOwnedMark(GashaponInfoRsp gashaponInfoRsp)
    {
        if (gashaponInfoRsp == null || gashaponInfoRsp.rewardPool == null) return;
        var drawnInfo = gashaponInfoRsp.rewardPool.Find(tmp => tmp.rewardId.ToString() == _rewardItem.rewardId);
        if (drawnInfo == null) return;
        ownedMarkObj.SetActive(drawnInfo.everDrawn == 1);
        numberTxt.gameObject.SetActive(drawnInfo.everDrawn != 1);
    }

    private void OnItemClicked() {
        onClickCallBack?.Invoke(_rewardItem.rewardId);
    }

}
