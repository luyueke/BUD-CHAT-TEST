using System;
using Game.Audio;
using UnityEngine;
using UnityEngine.UI;

public class MagicLotteryItem : MonoBehaviour {

    public string lotteryName;

    private Action<string> onSelectCallback;

    public void SetOnSelectCallBack(Action<string> callBack) {
        onSelectCallback = callBack;
    }

    public void OnSelect(bool isOn) {
        if (isOn) {
            AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftItems_B2);
            onSelectCallback?.Invoke(lotteryName);
        }
    }

    public void SetIsOn(bool isOn) {
        GetComponent<Toggle>().isOn = isOn;
    }

    public void SetIsOnWithoutNotify(bool isOn) {
        GetComponent<Toggle>().SetIsOnWithoutNotify(isOn);
    }


}
