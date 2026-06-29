
using Basic.Utils;
using GameData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UnityEngine;

public class FavoritesButton : CommonUIWidget
{
    public CButton Btn_Fav;
    public GameObject Go_Fav;
    public CText Txt_FavNum;

    private string _ugcId;
    private bool _isFAv;
    private int _favAmount;
    private InteractInfoView _interactInfoView;

    private void Awake()
    {
        Btn_Fav.onClick.AddListener(OnBtnFavClick);
    }

    /// <summary>
    /// 点赞需要的参数：
    /// args[0]:UgcId
    /// args[1]:当前收藏情况 0 未收藏 1 已收藏
    /// args[2]:当前收藏数量
    /// </summary>
    /// <param name="args"></param>
    public override void SetData(params object[] args)
    {
        base.SetData(args);

        _ugcId = (string)args[0];
        _isFAv = (int)args[1] == 1;
        _favAmount = (int)args[2];

        RefreshFavState();
        RefreshFavAmount();
    }
    
    public void BindInteractInfoView(InteractInfoView view)
    {
        _interactInfoView = view;
    }

    private void RefreshFavState()
    {
        Go_Fav.SetActive(_isFAv);
    }

    private void RefreshFavAmount()
    {
        var FavNumStr = GameUtils.ToBudCommonNumString(_favAmount);
        Txt_FavNum.text = FavNumStr;
        
        if(_interactInfoView != null)
            _interactInfoView.RefreshFavNum(_favAmount);
    }

    private void OnBtnFavClick()
    {
        JObject req = new JObject()
        {
            ["id"] = _ugcId,
            ["setType"] = _isFAv ? (int)CollectType.UnCollect : (int)CollectType.Collect,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCCollect, HttpMethod.POST, JsonConvert.SerializeObject(req), (content)=>{
            if (_isFAv)
            {
                _favAmount--;
            }
            else
            {
                _favAmount++;
            }
            _isFAv = !_isFAv;
            RefreshFavState();
            
            RefreshFavAmount();
        },null);
    }
    
}
