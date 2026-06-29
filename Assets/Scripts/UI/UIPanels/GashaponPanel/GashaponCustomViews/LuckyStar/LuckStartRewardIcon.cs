using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class LuckStartRewardIcon : MonoBehaviour {

    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private float delay = 0.5f;
    [SerializeField]
    private float duration = 0.5f;

    public BUDRewardType rewardType;
    public string pgcId;
    private Action onClickCallback;

    private void Start() {

        RefreshIcon();

        GetComponent<CButton>().onClick.AddListener(OnClick);
        Invoke(nameof(PlayAnim), delay);
    }

    private void PlayAnim() {
        var rectTransform = transform.GetComponent<RectTransform>();
        var localPos = rectTransform.anchoredPosition;
        var targetPos = new Vector3(localPos.x, localPos.y + 30);
        var sequence = DOTween.Sequence();
        sequence.Append(rectTransform.DOAnchorPos(targetPos, duration));
        sequence.Append(rectTransform.DOAnchorPos(localPos, duration));
        sequence.SetLoops(-1, LoopType.Yoyo);
        sequence.Play();
    }

    public void OnClick() {
        onClickCallback?.Invoke();
    }

    public void SetClickCallBack(Action callback) {
        onClickCallback = callback;
    }


    public void SetReward(BUDRewardType type, string id = null) {
        rewardType = type;
        pgcId = id;
        RefreshIcon();
    }

    private void RefreshIcon() {


        if (rewardType == BUDRewardType.ErrRewardType) {
            return;
        }

        if (iconImage.sprite != null) {
            return;
        }
        iconImage.color = Color.clear;
        // 暂时处理 非 PGC 即认为是 货币
        if (!string.IsNullOrEmpty(pgcId)) {
            PgcUtils.GetIconSpriteByPgcIdAsync(pgcId, gameObject, sp => {
                iconImage.sprite = sp;
                iconImage.color = Color.white;
                RefreshSize();
            });
        } else {
            iconImage.sprite = PgcUtils.LoadRewardIcon(rewardType, gameObject);
            iconImage.color = Color.white;
            RefreshSize();
        }
    }

    private void RefreshSize() {
        Sprite sp = iconImage.sprite;
        if (sp != null) {
            var newSize = iconImage.rectTransform.sizeDelta;
            float w = sp.rect.width / iconImage.pixelsPerUnit;
            float h = sp.rect.height / iconImage.pixelsPerUnit;
            float scale = 1;
            if (w > h) {
                scale = newSize.x / w;
            } else {
                scale = newSize.y / h;
            }
            iconImage.rectTransform.sizeDelta = new Vector2(w, h)*scale;
        }
    }

}
