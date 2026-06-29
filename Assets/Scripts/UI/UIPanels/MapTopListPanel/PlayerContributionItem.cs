using Com.TheFallenGames.OSA.Util.IO;
using System.Collections;
using System.Collections.Generic;
using UI.TopList;
using UnityEngine;
using UnityEngine.UI;

public class PlayerContributionItem : MonoBehaviour
{
    public Text nameTx;
    public Text rankTx;
    public Text fireTx;

    public RawImage charactreImage;
    public RawImage charactreUnlockImage;

    public void Init() {
        charactreImage.gameObject.SetActive(false);
        charactreUnlockImage.gameObject.SetActive(true);
        fireTx.text = "0";
        nameTx.text = "虚位以待";
    }


    public void InitUI(HeatContribution heatContribution) {
        Init();
        charactreImage.gameObject.SetActive(true);
        charactreUnlockImage.gameObject.SetActive(false);
        nameTx.text = heatContribution.userInfo.nickname;
        fireTx.text = heatContribution.score.ToString();
        // 确保charactreImage不为null
        if (charactreImage != null)
        {
            var remoteRewardRawImg = charactreImage.GetComponent<RemoteImageBehaviour>();
            if (remoteRewardRawImg != null)
            {
                remoteRewardRawImg.Load(
                    heatContribution.userInfo.portraitUrl,
                    true,
                    (fromCache, success) => {
                        if (!success)
                        {
                            charactreImage.gameObject.SetActive(false);
                            charactreUnlockImage.gameObject.SetActive(true);
                            LoggerUtils.LogError("无法加载图片");
                        }
                    }
                );
            }
        }
        else
        {
            charactreImage.gameObject.SetActive(false);
            charactreUnlockImage.gameObject.SetActive(true);
        }
    }
}
