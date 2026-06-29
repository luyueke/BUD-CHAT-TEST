using System;
using System.Collections.Generic;
using Game.Avatar;
using Game.Database;
using GameData;
using GameData.UGCData;
using Message;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using xasset;

public class BuyBoxPartnerItemsPanel : BasePanel<BuyBoxPartnerItemsPanel>
{
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private CButton BuyBtn;
    [SerializeField] private Text PriceTxt;
    [SerializeField] private Transform BoxModelRoot;
    [SerializeField] private AvatarCameraController AvatarCameraController;

    private CharacterBoxInfo _data;
    private Action _onSuccess;
    private Action _onClose;
    private GameObject _boxModel;
    private readonly List<Texture2D> _boxTextures = new List<Texture2D>();

    private const string BoxModelPath =
        "Assets/Loadable/Avatar/UGCRolePart/ModelPrefab/UGCBoxScene/qiye_ugcBreedingFarm_1_3d.prefab";

    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        BuyBtn.onClick.AddListener(OnBuyBtnClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _data = args?.Length > 0 ? args[0] as CharacterBoxInfo : null;
        if (_data == null)
        {
            Debug.LogError("BuyBoxPartnerItemsPanel: 盒子数据为空");
            return;
        }
        _onSuccess = args?.Length > 1 ? args[1] as Action : null;
        _onClose  = args?.Length > 2 ? args[2] as Action : null;

        PriceTxt.text = _data.paymentInfo?.price.ToString() ?? "0";
        LoadBoxModel(_data);
    }

    public override void OnHidden()
    {
        base.OnHidden();
        _onClose?.Invoke();
        _onClose = null;
        CleanupBoxModel();
    }

    private void LoadBoxModel(CharacterBoxInfo info)
    {
        CleanupBoxModel();

        var prefab = Loader.Load<GameObject>(BoxModelPath);
        if (prefab == null) return;

        _boxModel = prefab.Instantiate(BoxModelRoot);
        _boxModel.transform.localPosition = Vector3.zero;
        AvatarCameraController.RotateTarget = BoxModelRoot;

        if (string.IsNullOrEmpty(info.metaDataUrl)) return;

        var cachedModel = _boxModel;
        var request = Asset.LoadRemoteAssetAsync(info.metaDataUrl);
        if (request == null) return;
        request.completed += _ =>
        {
            if (cachedModel == null || request.result != Request.Result.Success) return;
            var text = System.Text.Encoding.UTF8.GetString(request.asset);
            if (string.IsNullOrEmpty(text)) return;
            var boxData = JsonConvert.DeserializeObject<UGCBoxSceneData>(text);
            if (boxData != null) ApplyBoxTextures(cachedModel, boxData);
        };
    }

    private void CleanupBoxModel()
    {
        foreach (var t in _boxTextures) if (t != null) Destroy(t);
        _boxTextures.Clear();
        if (_boxModel != null)
        {
            Destroy(_boxModel);
            _boxModel = null;
        }
    }

    private void ApplyBoxTextures(GameObject model, UGCBoxSceneData boxData)
    {
        if (boxData?.parts == null || boxData.parts.Count == 0) return;
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        foreach (var part in boxData.parts)
        {
            if (part?.pixels == null || part.pixels.Count == 0) continue;
            int rendererIndex = part.type - 1;
            if (rendererIndex < 0 || rendererIndex >= renderers.Length) continue;

            int canvasSize = part.pixels.Count <= 1024 ? 32 : 64;
            var colors = new Color32[canvasSize * canvasSize];
            foreach (var pixel in part.pixels)
            {
                var pos = DataUtil.DeSerializeVector2Int(pixel.p);
                Color col = DataUtil.DeSerializeColor(pixel.col);
                int idx = pos.y * canvasSize + pos.x;
                if (idx >= 0 && idx < colors.Length) colors[idx] = col;
            }

            var tex = new Texture2D(canvasSize, canvasSize, TextureFormat.RGBA32, false);
            tex.SetPixels32(colors);
            tex.Apply();
            _boxTextures.Add(tex);

            var mat = renderers[rendererIndex].material;
            if (mat.shader.name == "Universal Render Pipeline/Lit")
                mat.SetTexture("_BaseMap", tex);
            else if (mat.shader.name == "bud/patterns_ugc_ARI")
                mat.SetTexture("_patterns_tex", tex);
            else
                mat.SetTexture("_MainTex", tex);
        }
    }

    private void OnBuyBtnClick()
    {
        if (_data == null) return;

        if (!HasEnoughBalance(out int needNum))
        {
            OpenExchangePanel(needNum);
            return;
        }

        AIPartnerShopRequestCtrl.Inst.RequestBuyPartner(_data.id,
            () =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                // 广播购买成功消息，通知 CabinControllScenePanel 刷新已拥有的盒子场景列表
                MessageHelper.Broadcast(MessageName.OnBuyUgcItemSuccess, _data.id);
                CloseSelf();
                var rewards = new List<BoxRewardItemData>
                {
                    new()
                    {
                        itemType = BoxRewardItemData.ItemType.Common,
                        commonData = new CommonRewardItemData
                        {
                            UgcCover = _data.cover,
                            rewardName = _data.name,
                            RewardAmount = 1
                        }
                    }
                };
                UIManager.Inst.OpenPanel<CommonBoxRewardPanel>(PanelId.CommonBoxRewardPanel)
                    .ShowReward(rewards, OnRewardPanelClose);
            },
            err => Debug.LogError("购买盒子失败: " + err));
    }

    private bool HasEnoughBalance(out int needNum)
    {
        int price = _data.paymentInfo?.price ?? 0;
        var currencyType = _data.paymentInfo?.currencyType ?? CurrencyType.None;
        var balance = AccountDataManager.Inst.BalanceInfo.GetAccountCount(currencyType);
        needNum = Mathf.Max(0, price - balance);
        return needNum == 0;
    }

    private void OnRewardPanelClose()
    {
        if (!string.IsNullOrEmpty(CabinBoxManager.Inst.GetCurrentDeviceId()))
            UIManager.Inst.OpenPanel(PanelId.BuyTipsPanel);
        else
            _onSuccess?.Invoke();
    }

    private void OpenExchangePanel(int needNum)
    {
        var currencyType = _data.paymentInfo?.currencyType ?? CurrencyType.None;
        switch (currencyType)
        {
            case CurrencyType.Coin:
                UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel)
                    .SetData(CurrencyType.Coin, CurrencyType.Gem, needNum);
                break;
            case CurrencyType.Badge:
                UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel)
                    .SetData(CurrencyType.Badge, CurrencyType.Gem, needNum);
                break;
            case CurrencyType.PinkCoin:
                if (ExchangeCoinPanel.JudgePinkCoin(needNum))
                    UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel)
                        .SetData(CurrencyType.PinkCoin, CurrencyType.Gem, needNum);
                break;
            case CurrencyType.Gem:
                UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                break;
            default:
                UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel)
                    .SetData(currencyType, CurrencyType.Gem, needNum);
                break;
        }
    }
}
