using Game.Store;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class TheatreActionItem : MonoBehaviour
{
    [SerializeField] private Image ActionIcon;
    [SerializeField] private Text PriceText;
    [SerializeField] private CButton JumpBtn;
    [SerializeField] private Text ItemStatus;
    [SerializeField] private CButton ShowBtn;
    [SerializeField] private GameObject Selected;

    private string _ugcId;
    private bool _isEmote;
    private string _id;
    private bool _isPgc;
    private int _price;

    public System.Action<string, bool> OnJumpBtnClicked;
    public bool IsSelected => Selected != null && Selected.activeSelf;
    public string Id => _id;
    public int Price => _price;

    void Awake()
    {
        ShowBtn.onClick.AddListener(OnShowBtnClick);
        JumpBtn.onClick.AddListener(OnJumpBtnClick);
    }

    public void SetData(string id, bool isEmote = true)
    {
        if (string.IsNullOrEmpty(id)) return;
        _id = id;
        _isPgc = UniqueType.IsPgc(id);
        _price = 0;
        Selected.SetActive(false);
        ItemStatus.gameObject.SetActive(false);
        ShowBtn.gameObject.SetActive(!_isPgc);
        if (_isPgc)
            SetPgcData(id);
        else
            SetUgcData(id, isEmote);
    }

    public void SelectAction()
    {
        if (!_isPgc) Selected.SetActive(true);
    }

    public void DeselectAction()
    {
        Selected.SetActive(false);
    }

    private void SetPgcData(string pgcId)
    {
        PriceText.gameObject.SetActive(false);
        var sprite = PgcUtils.LoadEmoteIcon(pgcId, gameObject);
        if (sprite != null)
        {
            ActionIcon.sprite = sprite;
            ActionIcon.enabled = true;
        }
        bool owned = AssetsDataManager.IsOwned(pgcId);
        ItemStatus.gameObject.SetActive(true);
        ItemStatus.text = owned ? "已拥有" : "扭蛋获取";
    }

    private void OnJumpBtnClick()
    {
        if (!_isPgc)
            Selected.SetActive(!Selected.activeSelf);
        OnJumpBtnClicked?.Invoke(_id, _isPgc);
    }

    private void OnShowBtnClick()
    {
        if (string.IsNullOrEmpty(_ugcId)) return;
        var detailType = _isEmote ? AssetDetailType.UgcAnim : AssetDetailType.MusicTone;
        UIManager.Inst.OpenPanel(PanelId.AssetDetailPanel, detailType, _ugcId);
    }

    private void SetUgcData(string ugcId, bool isEmote)
    {
        _ugcId = ugcId;
        _isEmote = isEmote;

        if (!isEmote)
        {
            PriceText.gameObject.SetActive(false);
            return;
        }

        AssetsDataManager.GetUgcAnimInfo(ugcId, (success, data) =>
        {
            if (!success || data == null) return;
            var ugcInfo = data.UgcInfo;
            if (ugcInfo == null) return;

            int price = data.animInfo?.paymentInfo?.price ?? 0;
            _price = price;
            PriceText.text = price.ToString();

            if (!string.IsNullOrEmpty(ugcInfo.cover))
            {
                Game.Utils.GameSimpleImageDownloader.Instance.Enqueue(new Game.Utils.GameSimpleImageDownloader.Request
                {
                    url = ugcInfo.cover,
                    onDone = result =>
                    {
                        if (ActionIcon == null) return;
                        var tex = result.CreateTextureFromReceivedData();
                        ActionIcon.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
                        ActionIcon.enabled = true;
                    }
                });
            }
        });
    }
}
