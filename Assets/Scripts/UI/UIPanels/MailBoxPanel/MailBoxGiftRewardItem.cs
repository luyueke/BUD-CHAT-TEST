using Com.TheFallenGames.OSA.Util.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.MailBox
{
    public class MailBoxGiftRewardItem : MonoBehaviour
    {
        [HideInInspector] public Image rewardIcon;
        [HideInInspector] public RemoteImageBehaviour remoteAssetsIcon;
        [HideInInspector] public Text rewardNum;
        [HideInInspector] public GameObject rewardMask;

        private void Awake()
        {
            rewardIcon = GameObjectEx.FindChildByName(transform, "RewardIcon").GetComponent<Image>();
            rewardNum = GameObjectEx.FindChildByName(transform, "RewardNum").GetComponent<Text>();
            //remoteAssetsIcon = GameObjectEx.FindChildByName(transform, "RemoteIcon").GetComponent<RemoteImageBehaviour>();
            rewardMask = GameObjectEx.FindChildByName(transform, "RewardMask").gameObject;
        }



    }
}