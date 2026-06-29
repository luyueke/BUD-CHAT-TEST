using System;
using System.Data;
using Es;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class CameraModeActionBusiness : MonoBehaviour
{
    [SerializeField] private Button buyBtn;
    [SerializeField] private Button previewBtn;
    [SerializeField] private Button closeBtn;
    [SerializeField] private Text title;
    [SerializeField] private Text description;
    [SerializeField] private Image itemImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private CameraModeActionPreview cameraModeActionPreview;

    //[SerializeField] private GameObject buyRoot;
    //[SerializeField] private Button buyConfirmBtn;
    //[SerializeField] private Button buyCloseBtn;

    private bool isInit = false;
    private string id;
    private Action<bool, string> onClick;   

    private const string IconSpritePath = "Assets/Loadable/UI/UIPanel/CameraModePanel/CameraModePanel.spriteatlas";

    public void Init(string id, Action<bool, string> onClick){
        this.id = id;
        this.onClick = onClick;

        if(!isInit){
            isInit = true;
            InitUI();
        }

        if(cameraModeActionPreview != null) cameraModeActionPreview.gameObject.SetActive(false);
        string iconName = "";

        var config = DataTables.GetCameraSelfiePose(id);
        if(config != null){
            if(title != null) title.SetLocalText($"{config.name}");
            if(description != null) description.SetLocalText($"需要花费{config.price}");
            if(itemImage != null) itemImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(IconSpritePath, config.iconString, gameObject);
        
            PgcUtils.CurrencyIconPath.TryGetValue((CurrencyType)config.currency, out iconName);
            var commonPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Common);
            iconImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject);
        }
    }

    private void InitUI(){
        if(buyBtn == null) buyBtn = GameObjectEx.FindComponentByName<Button>(transform, "BuyBtn");
        if(previewBtn == null) previewBtn = GameObjectEx.FindComponentByName<Button>(transform, "PreviewBtn");
        if(closeBtn == null) closeBtn = GameObjectEx.FindComponentByName<Button>(transform, "CloseBtn");
        if(title == null) title = GameObjectEx.FindComponentByName<Text>(transform, "Title");
        if(description == null) description = GameObjectEx.FindComponentByName<Text>(transform, "Description");
        if(itemImage == null) itemImage = GameObjectEx.FindComponentByName<Image>(transform, "ItemImage");
        if(cameraModeActionPreview == null) cameraModeActionPreview = GameObjectEx.FindComponentByName<CameraModeActionPreview>(transform.parent, "CameraModeActionPreview");
        //if(buyRoot == null) buyRoot = GameObjectEx.FindChildByName(transform, "BuyRoot").gameObject;
        //if(buyConfirmBtn == null) buyConfirmBtn = GameObjectEx.FindComponentByName<Button>(transform, "BuyConfirmBtn");
        //if(buyCloseBtn == null) buyCloseBtn = GameObjectEx.FindComponentByName<Button>(transform, "BuyCloseBtn");

        if(buyBtn != null) buyBtn.onClick.AddListener(OnBuyBtnClick);
        if(previewBtn != null) previewBtn.onClick.AddListener(OnPreviewBtnClick);
        if(closeBtn != null) closeBtn.onClick.AddListener(OnCloseBtnClick);
        //if(buyConfirmBtn != null) buyConfirmBtn.onClick.AddListener(OnBuyConfirmBtnClick);
        //if(buyCloseBtn != null) buyCloseBtn.onClick.AddListener(OnBuyCloseBtnClick);
    }

    private void OnBuyBtnClick(){
        //if(buyRoot != null) buyRoot.SetActive(true);
        if(!string.IsNullOrEmpty(id)){

            JObject req = new JObject()
            {
                ["productType"] = 2,
                ["productId"] = id,
            };

            NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.BuyProductPay, 
            HttpMethod.POST, 
            JsonConvert.SerializeObject(req), 
            (response) => {
                TipPanel.ShowToast("购买成功");
                onClick?.Invoke(true, id);
                gameObject.SetActive(false);
            }, 
            (error) => {
                var rsp = JsonConvert.DeserializeObject<HttpResponseFailDataStruct>(error);
                TipPanel.ShowToast(rsp.rmsg);
            });
        }
    }

    private void OnCloseBtnClick(){
        gameObject.SetActive(false);
        onClick?.Invoke(false, id);
    }

    private void OnPreviewBtnClick(){
        if(string.IsNullOrEmpty(id)){
            return;
        }

        if(cameraModeActionPreview == null){
            cameraModeActionPreview = GameObjectEx.FindComponentByName<CameraModeActionPreview>(transform.parent, "CameraModeActionPreview");
        }

        if(cameraModeActionPreview != null){
            cameraModeActionPreview.gameObject.SetActive(true);
            cameraModeActionPreview.Init(id);
        }
    }

    // private void OnBuyConfirmBtnClick(){
    //     //二次确认购买用的，目前需求中没有二次确认，此为保留代码
    // }

    // private void OnBuyCloseBtnClick(){
    //     gameObject.SetActive(false);

    //     onClick?.Invoke(false, id);
    // }

}