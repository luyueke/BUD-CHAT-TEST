using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using System.Collections;
using System.Collections.Generic;
using UI.TopList;
using UnityEngine;
using UnityEngine.UI;

public class UserMapShowItem : MonoBehaviour
{
    public RawImage userImage;
    public RawImage userUnlockImage;

    public void Init()
    {
        userImage.gameObject.SetActive(false);
        userUnlockImage.gameObject.SetActive(true);
    }


    public void InitUI(string url)
    {   
        userImage.gameObject.SetActive(true);
        userUnlockImage.gameObject.SetActive(false);
        // 确保charactreImage不为null
        if (userImage != null)
        {
            var remoteRewardRawImg = userImage.GetComponent<RemoteImageBehaviour>();
            if (remoteRewardRawImg != null)
            {
                remoteRewardRawImg.Load(
                    url,
                    true,
                    (fromCache, success) => {
                        if (!success)
                        {
                            LoggerUtils.LogError("无法加载图片");
                            userImage.gameObject.SetActive(false);
                            userUnlockImage.gameObject.SetActive(true);
                        }
                    }
                );
            }
            else
            {
                userImage.gameObject.SetActive(false);
                userUnlockImage.gameObject.SetActive(true);
            }
        }
    }


}
