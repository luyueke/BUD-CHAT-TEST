

using UnityEngine;
using UnityEngine.UI;

public class GameHallFriendItem : MonoBehaviour
{
    public GameObject onLineFlagGo;
    public GameObject userItem;
    public RawImage headIconImg;

    public Image offline;

    public void Init()
    {
    }


    public void SetData(MyFriendsInfo info)
    {
        userItem.SetActive(false);
        onLineFlagGo.SetActive(false);
        headIconImg.color = new Color(0.74f, 0.74f, 0.74f, 1.0f);
        if (info != null)
        {
            var isOnline = info.isOnline == 1;
            if (isOnline){
                headIconImg.color = Color.white;
                onLineFlagGo.SetActive(true);
                offline.gameObject.SetActive(false);
            }
            else
            {
                offline.gameObject.SetActive(true);
            }
            userItem.SetActive(true);
        }
    }
}



