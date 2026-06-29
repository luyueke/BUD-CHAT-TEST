
using Game.GameHall.View;
using UnityEngine;
using UnityEngine.EventSystems;

public class FriendRequestView : MonoBehaviour
{

    public FriendRequestListEntry FriendRequestListEntry;
    private bool isInit = false;
    public GameObject noFriendObj;
    public GameObject loadingObj;

    private void Start()
    {
        FriendRequestListEntry.AddClickListener(OnScrollViewClick);
    }
    

    private void OnDestroy()
    {
        
    }

    public void OnInitCreated()
    {
        if (isInit)
        {
            return;
        }

        isInit = true;
        loadingObj.gameObject.SetActive(true);
        FriendRequestListEntry.GetFirstPageFriendDatas(OnHasFriends);
        
    }
    
    private void OnHasFriends(bool hasFriends)
    {
        loadingObj.gameObject.SetActive(false);
        noFriendObj.gameObject.SetActive(!hasFriends);
    }
    
    private void OnScrollViewClick(PointerEventData data){
     
    }
}
