using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ExtraRewardItem : MonoBehaviour
{
    [SerializeField] private CButton ItemButton;
    [SerializeField] private Image RewardIcon;
    [SerializeField] private Text RewardNum;
    [SerializeField] private Text CountNum;
    [SerializeField] private GameObject ClaimGo;
    [SerializeField] private GameObject LockGo;
    [SerializeField] private GameObject DoneGo;

    private Action itemClickListener;
    private ExtraRewardConfig _data;

    private int curState = 0;
    
    private void Start()
    {
        InitUI();
    }

    private void InitUI()
    {
        ItemButton.onClick.AddListener(OnBtnClick);
    }

    public void InitData(ExtraRewardConfig config)
    {
        _data = config;
        SetIcon(config.atlasPath,config.iconName);
        SetCountNum(config.CountNum);
        SetRewardNum(config.RewardNum);
    }

    public ExtraRewardConfig GetBindData()
    {
        return _data;
    }

    public void SetState(int state)
    {
        switch (state)
        {
            case (int)BudRewardStatus.ErrRewardStatus:
            case (int)BudRewardStatus.Lock:
                LockGo.SetActive(true);
                ClaimGo.SetActive(false);
                DoneGo.SetActive(false);
                ItemButton.SetClickAble(true);
                break;
            case (int)BudRewardStatus.Unlocked:
                LockGo.SetActive(false);
                ClaimGo.SetActive(true);
                DoneGo.SetActive(false);
                ItemButton.SetClickAble(true);
                break;
            case (int)BudRewardStatus.Claimed:
                LockGo.SetActive(false);
                ClaimGo.SetActive(false);
                DoneGo.SetActive(true);
                ItemButton.SetClickAble(false);
                break;
        }

        curState = state;
    }

    public int GetState()
    {
        return curState;
    }
    

    public void SetIcon(Sprite sprite)
    {
        RewardIcon.sprite = sprite;
    }
    
    public void SetIcon(string atlasPath,string spriteName)
    {
        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spriteName, this.gameObject);
        RewardIcon.sprite = sprite;
    }

    public void SetRewardNum(int num)
    {
        RewardNum.SetText("x" +num);
    }

    public void SetCountNum(int num)
    {
        CountNum.SetText(num+"");
    }

    public void AddClickListener(Action callback)
    {
        itemClickListener = callback;
    }

    public void ClearClickListener()
    {
        itemClickListener = null;
    }


    private void OnBtnClick()
    {
        itemClickListener?.Invoke();
    }
}
